using EquipmentTracking.Api.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
app.MapGet("/api/equipment", async (ApplicationDbContext db) =>
    Results.Ok(await db.Equipment
        .AsNoTracking()
        .OrderBy(item => item.Name)
        .ThenBy(item => item.AssetId)
        .Select(item => new EquipmentResponse(
            item.AssetId,
            item.Name,
            item.TrackingMode,
            item.Total,
            item.Available,
            item.CheckedOut,
            item.Damaged,
            item.Condition))
        .ToListAsync()))
    .RequireAuthorization();
app.MapGet("/api/equipment/{assetId}", async (string assetId, ApplicationDbContext db) =>
{
    var item = await db.Equipment.AsNoTracking().SingleOrDefaultAsync(e => e.AssetId == assetId);
    return item is null
        ? Results.NotFound(new { error = "Equipment was not found." })
        : Results.Ok(new EquipmentResponse(item.AssetId, item.Name, item.TrackingMode, item.Total, item.Available, item.CheckedOut, item.Damaged, item.Condition));
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
    catch (DbUpdateException)
    {
        return Results.BadRequest(new { error = "Asset ID already exists." });
    }

    return Results.Created($"/api/equipment/{Uri.EscapeDataString(equipment.AssetId)}",
        new EquipmentResponse(equipment.AssetId, equipment.Name, equipment.TrackingMode, equipment.Total,
            equipment.Available, equipment.CheckedOut, equipment.Damaged, equipment.Condition));
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
    string Condition);
