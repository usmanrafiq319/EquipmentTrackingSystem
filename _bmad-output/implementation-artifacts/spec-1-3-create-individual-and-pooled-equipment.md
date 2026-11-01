---
title: 'Create individual and pooled equipment'
type: 'feature'
created: '2026-11-01'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: ''
context:
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\epic-1-context.md'
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\spec-1-2-sign-in-with-named-roles.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.2 provides named authorization, but the catalogue is limited to seed data and cannot represent new individual assets or pooled stock. Office Staff and Manager/Admin users need a safe catalogue setup path that preserves the distinct tracking semantics of each equipment type.

**Approach:** Add authenticated equipment catalogue read/create behavior backed by the existing SQLite model, with server-side validation and role authorization. Extend the minimal Angular shell with a focused equipment creation form and catalogue view without implementing custody, availability workflows, or later review features.

## Boundaries & Constraints

**Always:** Treat the database as authoritative; require Office Staff or Manager/Admin for creation; allow authenticated users to query the catalogue; enforce unique asset IDs and valid tracking-mode-specific values on the server; initialize individual records as Good/Available with zero custody and pooled records with Available equal to Total and zero Checked-Out/Damaged; return client-safe `400`, `401`, `403`, and `404` responses as applicable; preserve Story 1.1 and 1.2 startup, seed, authentication, and authorization behavior.

**Never:** Implement checkout, return, custody, approvals, corrections, history, damage review, full availability projections, external identity, bulk import, or client-only authorization. Do not invent individual identities for pooled equipment or expose database internals in validation errors.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Create individual | Authorized Office Staff or Manager/Admin; name/type and unique asset ID | `201`; Individual, Good, Available, Total 1, Checked-Out 0, Damaged 0 | Missing/blank name or asset ID returns `400` |
| Duplicate individual ID | Authorized create with an existing asset ID | No duplicate row | `400` with safe validation message |
| Create pooled | Authorized creator; name/type, unique asset ID, positive integer total | `201`; Pooled, Available equals Total, Checked-Out/Damaged zero | Zero, negative, fractional, or non-numeric total returns `400` |
| Unauthorized create | Unauthenticated or Field Staff/Approver caller | No catalogue mutation | `401` or `403` |
| Catalogue query | Authenticated user | `200` with seeded and newly created equipment records and tracking fields | Unauthenticated request returns `401` |

</frozen-after-approval>

## Code Map

- `src/EquipmentTracking.Api/Program.cs` -- existing minimal API, Identity middleware, and role policy endpoints; add catalogue query/create endpoints without changing auth or startup ordering.
- `src/EquipmentTracking.Api/Data/ApplicationDbContext.cs` -- existing `Equipment` model and unique `AssetId` index; reuse the model and centralize create-state invariants at the API boundary.
- `src/EquipmentTracking.Api/Data/SeedData.cs` -- existing deterministic four-record catalogue and drone approval rule; preserve and verify its seed contents.
- `src/equipment-tracking-web/src/main.ts` -- existing standalone authenticated shell; add a small catalogue projection and role-appropriate create form with loading, validation, and API error states.
- `tests/EquipmentTracking.IntegrationTests/AuthTests.cs` -- existing role/session integration patterns; preserve Story 1.2 coverage and reuse authenticated clients where useful.
- `tests/EquipmentTracking.IntegrationTests/EquipmentTests.cs` -- new focused API/EF integration coverage for validation, authorization, seed contents, and persisted initial states.
- `README.md` -- document the Story 1.3 catalogue endpoints and local role behavior without claiming later workflows.

## Tasks & Acceptance

**Execution:**
- [ ] `src/EquipmentTracking.Api/Program.cs` and `src/EquipmentTracking.Api/Data/ApplicationDbContext.cs` -- implement authenticated catalogue query/create endpoints, request validation, unique-ID handling, and tracking-mode state invariants -- make catalogue setup server-authoritative.
- [ ] `src/EquipmentTracking.Api/Data/SeedData.cs` -- preserve and verify deterministic tripod, tablet, demo asset, drone, and restricted-rule seed data -- keep reset and repeat startup behavior stable.
- [ ] `src/equipment-tracking-web/src/main.ts` -- add a minimal authenticated catalogue view and Office Staff/Manager create form with safe validation and API errors -- expose only Story 1.3 behavior.
- [ ] `tests/EquipmentTracking.IntegrationTests/EquipmentTests.cs` and existing test files -- cover catalogue reads, seeded records, individual/pooled creation, invalid quantities, duplicate IDs, `401`, `403`, and persisted initial state -- verify every matrix row.
- [ ] `README.md` -- document catalogue query/create behavior and role restrictions -- keep local review instructions accurate.

**Acceptance Criteria:**
- Given an authorized Office Staff user, when they create an individual asset with a name/type and asset ID, then it starts Good/Available with no open custody and duplicate asset IDs return `400`.
- Given an authorized Office Staff user, when they create pooled equipment with a positive integer total, then Available equals Total and Checked-Out and Damaged equal zero; zero, negative, fractional, non-numeric, and unauthorized requests are rejected.
- Given the deterministic seed runs, when the catalogue is queried, then it includes four tripods, six field tablets, a usable individual demo asset, and the drone with its restricted rule data.

## Implementation Notes

## Verification

**Commands:**
- `dotnet test tests/EquipmentTracking.IntegrationTests/EquipmentTracking.IntegrationTests.csproj --framework net8.0` -- expected: Story 1.1, 1.2, and 1.3 integration tests pass.
- `dotnet build src/EquipmentTracking.Api/EquipmentTracking.Api.csproj --framework net8.0` -- expected: successful API build.
- `npm ci` and `npm run build` in `src/equipment-tracking-web` -- expected: Angular production build succeeds.
- `powershell -ExecutionPolicy Bypass -File scripts/start-review.ps1 -NoLaunch` -- expected: startup, migration, seed, health, auth, and catalogue verification remain green.
