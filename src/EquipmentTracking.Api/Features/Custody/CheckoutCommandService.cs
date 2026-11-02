using System.Data;
using EquipmentTracking.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentTracking.Api.Features.Custody;

public sealed record CheckoutCommand(
    string EquipmentAssetId,
    string? BorrowerEmail,
    int? Quantity,
    DateTimeOffset? DueAt,
    string? DeclaredLocation);

public sealed record CheckoutCommandResult(
    int StatusCode,
    string? Error = null,
    CustodyRecord? Custody = null);

public sealed class CheckoutCommandService(
    ApplicationDbContext db,
    UserManager<IdentityUser> users,
    TimeProvider clock)
{
    private static readonly string[] CheckoutRoles = ["Field Staff", "Office Staff", "Approver", "Manager/Admin"];

    public async Task<CheckoutCommandResult> ExecuteAsync(
        IdentityUser actor,
        CheckoutCommand command,
        CancellationToken cancellationToken = default)
    {
        var roles = await users.GetRolesAsync(actor);
        if (!roles.Any(role => CheckoutRoles.Contains(role, StringComparer.Ordinal)))
            return Invalid(StatusCodes.Status403Forbidden, "The current user cannot create custody records.");

        var borrower = actor;
        if (!string.IsNullOrWhiteSpace(command.BorrowerEmail))
        {
            if (!roles.Contains("Office Staff") && !roles.Contains("Manager/Admin"))
                return Invalid(StatusCodes.Status403Forbidden, "Only office or manager actors may choose a borrower.");

            borrower = await users.FindByEmailAsync(command.BorrowerEmail.Trim());
            if (borrower is null)
                return Invalid(StatusCodes.Status400BadRequest, "The borrower was not found.");
        }

        var now = clock.GetUtcNow();
        var dueAt = command.DueAt.HasValue
            ? CustodyTimePolicy.Normalize(command.DueAt.Value)
            : CustodyTimePolicy.DefaultDueAt(now);
        if (dueAt <= now)
            return Invalid(StatusCodes.Status400BadRequest, "Due time must be in the future.");
        if (command.DeclaredLocation is { Length: > 200 })
            return Invalid(StatusCodes.Status400BadRequest, "Declared location must be 200 characters or fewer.");
        if (string.IsNullOrWhiteSpace(command.EquipmentAssetId))
            return Invalid(StatusCodes.Status400BadRequest, "Equipment asset ID is required.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var equipment = await db.Equipment
                .SingleOrDefaultAsync(item => item.AssetId == command.EquipmentAssetId.Trim(), cancellationToken);
            if (equipment is null)
                return Invalid(StatusCodes.Status404NotFound, "Equipment was not found.");

            if (!IsValidQuantity(command.Quantity, equipment.TrackingMode, out var quantity))
                return Invalid(StatusCodes.Status400BadRequest, "Quantity must be a positive integer and match the equipment tracking mode.");

            if (equipment.TrackingMode == "Individual" &&
                (equipment.Available != 1 || IsUnavailableCondition(equipment.Condition)))
                return Invalid(StatusCodes.Status409Conflict, "The individual equipment is unavailable.");
            if (equipment.TrackingMode == "Pooled" && equipment.Available < quantity)
                return Invalid(StatusCodes.Status409Conflict, "Insufficient equipment is available.");

            ApprovalRequest? approval = null;
            if (await db.ApprovalRules.AnyAsync(rule => rule.EquipmentAssetId == equipment.AssetId, cancellationToken))
            {
                approval = await db.ApprovalRequests
                    .SingleOrDefaultAsync(request =>
                        request.EquipmentAssetId == equipment.AssetId &&
                        request.BorrowerId == borrower.Id &&
                        request.Quantity == quantity &&
                        request.Status == "Approved", cancellationToken);
                if (approval is null)
                    return Invalid(StatusCodes.Status400BadRequest, "Restricted equipment requires an approved request.");
            }

            if (equipment.TrackingMode == "Individual" &&
                await db.CustodyRecords.AnyAsync(record =>
                    record.EquipmentAssetId == equipment.AssetId && record.IsOpen, cancellationToken))
                return Invalid(StatusCodes.Status409Conflict, "The individual equipment already has open custody.");

            equipment.Available -= quantity;
            equipment.CheckedOut += quantity;
            equipment.Version++;
            var custody = new CustodyRecord
            {
                EquipmentAssetId = equipment.AssetId,
                BorrowerId = borrower.Id,
                CheckoutActorId = actor.Id,
                CheckedOutAt = clock.GetUtcNow(),
                DueAt = dueAt,
                DeclaredLocation = string.IsNullOrWhiteSpace(command.DeclaredLocation)
                    ? null
                    : command.DeclaredLocation.Trim(),
                Quantity = quantity,
                OutstandingQuantity = quantity,
                IsIndividual = equipment.TrackingMode == "Individual",
                ApprovalRequestId = approval?.Id
            };
            db.CustodyRecords.Add(custody);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new CheckoutCommandResult(StatusCodes.Status201Created, Custody: custody);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Invalid(StatusCodes.Status409Conflict, "The equipment changed concurrently. Reload and retry.");
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqliteException { SqliteErrorCode: 5 or 6 or 19 })
        {
            await transaction.RollbackAsync(cancellationToken);
            return Invalid(StatusCodes.Status409Conflict, "The equipment is being updated. Reload and retry.");
        }
    }

    private static bool IsValidQuantity(int? requested, string trackingMode, out int quantity)
    {
        quantity = requested ?? 1;
        return quantity > 0 &&
            (trackingMode == "Individual" ? requested is null or 1 : requested is > 0);
    }

    private static bool IsUnavailableCondition(string condition) =>
        string.Equals(condition, "Damaged", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(condition, "Retired", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(condition, "Unavailable", StringComparison.OrdinalIgnoreCase);

    private static CheckoutCommandResult Invalid(int statusCode, string error) =>
        new(statusCode, error);
}
