---
title: 'Start the seeded review environment'
type: 'feature'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '148397e968eb4ef0266da1d98bdec22a657ec4b9'
context:
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\epic-1-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repository contains finalized product and architecture decisions but no runnable application, so a reviewer cannot reach a deterministic demo environment.

**Approach:** Create the approved ASP.NET Core 8 and Angular 21 local application foundation, with EF Core SQLite migrations, deterministic Identity/demo seed data, and a single PowerShell startup command that restores, builds, initializes, verifies, and starts the review process.

## Boundaries & Constraints

**Always:** Target `net8.0` explicitly; use EF Core 8, SQLite, ASP.NET Core Identity, Angular 21 standalone components, a writable ignored database path, idempotent initialization, local invented credentials, and a health/readiness check. Keep the process local and document exact versions, credentials, reset guidance, and startup behavior.

**Never:** Implement later story workflows such as login UI/API, equipment CRUD, availability screens, checkout, return, approvals, corrections, history, or damage review. Do not require cloud services, an external database, production secrets, or manual database setup.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| First startup | Clean checkout and no SQLite file | Dependencies restore, Angular production build completes, migration/schema and deterministic seed apply, health endpoint responds, review process starts | Non-zero exit with actionable command output |
| Repeat startup | Existing initialized SQLite file | Migration and seed are idempotent; no duplicate roles, users, equipment, or approval rules | Non-zero exit if verification detects drift |
| Reset startup | Database file removed | Schema and the same deterministic seed are recreated | Non-zero exit if reset path is invalid |
| Unsupported toolchain | Wrong .NET/Node version or missing dependency | Script stops before starting a misleading process | Explicit prerequisite error |

</frozen-after-approval>

## Code Map

- `src/EquipmentTracking.Api/` -- new ASP.NET Core host, Identity/EF infrastructure, health endpoint, migrations, and deterministic seed boundary; no later-story feature endpoints.
- `src/equipment-tracking-web/` -- new Angular 21 standalone shell and production build configuration; no later-story workflow UI.
- `tests/EquipmentTracking.IntegrationTests/` -- startup, migration, seed idempotency, and health verification against temporary SQLite.
- `scripts/start-review.ps1` -- reviewer entry point; must explicitly pass `--framework net8.0` and coordinate frontend build, database initialization, verification, and API launch.
- `README.md` -- update startup, versions, local credentials, database reset, and scope notes without documenting unimplemented later stories as available.
- `.gitignore` -- ignore generated SQLite/database and build output while preserving source migrations and lockfiles.

## Tasks & Acceptance

**Execution:**
- [x] `src/EquipmentTracking.Api/` -- scaffold the `net8.0` API, EF Core 8 SQLite/Identity infrastructure, migration, health endpoint, and idempotent demo seed -- establish the local runtime boundary.
- [x] `src/equipment-tracking-web/` -- scaffold Angular 21 standalone production shell with pinned dependencies and API-compatible build output -- prove the approved client stack builds.
- [x] `tests/EquipmentTracking.IntegrationTests/` -- add focused startup, migration, seed repeatability, and health tests -- verify AC behavior without implementing later stories.
- [x] `scripts/start-review.ps1` -- implement prerequisite checks, restore/build/migrate/test orchestration, database path setup, and review process startup -- provide the one-command reviewer path.
- [x] `README.md` and `.gitignore` -- document and protect the local review environment -- make setup repeatable and secret-free.

**Acceptance Criteria:**
- Given supported Windows .NET 8 and Node/Angular 21 prerequisites, when `scripts/start-review.ps1` runs, then dependencies restore, Angular builds, the API targets `net8.0`, SQLite schema/migrations apply, deterministic seed data loads, verification passes, and the reviewable process starts.
- Given the startup command is run twice or after the SQLite file is reset, when initialization completes, then schema and demo data are deterministic without duplicate seeded records.
- Given the reviewer follows the README, when they use documented local demo credentials and startup command, then the app is reachable without an external database, cloud service, secret, or manual database step.

## Implementation Notes

- Added persisted `ApprovalRule` seed data for `DRONE-001` and `hamza@equipment.local`.
- Seeded pooled quantities as four tripods and six field tablets, plus four named local demo accounts.
- Added database-backed health checks, truthful readiness status, local EF tool restoration, and fatal native-command handling.
- Integration tests now start two application factories against the same SQLite file to verify repeatable initialization.

## Verification

**Commands:**
- `dotnet build src/EquipmentTracking.Api/EquipmentTracking.Api.csproj --framework net8.0` -- expected: successful API build.
- `dotnet test tests/EquipmentTracking.IntegrationTests/EquipmentTracking.IntegrationTests.csproj --framework net8.0` -- expected: startup, migration, seed, and health tests pass.
- `npm ci` and `npm run build` in `src/equipment-tracking-web` -- expected: Angular 21 production build succeeds.
- `powershell -ExecutionPolicy Bypass -File scripts/start-review.ps1 -NoLaunch` -- expected: restore/build/migration/seed/health verification succeeds without starting a second persistent process.

## Review Triage Log

- blind-hunter: patch -- local `dotnet-ef` restoration was missing; added manifest restoration and restored the selected `dotnet-tools.json`.
- blind-hunter: patch -- Node prerequisite accepted versions broader than the documented 24.x contract; narrowed the check.
- blind-hunter: patch -- npm directory changes could leak on failure; wrapped the location change in `try/finally`.
- blind-hunter: patch -- health was a constant success and readiness returned 200 for false; added a database health check and 503 readiness failure.
- blind-hunter: patch -- role creation failures were ignored; seed now throws explicit errors.
- blind-hunter: defer -- authentication endpoints and authorization are Story 1.2 scope and remain intentionally absent.
- blind-hunter: defer -- mutable seeded-user password and data-drift repair are outside Story 1.1's reset/idempotency contract.
- blind-hunter: defer -- approval-rule foreign keys and CORS hardening are later infrastructure concerns not required by this local foundation.
- edge-case-hunter: patch -- database paths without a directory caused startup failure; directory creation is now guarded.
- edge-case-hunter: defer -- concurrent multi-process seeding is outside the single-process local deployment boundary.
- edge-case-hunter: patch -- seeded credentials now fail explicitly if an existing local account has drifted from the documented password.
- edge-case-hunter: patch -- Node version check now matches the pinned 24.x requirement.
- edge-case-hunter: defer -- launch-process exit reporting and live health polling are not needed for the blocking review process or Story 1.1 acceptance.
- edge-case-hunter: defer -- expanded seed-drift assertions and relational approval foreign keys belong to later catalogue/approval stories.
- verification-gap: patch -- repeatability test now creates two application factories against the same SQLite file.
- verification-gap: patch -- readiness and health endpoint checks run against the initialized test application.
