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
