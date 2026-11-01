using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EquipmentTracking.IntegrationTests;

public class AuthTests
{
    [Fact]
    public async Task Login_me_logout_and_role_protected_action_use_the_seeded_identity()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"equipment-tracking-auth-{Guid.NewGuid():N}.db");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("DatabasePath", databasePath));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var unauthenticated = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/review/action", null)).StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "reviewer@equipment.local", password = "Review123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loginBody = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", loginBody, StringComparison.OrdinalIgnoreCase);
        var user = JsonSerializer.Deserialize<JsonElement>(loginBody);
        Assert.Equal("reviewer@equipment.local", user.GetProperty("email").GetString());
        Assert.Contains("Manager/Admin", user.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));

        var me = await client.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("reviewer@equipment.local", me.GetProperty("email").GetString());
        Assert.Contains("Manager/Admin", me.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
        var action = await client.PostAsync("/api/review/action", null);
        Assert.Equal(HttpStatusCode.OK, action.StatusCode);
        var actionBody = await action.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("reviewer@equipment.local", actionBody.GetProperty("actor").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/logout", null)).StatusCode);
    }

    [Fact]
    public async Task Invalid_credentials_are_generic_and_non_manager_is_forbidden()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"equipment-tracking-auth-{Guid.NewGuid():N}.db");
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("DatabasePath", databasePath));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        var invalid = await client.PostAsJsonAsync("/api/auth/login", new { email = "unknown@equipment.local", password = "wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);
        Assert.DoesNotContain("unknown@equipment.local", await invalid.Content.ReadAsStringAsync());

        foreach (var (email, expectedStatus) in new[]
        {
            ("ali@equipment.local", HttpStatusCode.Forbidden),
            ("saima@equipment.local", HttpStatusCode.Forbidden),
            ("hamza@equipment.local", HttpStatusCode.Forbidden),
            ("reviewer@equipment.local", HttpStatusCode.OK)
        })
        {
            using var roleClient = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
            var login = await roleClient.PostAsJsonAsync("/api/auth/login", new { email, password = "Review123!" });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.Equal(expectedStatus, (await roleClient.PostAsync("/api/review/action", null)).StatusCode);
        }
    }
}
