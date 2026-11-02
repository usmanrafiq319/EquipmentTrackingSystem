using EquipmentTracking.Api.Data;
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
app.MapGet("/api/equipment", async (string? search, ApplicationDbContext db) =>
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

    return Results.Ok(equipment.Select(item => ToEquipmentResponse(item, rules)));
})
    .RequireAuthorization();
app.MapGet("/api/equipment/{assetId}", async (string assetId, ApplicationDbContext db) =>
{
    var item = await db.Equipment.AsNoTracking().SingleOrDefaultAsync(e => e.AssetId == assetId);
    if (item is null)
        return Results.NotFound(new { error = "Equipment was not found." });

    var rules = await db.ApprovalRules.AsNoTracking()
        .Where(rule => rule.EquipmentAssetId == item.AssetId)
        .ToListAsync();
    if (!HasValidQuantities([item]))
        return Results.Problem("Equipment quantity data is invalid.", statusCode: StatusCodes.Status500InternalServerError);

    return Results.Ok(ToEquipmentResponse(item, rules));
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

static EquipmentResponse ToEquipmentResponse(Equipment item, IEnumerable<ApprovalRule> rules) =>
    new(item.AssetId, item.Name, item.TrackingMode, item.Total, item.Available,
        item.CheckedOut, item.Damaged, item.Condition,
        item.TrackingMode == "Individual" ? item.Available == 1 : null,
        rules.Any(rule => rule.EquipmentAssetId == item.AssetId),
        null, null, false, false,
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
    IReadOnlyList<string> ApproverEmails);
