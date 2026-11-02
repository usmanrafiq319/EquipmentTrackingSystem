---
title: 'Use the responsive custody workflow'
type: 'feature'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
baseline_commit: 'b01dd5168a745f782e9bf0b49492dbfab3d3a6f9'
review_loop_iteration: 0
context:
  - 'C:/Users/usman/EquipmentTrackingSystem/_bmad-output/implementation-artifacts/epic-2-context.md'
  - 'C:/Users/usman/EquipmentTrackingSystem/_bmad-output/implementation-artifacts/spec-2-3-return-equipment-with-immutable-events.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The existing Angular catalogue exposes equipment but provides no practical checkout or return workflow, so authenticated users cannot complete the core custody path from a phone or desktop.

**Approach:** Extend the existing Angular app with compact checkout and return forms backed by the completed custody endpoints, add the smallest authenticated active-custody projection needed by the UI, and reload catalogue/custody state after every successful mutation or conflict.

## Boundaries & Constraints

**Always:** Use server-owned identity, authorization, validation, due status, and persisted projections; show actual borrower, outstanding quantity, and Due Soon/Overdue labels; support individual Good/Damaged returns and pooled Good/Damaged partial allocations; expose accessible labels, keyboard focus, responsive layout, loading, success, validation, 401/403, and 409 states; never claim a mutation succeeded before a successful response.

**Never:** Add approval decisions, damage review, corrections, history pages, new frontend architecture, or Epic 4 features; bypass existing checkout/return services; accept a client-selected borrower in the common field-user flow.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Individual checkout | Available unrestricted item, authenticated user, future due time | Create custody for signed-in user and refresh catalogue/custody | Show server error; reload on 409 |
| Pooled checkout | Available pooled item and positive quantity within availability | Create requested outstanding quantity and refresh projections | Validate whole positive quantity; show 409 and reload |
| Individual return | Active authorized custody, Good or Damaged | Close custody, show success, refresh authoritative state | Show 403/409 action and reload |
| Pooled partial return | Active authorized custody, Good + Damaged allocations equal positive return quantity | Reduce outstanding quantity and keep open until zero | Reject invalid or excessive allocation visibly |
| Network or validation failure | Request fails or form is invalid | Keep form state and explain next action | No success state or guessed projection |

</frozen-after-approval>

## Code Map

- `src/equipment-tracking-web/src/main.ts` -- existing standalone Angular catalogue/auth shell; add custody models, forms, calls, state refresh, and status messaging here.
- `src/equipment-tracking-web/src/styles.css` -- existing responsive/accessibility styles; extend for compact cards, forms, status and mobile layouts.
- `src/EquipmentTracking.Api/Program.cs` -- existing checkout/return mappings and equipment projection; add the smallest authenticated active-custody read projection with borrower and server due status.
- `src/EquipmentTracking.Api/Features/Custody/CheckoutCommandService.cs` -- authoritative checkout validation, default due time, identity and conflict semantics; do not duplicate or weaken it.
- `src/EquipmentTracking.Api/Features/Custody/ReturnCommandService.cs` -- authoritative return authorization/allocation/conflict semantics; do not duplicate or weaken it.
- `tests/EquipmentTracking.IntegrationTests/CustodyTests.cs` -- existing custody API coverage; add focused projection/read coverage only if required by the new endpoint.
- `src/equipment-tracking-web/package.json` -- Angular build is the available frontend verification command; no test runner is currently configured.

## Tasks & Acceptance

**Execution:**
- [x] `src/EquipmentTracking.Api/Program.cs` -- expose active custody records with borrower identity and server-derived due status -- give the UI authoritative data without adding a separate subsystem.
- [x] `src/equipment-tracking-web/src/main.ts` -- implement checkout/return workflow, error mapping, focus-friendly forms, and refreshes -- cover the common custody journeys.
- [x] `src/equipment-tracking-web/src/styles.css` -- style responsive custody controls and accessible status states -- preserve phone/desktop usability.
- [x] `tests/EquipmentTracking.IntegrationTests/CustodyTests.cs` -- verify the active-custody projection contract -- prevent UI-facing API drift.

**Acceptance Criteria:**
- Given an authenticated user viewing available equipment or active custody, when checkout or return opens, then only required fields plus optional location/note are shown and server-derived due labels are visible.
- Given valid unrestricted individual or pooled checkout input, when submitted, then the existing endpoint records the signed-in borrower and the UI shows success with refreshed authoritative availability.
- Given an authorized individual or pooled return, when submitted with valid Good/Damaged allocation, then the UI shows success and refreshed outstanding custody/inventory, including partial pooled returns.
- Given validation, authorization, network, or 409 failure, when the API responds, then the UI shows an actionable state and reloads authoritative data for conflicts without claiming success.
- Given seeded demo data and an authenticated session, when a standard checkout or return is performed on phone or desktop, then the common path remains usable within 30 seconds.

## Implementation Notes

- Added an authenticated active-custody projection with borrower identity and server-derived due status.
- Added responsive checkout and individual/pooled return forms with authoritative refreshes after success and conflicts.
- Added accessible labels, live status/error messaging, validation, and responsive custody card styling.

## Verification

**Commands:**
- `dotnet test .\tests\EquipmentTracking.IntegrationTests\EquipmentTracking.IntegrationTests.csproj --framework net8.0` -- expected: all API tests pass.
- `dotnet build .\src\EquipmentTracking.Api\EquipmentTracking.Api.csproj --framework net8.0 --no-restore` -- expected: API build succeeds.
- `npm run build` in `src\equipment-tracking-web` -- expected: Angular production build succeeds.
- `.\scripts\start-review.ps1 -NoLaunch` -- expected: seeded API and Angular reviewer startup succeeds.

**Result:** 19 integration tests passed; the API build completed with 0 warnings and 0 errors; the Angular production build succeeded; seeded reviewer startup passed. `npm install` reported existing dependency audit findings (31 vulnerabilities) without changing the manifest.
