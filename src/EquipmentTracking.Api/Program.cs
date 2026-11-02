using EquipmentTracking.Api.Data;
using EquipmentTracking.Api.Features.Custody;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var databasePath = builder.Configuration["DatabasePath"] ?? Path.Combine(builder.Environment.ContentRootPath, "data", "equipment-tracking.db");
var databaseDirectory = Path.GetDirectoryName(databasePath);
if (!string.IsNullOrEmpty(databaseDirectory))
    Directory.CreateDirectory(databaseDirectory);
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CheckoutCommandService>();
builder.Services.AddScoped<ReturnCommandService>();
builder.Services.AddIdentityCore<IdentityUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager();
builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies(options => options.ApplicationCookie!.Configure(cookie =>
    {
        cookie.Cookie.Name = "equipment-tracking-session";
        cookie.Cookie.HttpOnly = true;
        cookie.Cookie.SameSite = SameSiteMode.Lax;
        cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        cookie.LoginPath = "/api/auth/login";
        cookie.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        cookie.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    }));
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("database");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapGet("/readiness", async (ApplicationDbContext db) =>
{
    var ready = await db.Database.CanConnectAsync();
    return ready ? Results.Ok(new { ready }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});
app.MapGet("/api/review/summary", async (ApplicationDbContext db) => Results.Ok(new
{
    users = await db.Users.CountAsync(),
    roles = await db.Roles.CountAsync(),
    equipment = await db.Equipment.CountAsync(),
    approvalRules = await db.ApprovalRules.CountAsync()
}));
app.MapPost("/api/auth/login", async (
    LoginRequest request,
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> users) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.Unauthorized();

    var user = await users.FindByEmailAsync(request.Email.Trim());
    if (user is null || !await users.CheckPasswordAsync(user, request.Password))
        return Results.Unauthorized();

    await signInManager.SignInAsync(user, isPersistent: false);
    var roles = await users.GetRolesAsync(user);
    return Results.Ok(new AuthUser(user.Id, user.Email ?? user.UserName ?? string.Empty, user.UserName ?? user.Email ?? string.Empty, roles));
});
app.MapPost("/api/auth/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
});
app.MapGet("/api/auth/me", async (
    HttpContext httpContext,
    UserManager<IdentityUser> users) =>
{
    if (httpContext.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var user = await users.GetUserAsync(httpContext.User);
    if (user is null)
        return Results.Unauthorized();

    return Results.Ok(new AuthUser(user.Id, user.Email ?? user.UserName ?? string.Empty, user.UserName ?? user.Email ?? string.Empty, await users.GetRolesAsync(user)));
}).RequireAuthorization();
app.MapGet("/api/custody/active", async (
    HttpContext httpContext,
    ApplicationDbContext db,
    UserManager<IdentityUser> users,
    TimeProvider clock) =>
{
    var actor = await users.GetUserAsync(httpContext.User);
    if (actor is null)
        return Results.Unauthorized();

    var roles = await users.GetRolesAsync(actor);
    var canViewAll = roles.Any(role => role is "Office Staff" or "Manager/Admin" or "Approver");
    var records = db.CustodyRecords.AsNoTracking().Where(record => record.IsOpen);
    if (!canViewAll)
        records = records.Where(record => record.BorrowerId == actor.Id);

    var rows = await (
        from custody in records
        join equipment in db.Equipment.AsNoTracking() on custody.EquipmentAssetId equals equipment.AssetId
        join borrower in db.Users.AsNoTracking() on custody.BorrowerId equals borrower.Id
        select new
        {
            custody.Id,
            custody.EquipmentAssetId,
            equipment.Name,
            equipment.TrackingMode,
            custody.BorrowerId,
            Borrower = borrower.Email ?? borrower.UserName ?? borrower.Id,
            custody.CheckedOutAt,
            custody.DueAt,
            custody.DeclaredLocation,
            custody.Quantity,
            custody.OutstandingQuantity,
            custody.IsIndividual,
            custody.Status
        }).ToListAsync();
    var now = clock.GetUtcNow();
    return Results.Ok(rows
        .OrderBy(row => row.DueAt)
        .ThenBy(row => row.Name)
        .Select(row => new ActiveCustodyResponse(
            row.Id,
            row.EquipmentAssetId,
            row.Name,
            row.TrackingMode,
            row.BorrowerId,
            row.Borrower,
            row.CheckedOutAt,
            row.DueAt,
            row.DeclaredLocation,
            row.Quantity,
            row.OutstandingQuantity,
            row.IsIndividual,
            row.Status,
            CustodyTimePolicy.GetDueStatus(row.DueAt, now))));
}).RequireAuthorization();
app.MapGet("/api/equipment", async (
    string? search,
    ApplicationDbContext db,
    TimeProvider clock) =>
{
    var equipment = await db.Equipment
        .AsNoTracking()
        .ToListAsync();
    if (!HasValidQuantities(equipment))
        return Results.Problem("Equipment quantity data is invalid.", statusCode: StatusCodes.Status500InternalServerError);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        equipment = equipment
            .Where(item => item.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                           item.AssetId.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    equipment = equipment
        .OrderBy(item => item.Name)
        .ThenBy(item => item.AssetId)
        .ToList();
    var rules = await db.ApprovalRules.AsNoTracking().ToListAsync();
    var custody = await db.CustodyRecords.AsNoTracking()
        .Where(record => record.IsOpen && record.IsIndividual)
        .ToListAsync();
    var borrowerIds = custody.Select(record => record.BorrowerId).Distinct().ToArray();
    var borrowers = await db.Users.AsNoTracking()
        .Where(user => borrowerIds.Contains(user.Id))
        .ToDictionaryAsync(user => user.Id, user => user.Email ?? user.UserName ?? user.Id);
    var now = clock.GetUtcNow();

    return Results.Ok(equipment.Select(item =>
    {
        var currentCustody = custody.SingleOrDefault(record => record.EquipmentAssetId == item.AssetId);
        var custodian = currentCustody is not null && borrowers.TryGetValue(currentCustody.BorrowerId, out var email)
            ? email
            : null;
        return ToEquipmentResponse(item, rules, currentCustody, custodian, now);
    }));
})
    .RequireAuthorization();
app.MapGet("/api/equipment/{assetId}", async (
    string assetId,
    ApplicationDbContext db,
    TimeProvider clock) =>
{
    var item = await db.Equipment.AsNoTracking().SingleOrDefaultAsync(e => e.AssetId == assetId);
    if (item is null)
        return Results.NotFound(new { error = "Equipment was not found." });

    var rules = await db.ApprovalRules.AsNoTracking()
        .Where(rule => rule.EquipmentAssetId == item.AssetId)
        .ToListAsync();
    var custody = await db.CustodyRecords.AsNoTracking()
        .SingleOrDefaultAsync(record => record.EquipmentAssetId == item.AssetId && record.IsOpen && record.IsIndividual);
    var custodian = custody is null
        ? null
        : await db.Users.AsNoTracking()
            .Where(user => user.Id == custody.BorrowerId)
            .Select(user => user.Email ?? user.UserName ?? user.Id)
            .SingleOrDefaultAsync();
    if (!HasValidQuantities([item]))
        return Results.Problem("Equipment quantity data is invalid.", statusCode: StatusCodes.Status500InternalServerError);

    return Results.Ok(ToEquipmentResponse(item, rules, custody, custodian, clock.GetUtcNow()));
}).RequireAuthorization();
app.MapPost("/api/equipment", async (CreateEquipmentRequest request, ApplicationDbContext db) =>
{
    var name = request.Name?.Trim();
    var assetId = request.AssetId?.Trim();
    var trackingMode = request.TrackingMode?.Trim();

    if (string.IsNullOrWhiteSpace(name))
        return Results.BadRequest(new { error = "Equipment name is required." });
    if (string.IsNullOrWhiteSpace(assetId))
        return Results.BadRequest(new { error = "Asset ID is required." });
    if (assetId.Length > 100 || assetId.Contains('/'))
        return Results.BadRequest(new { error = "Asset ID must be 100 characters or fewer and cannot contain '/'." });
    if (name.Length > 200)
        return Results.BadRequest(new { error = "Equipment name must be 200 characters or fewer." });
    if (!string.Equals(trackingMode, "Individual", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(trackingMode, "Pooled", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Tracking mode must be Individual or Pooled." });
    if (await db.Equipment.AnyAsync(item => item.AssetId == assetId))
        return Results.BadRequest(new { error = "Asset ID already exists." });

    var isPooled = string.Equals(trackingMode, "Pooled", StringComparison.OrdinalIgnoreCase);
    if (isPooled && (!request.Total.HasValue || request.Total.Value <= 0))
        return Results.BadRequest(new { error = "Pooled total must be a positive integer." });

    var total = isPooled ? request.Total!.Value : 1;
    var equipment = new Equipment
    {
        AssetId = assetId,
        Name = name,
        TrackingMode = isPooled ? "Pooled" : "Individual",
        Total = total,
        Available = total,
        CheckedOut = 0,
        Damaged = 0,
        Condition = "Good"
    };
    db.Equipment.Add(equipment);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 19 })
    {
        return Results.BadRequest(new { error = "Asset ID already exists." });
    }

    return Results.Created($"/api/equipment/{Uri.EscapeDataString(equipment.AssetId)}",
        ToEquipmentResponse(equipment, []));
}).RequireAuthorization(policy => policy.RequireRole("Office Staff", "Manager/Admin"));
app.MapPost("/api/approvals/requests", async (
        ApprovalRequestCommand request,
        HttpContext httpContext,
        ApplicationDbContext db,
        UserManager<IdentityUser> users,
        TimeProvider clock) =>
{
        var actor = await users.GetUserAsync(httpContext.User);
        if (actor is null)
            return Results.Unauthorized();

        var roles = await users.GetRolesAsync(actor);
        var borrower = actor;
        if (!string.IsNullOrWhiteSpace(request.BorrowerEmail))
        {
            if (!roles.Contains("Office Staff") && !roles.Contains("Manager/Admin"))
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            borrower = await users.FindByEmailAsync(request.BorrowerEmail.Trim());
            if (borrower is null)
                return Results.BadRequest(new { error = "The borrower was not found." });
        }

        var equipment = await db.Equipment.SingleOrDefaultAsync(item => item.AssetId == request.EquipmentAssetId.Trim());
        if (equipment is null)
            return Results.NotFound(new { error = "Equipment was not found." });
        if (!await db.ApprovalRules.AnyAsync(rule => rule.EquipmentAssetId == equipment.AssetId))
            return Results.BadRequest(new { error = "Equipment does not require approval." });
        var quantity = request.Quantity ?? 1;
        if (quantity <= 0 || (equipment.TrackingMode == "Individual" && quantity != 1))
            return Results.BadRequest(new { error = "Quantity must be a positive integer and match the equipment tracking mode." });
        var now = clock.GetUtcNow();
        var dueAt = request.DueAt.HasValue
            ? CustodyTimePolicy.Normalize(request.DueAt.Value)
            : CustodyTimePolicy.DefaultDueAt(now);
        if (dueAt <= now)
            return Results.BadRequest(new { error = "Due time must be in the future." });
        if (request.DeclaredLocation is { Length: > 200 })
            return Results.BadRequest(new { error = "Declared location must be 200 characters or fewer." });

        var approval = new ApprovalRequest
        {
            EquipmentAssetId = equipment.AssetId,
            BorrowerId = borrower.Id,
            RequestedById = actor.Id,
            RequestedAt = clock.GetUtcNow(),
            DueAt = dueAt,
            DeclaredLocation = string.IsNullOrWhiteSpace(request.DeclaredLocation) ? null : request.DeclaredLocation.Trim(),
            Quantity = quantity
        };
        db.ApprovalRequests.Add(approval);
        await db.SaveChangesAsync();
        return Results.Accepted($"/api/approvals/requests/{approval.Id}", new
        {
            id = approval.Id,
            status = approval.Status,
            equipmentAssetId = approval.EquipmentAssetId,
            borrowerId = approval.BorrowerId,
            quantity = approval.Quantity,
            dueAt = approval.DueAt.UtcDateTime
        });
}).RequireAuthorization();
app.MapPost("/api/custody/checkouts", async (
        CheckoutRequest request,
        HttpContext httpContext,
        UserManager<IdentityUser> users,
        CheckoutCommandService service,
        CancellationToken cancellationToken) =>
{
        var actor = await users.GetUserAsync(httpContext.User);
        if (actor is null)
            return Results.Unauthorized();

        var result = await service.ExecuteAsync(actor, new CheckoutCommand(
            request.EquipmentAssetId ?? string.Empty,
            request.BorrowerEmail,
            request.Quantity,
            request.DueAt,
            request.DeclaredLocation), cancellationToken);
        if (result.StatusCode == StatusCodes.Status201Created)
        {
            var custody = result.Custody!;
            return Results.Created($"/api/custody/{custody.Id}", new
            {
                id = custody.Id,
                equipmentAssetId = custody.EquipmentAssetId,
                borrowerId = custody.BorrowerId,
                checkoutActorId = custody.CheckoutActorId,
                checkedOutAt = custody.CheckedOutAt.UtcDateTime,
                quantity = custody.Quantity,
                outstandingQuantity = custody.OutstandingQuantity,
                dueAt = custody.DueAt.UtcDateTime,
                declaredLocation = custody.DeclaredLocation,
                isIndividual = custody.IsIndividual,
                isOpen = custody.IsOpen,
                status = custody.Status,
                approvalRequestId = custody.ApprovalRequestId
            });
        }

        return Results.Json(new { error = result.Error }, statusCode: result.StatusCode);
}).RequireAuthorization();
app.MapPost("/api/custody/{custodyId:guid}/returns", async (
        Guid custodyId,
        ReturnRequest request,
        HttpContext httpContext,
        UserManager<IdentityUser> users,
        ReturnCommandService service,
        CancellationToken cancellationToken) =>
{
    var actor = await users.GetUserAsync(httpContext.User);
    if (actor is null)
        return Results.Unauthorized();

    var result = await service.ExecuteAsync(actor, new ReturnCommand(
        custodyId,
        request.Quantity,
        request.Condition,
        request.GoodQuantity,
        request.DamagedQuantity,
        request.Note), cancellationToken);
    if (result.StatusCode != StatusCodes.Status200OK)
        return Results.Json(new { error = result.Error }, statusCode: result.StatusCode);

    var custody = result.Custody!;
    var returnEvent = result.Event!;
    return Results.Ok(new
    {
        custody = new
        {
            id = custody.Id,
            equipmentAssetId = custody.EquipmentAssetId,
            borrowerId = custody.BorrowerId,
            quantity = custody.Quantity,
            outstandingQuantity = custody.OutstandingQuantity,
            isIndividual = custody.IsIndividual,
            isOpen = custody.IsOpen,
            status = custody.Status
        },
        @event = new
        {
            id = returnEvent.Id,
            custodyRecordId = returnEvent.CustodyRecordId,
            equipmentAssetId = returnEvent.EquipmentAssetId,
            actorId = returnEvent.ActorId,
            returnedAt = returnEvent.ReturnedAt.UtcDateTime,
            quantity = returnEvent.Quantity,
            condition = returnEvent.Condition,
            goodQuantity = returnEvent.GoodQuantity,
            damagedQuantity = returnEvent.DamagedQuantity,
            note = returnEvent.Note
        }
    });
}).RequireAuthorization();
app.MapPost("/api/review/action", (HttpContext httpContext) =>
    Results.Ok(new { actor = httpContext.User.Identity?.Name }))
    .RequireAuthorization(policy => policy.RequireRole("Manager/Admin"));
app.MapFallbackToFile("index.html");
app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

static EquipmentResponse ToEquipmentResponse(
    Equipment item,
    IEnumerable<ApprovalRule> rules,
    CustodyRecord? custody = null,
    string? custodian = null,
    DateTimeOffset? now = null) =>
    new(item.AssetId, item.Name, item.TrackingMode, item.Total, item.Available,
        item.CheckedOut, item.Damaged, item.Condition,
        item.TrackingMode == "Individual" ? item.Available == 1 : null,
        rules.Any(rule => rule.EquipmentAssetId == item.AssetId),
        custodian, custody?.DueAt, custody is not null && custody.DueAt <= (now ?? DateTimeOffset.UtcNow),
        custody is not null && custody.IsOpen,
        CustodyTimePolicy.GetDueStatus(custody?.DueAt, now ?? DateTimeOffset.UtcNow),
        rules.Where(rule => rule.EquipmentAssetId == item.AssetId)
            .Select(rule => rule.ApproverEmail)
            .ToArray());

static bool HasValidQuantities(IEnumerable<Equipment> equipment) =>
    equipment.All(item =>
        item.Total > 0 &&
        item.Available >= 0 &&
        item.CheckedOut >= 0 &&
        item.Damaged >= 0 &&
        (long)item.Available + item.CheckedOut + item.Damaged == item.Total &&
        (item.TrackingMode != "Individual" || item.Total == 1));

app.Run();

public partial class Program { }

public sealed record LoginRequest(string? Email, string? Password);

public sealed record AuthUser(string Id, string Email, string Name, IList<string> Roles);

public sealed record CreateEquipmentRequest(string? Name, string? AssetId, string? TrackingMode, int? Total);

public sealed record CheckoutRequest(
    string? EquipmentAssetId,
    string? BorrowerEmail,
    int? Quantity,
    DateTimeOffset? DueAt,
    string? DeclaredLocation);

public sealed record ReturnRequest(
    int? Quantity,
    string? Condition,
    int? GoodQuantity,
    int? DamagedQuantity,
    string? Note);

public sealed record ApprovalRequestCommand(
    string EquipmentAssetId,
    string? BorrowerEmail,
    int? Quantity,
    DateTimeOffset? DueAt,
    string? DeclaredLocation);

public sealed record EquipmentResponse(
    string AssetId,
    string Name,
    string TrackingMode,
    int Total,
    int Available,
    int CheckedOut,
    int Damaged,
    string Condition,
    bool? IsAvailable,
    bool ApprovalRequired,
    string? CurrentCustodian,
    DateTimeOffset? DueAt,
    bool IsOverdue,
    bool HasUnresolvedCustody,
    string? DueStatus,
    IReadOnlyList<string> ApproverEmails);

public sealed record ActiveCustodyResponse(
    Guid Id,
    string EquipmentAssetId,
    string EquipmentName,
    string TrackingMode,
    string BorrowerId,
    string Borrower,
    DateTimeOffset CheckedOutAt,
    DateTimeOffset DueAt,
    string? DeclaredLocation,
    int Quantity,
    int OutstandingQuantity,
    bool IsIndividual,
    string Status,
    string? DueStatus);
