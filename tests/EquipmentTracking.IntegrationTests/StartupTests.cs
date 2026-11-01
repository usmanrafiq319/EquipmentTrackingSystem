using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace EquipmentTracking.IntegrationTests;

public class StartupTests
{
    private static WebApplicationFactory<Program> CreateFactory(string databasePath)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting("DatabasePath", databasePath));
    }

    [Fact]
    public async Task Health_endpoint_is_available()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"equipment-tracking-tests-{Guid.NewGuid():N}.db");
        using (var factory = CreateFactory(databasePath))
        {
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await client.GetAsync("/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task Seed_is_repeatable_and_contains_expected_records()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"equipment-tracking-tests-{Guid.NewGuid():N}.db");
        string first;
        using (var firstFactory = CreateFactory(databasePath))
        using (var firstClient = firstFactory.CreateClient())
        {
            first = await firstClient.GetStringAsync("/api/review/summary");
        }

        using (var secondFactory = CreateFactory(databasePath))
        using (var secondClient = secondFactory.CreateClient())
        {
            var second = await secondClient.GetStringAsync("/api/review/summary");
            Assert.Contains("\"users\":4", first);
            Assert.Contains("\"equipment\":4", first);
            Assert.Contains("\"approvalRules\":1", first);
            Assert.Equal(first, second);
        }
    }
}
