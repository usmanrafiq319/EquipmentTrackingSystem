using System.Data;
using EquipmentTracking.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquipmentTracking.Api.Features.Custody;

public sealed record ReturnCommand(
    Guid CustodyId,
    int? Quantity,
    string? Condition,
    int? GoodQuantity,
    int? DamagedQuantity,
    string? Note);

public sealed record ReturnCommandResult(
    int StatusCode,
    string? Error = null,
    CustodyRecord? Custody = null,
    ReturnEvent? Event = null);

public sealed class ReturnCommandService(
    ApplicationDbContext db,
    UserManager<IdentityUser> users,
    TimeProvider clock)
{
    private static readonly string[] OfficeRoles = ["Office Staff", "Manager/Admin", "Approver"];

    public async Task<ReturnCommandResult> ExecuteAsync(
        IdentityUser actor,
        ReturnCommand command,
        CancellationToken cancellationToken = default)
    {
        var roles = await users.GetRolesAsync(actor);
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        try
        {
            var custody = await db.CustodyRecords
                .SingleOrDefaultAsync(item => item.Id == command.CustodyId, cancellationToken);
            if (custody is null)
                return Invalid(StatusCodes.Status404NotFound, "Custody record was not found.");
            if (!custody.IsOpen || custody.OutstandingQuantity <= 0)
                return Invalid(StatusCodes.Status409Conflict, "The custody record is already closed.");
            if (custody.BorrowerId != actor.Id &&
                !roles.Any(role => OfficeRoles.Contains(role, StringComparer.Ordinal)))
                return Invalid(StatusCodes.Status403Forbidden, "The current user cannot return this custody record.");

            var equipment = await db.Equipment
                .SingleOrDefaultAsync(item => item.AssetId == custody.EquipmentAssetId, cancellationToken);
            if (equipment is null)
                return Invalid(StatusCodes.Status409Conflict, "The equipment for this custody record was not found.");

            if (!TryValidateAllocation(custody, command, out var quantity, out var good, out var damaged, out var condition, out var error))
                return Invalid(StatusCodes.Status400BadRequest, error!);

            equipment.CheckedOut -= quantity;
            equipment.Available += good;
            equipment.Damaged += damaged;
            equipment.Version++;
            custody.OutstandingQuantity -= quantity;
            if (custody.OutstandingQuantity == 0)
            {
                custody.IsOpen = false;
                custody.Status = "Returned";
            }

            var returnEvent = new ReturnEvent
            {
                CustodyRecordId = custody.Id,
                EquipmentAssetId = equipment.AssetId,
                ActorId = actor.Id,
                ReturnedAt = clock.GetUtcNow(),
                Quantity = quantity,
                Condition = condition,
                GoodQuantity = good,
                DamagedQuantity = damaged,
                Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim()
            };
            db.ReturnEvents.Add(returnEvent);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ReturnCommandResult(StatusCodes.Status200OK, Custody: custody, Event: returnEvent);
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

    private static bool TryValidateAllocation(
        CustodyRecord custody,
        ReturnCommand command,
        out int quantity,
        out int good,
        out int damaged,
        out string condition,
        out string? error)
    {
        quantity = command.Quantity ?? 1;
        good = command.GoodQuantity ?? 0;
        damaged = command.DamagedQuantity ?? 0;
        condition = command.Condition?.Trim() ?? string.Empty;
        if (condition.Length > 0)
            condition = char.ToUpperInvariant(condition[0]) + condition[1..].ToLowerInvariant();
        error = null;

        if (custody.IsIndividual)
        {
            if ((command.Quantity.HasValue && command.Quantity.Value != 1) ||
                command.GoodQuantity.HasValue || command.DamagedQuantity.HasValue ||
                condition is not ("Good" or "Damaged"))
            {
                error = "Individual returns require quantity 1 and condition Good or Damaged.";
                return false;
            }
            good = condition == "Good" ? 1 : 0;
            damaged = condition == "Damaged" ? 1 : 0;
        }
        else
        {
            if (quantity <= 0 || good < 0 || damaged < 0 || good + damaged != quantity)
            {
                error = "Pooled return allocations must be non-negative and equal the returned quantity.";
                return false;
            }
            if (quantity > custody.OutstandingQuantity)
            {
                error = "The returned quantity exceeds the outstanding custody quantity.";
                return false;
            }
            condition = good > 0 && damaged > 0 ? "Mixed" : damaged > 0 ? "Damaged" : "Good";
        }

        if (quantity <= 0 || quantity > custody.OutstandingQuantity)
        {
            error = "The returned quantity is invalid or exceeds the outstanding custody quantity.";
            return false;
        }
        return true;
    }

    private static ReturnCommandResult Invalid(int statusCode, string error) => new(statusCode, error);
}
