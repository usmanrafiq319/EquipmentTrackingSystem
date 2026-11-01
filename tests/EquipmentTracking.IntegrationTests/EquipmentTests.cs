using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EquipmentTracking.IntegrationTests;

public class EquipmentTests
{
    [Fact]
    public async Task Authenticated_catalogue_contains_seeded_equipment_and_tracking_fields()
    {
        using var client = CreateClient();
        await LoginAsync(client, "reviewer@equipment.local");

        var response = await client.GetAsync("/api/equipment");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.NotNull(items);
        Assert.Equal(4, items!.Length);
        Assert.Equal(4, items.Single(item => item.GetProperty("assetId").GetString() == "TRIPODS").GetProperty("total").GetInt32());
        Assert.Equal(6, items.Single(item => item.GetProperty("assetId").GetString() == "FIELD-TABLETS").GetProperty("total").GetInt32());
        Assert.Equal("Good", items.Single(item => item.GetProperty("assetId").GetString() == "CAMERA-DEMO-01").GetProperty("condition").GetString());
        Assert.Equal("Restricted", items.Single(item => item.GetProperty("assetId").GetString() == "DRONE-001").GetProperty("condition").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/equipment/DRONE-001")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/equipment/MISSING")).StatusCode);
    }

    [Fact]
    public async Task Office_staff_can_create_individual_and_pooled_equipment_with_initial_states()
    {
        using var client = CreateClient();
        await LoginAsync(client, "saima@equipment.local");

        var individual = await client.PostAsJsonAsync("/api/equipment",
            new { name = "Handheld GPS", assetId = "GPS-001", trackingMode = "Individual" });
        Assert.Equal(HttpStatusCode.Created, individual.StatusCode);
        var individualBody = await individual.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Good", individualBody.GetProperty("condition").GetString());
        Assert.Equal(1, individualBody.GetProperty("total").GetInt32());
        Assert.Equal(1, individualBody.GetProperty("available").GetInt32());
        Assert.Equal(0, individualBody.GetProperty("checkedOut").GetInt32());
        Assert.Equal(0, individualBody.GetProperty("damaged").GetInt32());

        var pooled = await client.PostAsJsonAsync("/api/equipment",
            new { name = "Safety Vests", assetId = "VESTS-001", trackingMode = "Pooled", total = 12 });
        Assert.Equal(HttpStatusCode.Created, pooled.StatusCode);
        var pooledBody = await pooled.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(12, pooledBody.GetProperty("total").GetInt32());
        Assert.Equal(12, pooledBody.GetProperty("available").GetInt32());
        Assert.Equal(0, pooledBody.GetProperty("checkedOut").GetInt32());
        Assert.Equal(0, pooledBody.GetProperty("damaged").GetInt32());

        var persisted = await client.GetFromJsonAsync<JsonElement[]>("/api/equipment");
        Assert.Contains(persisted!, item => item.GetProperty("assetId").GetString() == "GPS-001");
        Assert.Equal(12, persisted!.Single(item => item.GetProperty("assetId").GetString() == "VESTS-001").GetProperty("available").GetInt32());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/equipment",
            new { name = "Duplicate", assetId = "GPS-001", trackingMode = "Individual" })).StatusCode);
    }

    [Fact]
    public async Task Create_validation_and_role_authorization_are_server_enforced()
    {
        using var factory = CreateFactory();
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/equipment")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/equipment",
            new { name = "Nope", assetId = "NOPE", trackingMode = "Individual" })).StatusCode);

        using var fieldStaff = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(fieldStaff, "ali@equipment.local");
        Assert.Equal(HttpStatusCode.Forbidden, (await fieldStaff.PostAsJsonAsync("/api/equipment",
            new { name = "Nope", assetId = "NOPE", trackingMode = "Individual" })).StatusCode);

        using var officeStaff = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(officeStaff, "saima@equipment.local");
        foreach (var total in new object[] { 0, -1, 1.5, "not-a-number" })
        {
            var response = await officeStaff.PostAsJsonAsync("/api/equipment",
                new { name = "Invalid Pool", assetId = $"INVALID-{Guid.NewGuid():N}", trackingMode = "Pooled", total });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        Assert.Equal(HttpStatusCode.BadRequest, (await officeStaff.PostAsJsonAsync("/api/equipment",
            new { name = " ", assetId = "INVALID-NAME", trackingMode = "Individual" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await officeStaff.PostAsJsonAsync("/api/equipment",
            new { name = "Missing ID", assetId = " ", trackingMode = "Individual" })).StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"equipment-tracking-equipment-{Guid.NewGuid():N}.db");
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseSetting("DatabasePath", databasePath));
    }

    private static HttpClient CreateClient()
    {
        var factory = CreateFactory();
        return factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Review123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }
}
