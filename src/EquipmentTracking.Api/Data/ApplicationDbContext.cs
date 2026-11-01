using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EquipmentTracking.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<ApprovalRule> ApprovalRules => Set<ApprovalRule>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Equipment>().HasIndex(x => x.AssetId).IsUnique();
        builder.Entity<Equipment>().Property(x => x.Name).HasMaxLength(200);
        builder.Entity<Equipment>().Property(x => x.AssetId).HasMaxLength(100);
        builder.Entity<ApprovalRule>().HasIndex(x => new { x.EquipmentAssetId, x.ApproverEmail }).IsUnique();
    }

}

public class Equipment
{
    public int Id { get; set; }
    public required string AssetId { get; set; }
    public required string Name { get; set; }
    public string TrackingMode { get; set; } = "Individual";
    public int Total { get; set; } = 1;
    public int Available { get; set; } = 1;
    public int CheckedOut { get; set; }
    public int Damaged { get; set; }
    public string Condition { get; set; } = "Good";
}

public class ApprovalRule
{
    public int Id { get; set; }
    public required string EquipmentAssetId { get; set; }
    public required string ApproverEmail { get; set; }
}
