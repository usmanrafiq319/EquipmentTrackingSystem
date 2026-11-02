---
title: 'Return equipment with immutable events'
type: 'feature'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
baseline_commit: '972a7e19fca835574201db80bec7129a51102bb8'
review_loop_iteration: 0
context:
  - 'C:/Users/usman/EquipmentTrackingSystem/_bmad-output/implementation-artifacts/epic-2-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Custody can be established but equipment cannot yet be returned with trustworthy condition, partial-return, and immutable history semantics.

**Approach:** Add one authenticated return command and thin API boundary using the existing custody transaction conventions. Persist append-only return events, update individual or pooled inventory atomically, keep original checkout facts unchanged, and expose refreshed authoritative projections.

## Boundaries & Constraints

**Always:** Derive the actor from the authenticated session; allow the borrower or Office Staff, Manager/Admin, and authorized equivalent office actor; validate current custody and inventory inside one serializable SQLite transaction; use UTC timestamps; preserve `Available + CheckedOut + Damaged = Total`; reject invalid, duplicate, over-return, unauthorized, and concurrency-conflict requests without partial state; keep all prior checkout and return facts immutable.

**Never:** Build the Angular return UI, damage-review workflow, history screens, correction paths, survey-kit behavior, or optional Epic 4 features; bypass the shared checkout model or approval behavior; accept client-supplied actor identity or mutate projections outside the command.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Individual good | Open individual custody, condition Good | 200; close custody; Available +1; one immutable Good event | N/A |
| Individual damaged | Open individual custody, condition Damaged | 200; close custody; Damaged +1 and unavailable; one event | N/A |
| Pooled partial | Open pooled custody; positive return with Good + Damaged = returned quantity | 200; reduce outstanding/CheckedOut; increase corresponding counters; record allocation event; custody remains open if outstanding remains | N/A |
| Invalid or duplicate | Closed custody, zero/negative quantity, bad condition/allocation, or over-return | No state or event change | 400 or 409 |
| Unauthorized | Non-borrower Field Staff or unauthenticated actor | No state or event change | 401 or 403 |

</frozen-after-approval>

## Code Map

- `src/EquipmentTracking.Api/Data/ApplicationDbContext.cs` -- add return-event persistence and immutable relationship; preserve equipment version concurrency and custody indexes.
- `src/EquipmentTracking.Api/Features/Custody/CheckoutCommandService.cs` -- reuse role, transaction, SQLite conflict, and result conventions; do not alter checkout behavior.
- `src/EquipmentTracking.Api/Program.cs` -- add thin authenticated return request/response mapping and register the service; preserve existing endpoints and projections.
- `tests/EquipmentTracking.IntegrationTests/CustodyTests.cs` -- add focused API/inventory/event/authorization/rollback coverage using isolated seeded factories.
- `src/EquipmentTracking.Api/Migrations/` -- add the additive EF migration required by the event entity.

## Tasks & Acceptance

**Execution:**
- [x] Add immutable `ReturnEvent` persistence and migration without changing checkout facts.
- [x] Implement an atomic return command for individual and pooled custody, including authorization, allocation validation, outstanding quantities, UTC attribution, and conflict mapping.
- [x] Map the authenticated return API and refresh existing equipment projections from persisted state.
- [x] Add focused integration tests for all matrix cases and invariant preservation.

**Acceptance Criteria:**
- Given active individual custody, when the borrower or authorized office actor returns Good or Damaged with an optional note, then the asset and one immutable event reflect that return.
- Given active pooled custody, when positive Good and Damaged allocations equal the returned quantity, then Checked-Out and Available/Damaged counters reconcile and the event records the allocation.
- Given partial pooled returns, when each valid return is submitted, then outstanding quantity equals original quantity minus all returned quantities and the record closes only at zero.
- Given invalid, duplicate, over-return, unauthorized, or concurrency failure, when return is submitted, then the API returns an explicit error with no inventory, custody, or event mutation.
- Given a successful return, when equipment or custody is read, then the response reflects authoritative committed state.

## Implementation Notes

- Added `ReturnEvent` persistence and additive migration.
- Return commands use the existing serializable SQLite transaction and equipment version token; individual damaged returns persist `Condition = "Damaged"` and remain unavailable.
- Added pooled allocation, note-length, and inventory-invariant validation after review.

## Spec Change Log

## Review Triage Log

- `medium` -- pooled condition, oversized note, and pre-mutation inventory validation gaps were verified and fixed in `ReturnCommandService`.
- `false` -- transaction-startup conflict claim was not present in the cited implementation; existing transaction error mapping covers database update/SQLite conflicts.
- `false` -- projection-refresh gap was disproven by the existing API projection path and the passing integration/startup suite.
- `low` -- additional concurrency and authorized-office tests were suggested; existing checkout concurrency conventions and return authorization coverage remain intact, with no deferred acceptance criterion.

## Verification

**Commands:**
- `dotnet test .\tests\EquipmentTracking.IntegrationTests\EquipmentTracking.IntegrationTests.csproj --framework net8.0` -- expected: all integration tests pass.
- `dotnet build .\src\EquipmentTracking.Api\EquipmentTracking.Api.csproj --framework net8.0 --no-restore` -- expected: build succeeds.
- `.\scripts\start-review.ps1 -NoLaunch` -- expected: readiness and seeded review startup succeed.

**Result:** Integration tests passed (18/18); API build succeeded with 0 warnings and 0 errors; final `start-review.ps1 -NoLaunch` passed including Angular build, migrations, and seeded startup. Review findings were triaged with no unresolved high-severity issue.
