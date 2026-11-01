using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EquipmentTracking.Api.Data;

public static class SeedData
{
    public const string DemoEmail = "reviewer@equipment.local";
    public const string DemoPassword = "Review123!";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Field Staff", "Office Staff", "Approver", "Manager/Admin" })
        {
            if (!await roles.RoleExistsAsync(role))
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Unable to seed role {role}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
            }
        }

        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        await EnsureUserAsync(users, DemoEmail, "Manager/Admin");
        await EnsureUserAsync(users, "ali@equipment.local", "Field Staff");
        await EnsureUserAsync(users, "saima@equipment.local", "Office Staff");
        await EnsureUserAsync(users, "hamza@equipment.local", "Approver");

        var db = services.GetRequiredService<ApplicationDbContext>();
        var equipment = new[]
        {
            new Equipment { AssetId = "TRIPODS", Name = "Tripod", TrackingMode = "Pooled", Total = 4, Available = 4 },
            new Equipment { AssetId = "FIELD-TABLETS", Name = "Field Tablet", TrackingMode = "Pooled", Total = 6, Available = 6 },
            new Equipment { AssetId = "CAMERA-DEMO-01", Name = "Demo Camera", TrackingMode = "Individual" },
            new Equipment { AssetId = "DRONE-001", Name = "Drone", TrackingMode = "Individual", Condition = "Restricted" }
        };
        foreach (var item in equipment)
        {
            if (!await db.Equipment.AnyAsync(existing => existing.AssetId == item.AssetId))
                db.Equipment.Add(item);
        }

        if (!await db.ApprovalRules.AnyAsync(rule =>
            rule.EquipmentAssetId == "DRONE-001" && rule.ApproverEmail == "hamza@equipment.local"))
        {
            db.ApprovalRules.Add(new ApprovalRule
            {
                EquipmentAssetId = "DRONE-001",
                ApproverEmail = "hamza@equipment.local"
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task EnsureUserAsync(UserManager<IdentityUser> users, string email, string role)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await users.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to seed demo user {email}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
        else if (!await users.CheckPasswordAsync(user, DemoPassword))
        {
            throw new InvalidOperationException($"Seeded demo user {email} does not have the documented local password.");
        }

        if (!await users.IsInRoleAsync(user, role))
        {
            var result = await users.AddToRoleAsync(user, role);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Unable to assign demo role {role}: {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
    }
}
