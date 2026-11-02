using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using EquipmentTracking.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EquipmentTracking.IntegrationTests;

public class CustodyTests
{
    [Fact]
    public async Task Checkout_requires_authentication_and_server_enforced_borrower_role()
    {
        using var factory = CreateFactory();
        using var anonymous = factory.CreateClient();
        var request = new { equipmentAssetId = "CAMERA-DEMO-01" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/custody/checkouts", request)).StatusCode);

        using var field = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(field, "ali@equipment.local");
        var forbidden = await field.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01", borrowerEmail = "saima@equipment.local" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Restricted_checkout_is_rejected_for_manager_and_pending_request_does_not_reserve()
    {
        using var factory = CreateFactory();
        using var manager = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(manager, "reviewer@equipment.local");

        var rejected = await manager.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "DRONE-001" });
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

        var pending = await manager.PostAsJsonAsync("/api/approvals/requests",
            new { equipmentAssetId = "DRONE-001" });
        Assert.Equal(HttpStatusCode.Accepted, pending.StatusCode);
        var pendingBody = await pending.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", pendingBody.GetProperty("status").GetString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var drone = await db.Equipment.SingleAsync(item => item.AssetId == "DRONE-001");
        Assert.Equal(1, drone.Available);
        Assert.Equal(0, drone.CheckedOut);
        Assert.Empty(await db.CustodyRecords.ToListAsync());
        Assert.Single(await db.ApprovalRequests.Where(request => request.Status == "Pending").ToListAsync());
    }

    [Fact]
    public async Task Checkout_validates_quantities_availability_and_duplicate_individual_custody()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/custody/checkouts",
                new { equipmentAssetId = "TRIPODS", quantity = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/custody/checkouts",
                new { equipmentAssetId = "TRIPODS", quantity = 5 })).StatusCode);

        var first = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var duplicate = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var camera = await db.Equipment.SingleAsync(item => item.AssetId == "CAMERA-DEMO-01");
        Assert.Equal(0, camera.Available);
        Assert.Equal(1, camera.CheckedOut);
        Assert.Single(await db.CustodyRecords.Where(record => record.EquipmentAssetId == "CAMERA-DEMO-01").ToListAsync());
    }

    [Fact]
    public async Task Competing_pooled_checkouts_do_not_oversubscribe_inventory()
    {
        using var factory = CreateFactory();
        using var first = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var second = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(first, "ali@equipment.local");
        await LoginAsync(second, "saima@equipment.local");

        var responses = await Task.WhenAll(
            first.PostAsJsonAsync("/api/custody/checkouts", new { equipmentAssetId = "TRIPODS", quantity = 3 }),
            second.PostAsJsonAsync("/api/custody/checkouts", new { equipmentAssetId = "TRIPODS", quantity = 3 }));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tripods = await db.Equipment.SingleAsync(item => item.AssetId == "TRIPODS");
        Assert.Equal(1, tripods.Available);
        Assert.Equal(3, tripods.CheckedOut);
        Assert.Equal(4, tripods.Available + tripods.CheckedOut + tripods.Damaged);
    }

    [Fact]
    public async Task Checkout_uses_karachi_tomorrow_default_and_refreshes_individual_projection()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        var checkout = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01", declaredLocation = "Field vehicle" });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var checkoutBody = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var dueAt = checkoutBody.GetProperty("dueAt").GetDateTime();
        var karachi = ResolveKarachiTimeZone();
        var expectedDate = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, karachi).Date.AddDays(1);
        Assert.Equal(expectedDate, TimeZoneInfo.ConvertTime(dueAt, karachi).Date);
        Assert.Equal("Field vehicle", checkoutBody.GetProperty("declaredLocation").GetString());

        var projection = await client.GetFromJsonAsync<JsonElement>("/api/equipment/CAMERA-DEMO-01");
        Assert.Equal("ali@equipment.local", projection.GetProperty("currentCustodian").GetString());
        Assert.Equal("DueSoon", projection.GetProperty("dueStatus").GetString());
        Assert.True(projection.GetProperty("hasUnresolvedCustody").GetBoolean());
        Assert.False(projection.GetProperty("isOverdue").GetBoolean());
    }

    [Fact]
    public async Task Checkout_normalizes_editable_due_date_to_utc()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        var requestedDueAt = DateTimeOffset.UtcNow.AddDays(3).AddMinutes(1);
        var checkout = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01", dueAt = requestedDueAt });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
        var body = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var persistedDueAt = body.GetProperty("dueAt").GetDateTime();

        Assert.Equal(requestedDueAt.UtcDateTime, persistedDueAt, precision: TimeSpan.FromSeconds(1));
        var projection = await client.GetFromJsonAsync<JsonElement>("/api/equipment/CAMERA-DEMO-01");
        Assert.Equal("Active", projection.GetProperty("dueStatus").GetString());
    }

    [Fact]
    public async Task Individual_return_appends_event_and_refreshes_projection()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        var checkout = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01" });
        var checkoutBody = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var custodyId = checkoutBody.GetProperty("id").GetGuid();

        var returned = await client.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
            new { condition = "Good", note = "Returned to office" });
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var camera = await db.Equipment.SingleAsync(item => item.AssetId == "CAMERA-DEMO-01");
        Assert.Equal((1, 0, 0), (camera.Available, camera.CheckedOut, camera.Damaged));
        var custody = await db.CustodyRecords.SingleAsync(item => item.Id == custodyId);
        Assert.False(custody.IsOpen);
        var @event = await db.ReturnEvents.SingleAsync();
        Assert.Equal(("Good", 1, "Returned to office"), (@event.Condition, @event.Quantity, @event.Note));
    }

    [Fact]
    public async Task Active_custody_projection_returns_borrower_and_due_status()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        var checkout = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "TRIPODS", quantity = 2 });
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);

        var projection = await client.GetFromJsonAsync<JsonElement>("/api/custody/active");
        var record = Assert.Single(projection.EnumerateArray());
        Assert.Equal("TRIPODS", record.GetProperty("equipmentAssetId").GetString());
        Assert.Equal("ali@equipment.local", record.GetProperty("borrower").GetString());
        Assert.Equal(2, record.GetProperty("outstandingQuantity").GetInt32());
        Assert.Equal("DueSoon", record.GetProperty("dueStatus").GetString());
    }

    [Fact]
    public async Task Pooled_partial_return_reconciles_allocations_and_keeps_custody_open()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        var checkout = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "TRIPODS", quantity = 3 });
        var checkoutBody = await checkout.Content.ReadFromJsonAsync<JsonElement>();
        var custodyId = checkoutBody.GetProperty("id").GetGuid();

        var returned = await client.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
            new { quantity = 2, goodQuantity = 1, damagedQuantity = 1 });
        Assert.Equal(HttpStatusCode.OK, returned.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tripods = await db.Equipment.SingleAsync(item => item.AssetId == "TRIPODS");
        Assert.Equal((2, 1, 1), (tripods.Available, tripods.CheckedOut, tripods.Damaged));
        var custody = await db.CustodyRecords.SingleAsync(item => item.Id == custodyId);
        Assert.True(custody.IsOpen);
        Assert.Equal(1, custody.OutstandingQuantity);
    }

    [Fact]
    public async Task Invalid_and_unauthorized_returns_do_not_mutate_state()
    {
        using var factory = CreateFactory();
        using var borrower = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using var other = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using (var seedScope = factory.Services.CreateScope())
        {
            var users = seedScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var fieldUser = new IdentityUser
            {
                UserName = "other-field@equipment.local",
                Email = "other-field@equipment.local",
                EmailConfirmed = true
            };
            await users.CreateAsync(fieldUser, "Review123!");
            await users.AddToRoleAsync(fieldUser, "Field Staff");
        }
        await LoginAsync(borrower, "ali@equipment.local");
        await LoginAsync(other, "other-field@equipment.local");

        var checkout = await borrower.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "TRIPODS", quantity = 2 });
        var custodyId = (await checkout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await other.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
                new { quantity = 1, goodQuantity = 1 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await borrower.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
                new { quantity = 2, goodQuantity = 1, damagedQuantity = 0 })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await borrower.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
                new { quantity = 3, goodQuantity = 3 })).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tripods = await db.Equipment.SingleAsync(item => item.AssetId == "TRIPODS");
        Assert.Equal((2, 2, 0), (tripods.Available, tripods.CheckedOut, tripods.Damaged));
        Assert.Empty(await db.ReturnEvents.ToListAsync());
    }

    [Fact]
    public async Task Individual_damaged_return_is_unavailable_and_duplicate_is_rejected()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(client, "ali@equipment.local");

        var checkout = await client.PostAsJsonAsync("/api/custody/checkouts",
            new { equipmentAssetId = "CAMERA-DEMO-01" });
        var custodyId = (await checkout.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.OK,
            (await client.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
                new { condition = "Damaged" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync($"/api/custody/{custodyId}/returns",
                new { condition = "Damaged" })).StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var camera = await db.Equipment.SingleAsync(item => item.AssetId == "CAMERA-DEMO-01");
        Assert.Equal((0, 0, 1), (camera.Available, camera.CheckedOut, camera.Damaged));
        Assert.Equal("Damaged", camera.Condition);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"equipment-tracking-custody-{Guid.NewGuid():N}.db");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("DatabasePath", databasePath));
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Review123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static TimeZoneInfo ResolveKarachiTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        }
    }
}
