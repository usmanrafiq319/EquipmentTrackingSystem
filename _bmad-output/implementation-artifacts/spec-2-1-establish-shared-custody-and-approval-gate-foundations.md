---
title: 'Establish shared custody and approval-gate foundations'
type: 'feature'
created: '2026-11-02'
status: 'awaiting-approval'
route: 'bmad-agent-dev'
baseline_commit: 'c350101c28ca362abc882891813c702f89b4ff21'
---

## Intent

Implement the Story 2.1 backend foundation so every current and future checkout
entry point reaches one server-owned custody command boundary. The command must
enforce actor roles, reload authoritative SQLite state inside one write
transaction, validate individual/pooled availability and quantities, validate
due/location input, and reject restricted equipment unless a matching approved
request exists.

Pending restricted requests are represented without custody or inventory
reservation. Manager/Admin authority permits the management path but never
substitutes for the configured approver.

## Acceptance Criteria

### AC-2.1.1 — Shared transactional command boundary

For individual or pooled checkout, the application command:

1. Receives the authenticated actor and explicit command data.
2. Begins one SQLite write transaction.
3. Reloads current equipment, approval, and relevant custody/request rows.
4. Enforces role, due/location, tracking-mode, quantity, availability, and
   restricted-state rules using the reloaded state.
5. Mutates only through the command owner and commits once; any validation,
   database, or concurrency failure rolls back all mutations.

The HTTP handler only maps request/identity data to the command and maps typed
results to `400`, `401`, `403`, `404`, `409`, or success responses.

### AC-2.1.2 — Non-bypassable restricted approval gate

Any self-service, office-assisted, manager-created, or future kit checkout
command for restricted equipment is rejected unless a matching approved
approval request exists for the equipment, borrower, quantity, and requested
checkout. The rejection creates no custody, inventory change, audit success
record, or success-shaped response.

All paths must call the same command/gate implementation; no role, endpoint, or
direct persistence path may bypass it.

### AC-2.1.3 — Pending requests never reserve equipment

A pending restricted request stores request facts only. It creates no custody
record and does not change `Available`, `CheckedOut`, or individual availability.
Approval lifecycle persistence is limited to the foundation needed by this
story; approval decision UI and full approval screens remain deferred.

### AC-2.1.4 — Server-enforced authorization and validation

The server derives the actor from the authenticated Identity session. A
borrower may not select another borrower through self-service. Role checks are
server-enforced for self-service and office/manager-assisted commands.

Invalid or non-positive pooled quantities, fractional/non-numeric quantities,
unavailable assets, damaged/unavailable individual assets, invalid due values,
and malformed locations return explicit validation/conflict responses without
partial state.

## Scope boundaries

Included:

- Domain/application persistence for custody records and the approval-request
  state required to enforce the gate.
- Shared checkout command service and one public checkout/request boundary.
- SQLite transaction and concurrency handling, database constraints needed to
  protect active individual custody and quantity invariants.
- Focused backend integration/unit tests.
- Directly related migration and documentation updates.

Excluded:

- Angular checkout UI.
- Returns and immutable return events.
- Approval decision screens and later approval-history UX.
- Corrections, damage review, history, survey-kit orchestration, and unrelated
  Epic 2/3 work.

## Implementation map

- `src/EquipmentTracking.Api/Data/ApplicationDbContext.cs`: entities,
  relationships, constraints, concurrency/version mapping, and transaction
  model.
- `src/EquipmentTracking.Api/Features/Custody/`: shared checkout command,
  validation, authorization, result types, and HTTP boundary.
- `src/EquipmentTracking.Api/Features/Approvals/`: restricted request state and
  gate policy used by checkout.
- `src/EquipmentTracking.Api/Program.cs`: dependency registration and thin
  endpoint mapping; preserve Epic 1 routes.
- `src/EquipmentTracking.Api/Migrations/`: additive EF migration only.
- `tests/EquipmentTracking.IntegrationTests/`: restricted rejection,
  invalid quantity, unavailable asset, authorization, concurrency, pending
  non-reservation, and rollback coverage.

## Verification plan

1. Run focused integration tests for Story 2.1.
2. Run the full `net8.0` integration suite to protect Epic 1.
3. Build the API with `--framework net8.0`.
4. Run the Angular production build without changing the Angular surface.
5. Run `scripts/start-review.ps1 -NoLaunch` and verify readiness, seed, login,
   and catalogue behavior.

## Deferred or incomplete criteria

None are intentionally deferred within Story 2.1. Approval decision UI,
checkout UI, and actual checkout success flows belong to later stories and will
remain out of scope; the backend foundation must still expose the shared
boundary required by those later paths.
