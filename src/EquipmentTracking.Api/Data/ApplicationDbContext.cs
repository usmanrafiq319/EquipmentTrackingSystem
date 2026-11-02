using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EquipmentTracking.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<ApprovalRule> ApprovalRules => Set<ApprovalRule>();
    public DbSet<CustodyRecord> CustodyRecords => Set<CustodyRecord>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ReturnEvent> ReturnEvents => Set<ReturnEvent>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Equipment>().HasIndex(x => x.AssetId).IsUnique();
        builder.Entity<Equipment>().Property(x => x.Name).HasMaxLength(200);
        builder.Entity<Equipment>().Property(x => x.AssetId).HasMaxLength(100);
        builder.Entity<ApprovalRule>().HasIndex(x => new { x.EquipmentAssetId, x.ApproverEmail }).IsUnique();
        builder.Entity<Equipment>().Property(x => x.Version).IsConcurrencyToken();
        builder.Entity<CustodyRecord>().HasIndex(x => new { x.EquipmentAssetId, x.IsOpen, x.IsIndividual })
            .IsUnique()
            .HasFilter("\"IsOpen\" = 1 AND \"IsIndividual\" = 1");
        builder.Entity<ApprovalRequest>().HasIndex(x => new { x.EquipmentAssetId, x.BorrowerId, x.Quantity, x.Status });
        builder.Entity<ReturnEvent>().HasOne<CustodyRecord>()
            .WithMany()
            .HasForeignKey(x => x.CustodyRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ReturnEvent>().Property(x => x.Note).HasMaxLength(2000);
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
    public int Version { get; set; }
}

public class ApprovalRule
{
    public int Id { get; set; }
    public required string EquipmentAssetId { get; set; }
    public required string ApproverEmail { get; set; }
}

public class CustodyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EquipmentAssetId { get; set; }
    public required string BorrowerId { get; set; }
    public required string CheckoutActorId { get; set; }
    public DateTimeOffset CheckedOutAt { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public string? DeclaredLocation { get; set; }
    public int Quantity { get; set; }
    public int OutstandingQuantity { get; set; }
    public bool IsIndividual { get; set; }
    public bool IsOpen { get; set; } = true;
    public string Status { get; set; } = "Active";
    public Guid? ApprovalRequestId { get; set; }
}

public class ApprovalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string EquipmentAssetId { get; set; }
    public required string BorrowerId { get; set; }
    public required string RequestedById { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset DueAt { get; set; }
    public string? DeclaredLocation { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending";
    public string? DecisionReason { get; set; }
}

public class ReturnEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustodyRecordId { get; set; }
    public required string EquipmentAssetId { get; set; }
    public required string ActorId { get; set; }
    public DateTimeOffset ReturnedAt { get; set; }
    public int Quantity { get; set; }
    public string Condition { get; set; } = "Good";
    public int GoodQuantity { get; set; }
    public int DamagedQuantity { get; set; }
    public string? Note { get; set; }
}
