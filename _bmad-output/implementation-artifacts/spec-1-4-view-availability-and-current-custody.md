---
title: 'View availability and current custody'
type: 'feature'
created: '2026-11-02'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '2e51bf6369064fc2c0c229a8cfb66a62e2a2f4bf'
context:
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\epic-1-context.md'
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\spec-1-3-create-individual-and-pooled-equipment.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Authenticated users can see a basic catalogue, but cannot search it or reliably answer whether an individual asset is available, who has it, or how pooled stock is distributed.

**Approach:** Extend the server-owned equipment projection with explicit availability, condition, approval, and nullable custody/due fields, then render a searchable responsive catalogue and individual detail view in Angular. The projection must be compatible with future checkout records while returning null/empty custody facts until Epic 2 creates authoritative records.

## Boundaries & Constraints

**Always:** Require authentication for catalogue/search/detail reads. Keep the database authoritative: individual availability comes from persisted state; pooled responses expose Total, Available, Checked-Out, and Damaged and preserve `Available + CheckedOut + Damaged = Total`. Return approval restriction data from persisted approval rules. Represent current custodian, due time, due state, and unresolved-custody state as nullable/explicit projection fields; never infer or fabricate them. Keep response fields stable for future custody integration. Preserve existing create, login, seed, authorization, and startup behavior. Provide keyboard-focusable controls, readable labels, responsive phone/desktop layout, loading, empty, validation, and API error states.

**Never:** Implement checkout, return, approval decisions, custody mutations, correction, damage review, or fabricated demo custody. Do not calculate authoritative availability in Angular or create individual identities for pooled quantities.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Search catalogue | Authenticated user and optional case-insensitive query | `GET /api/equipment?search=` returns matching authoritative projections | Empty result is a clear empty state |
| Individual projection | Individual equipment with no checkout record | Availability/condition/approval are shown; custodian, due, and unresolved custody are null/false as appropriate | API failure renders an alert without stale fabricated data |
| Pooled projection | Pooled equipment counters | Total, Available, Checked-Out, Damaged are shown and invariant holds | Invalid persisted counters are not repaired client-side; API returns explicit server error |
| Detail and auth | Authenticated existing/missing asset, anonymous request | Detail returns projection or `404`; anonymous list/detail returns `401` | UI shows safe retry/error state |

</frozen-after-approval>

## Code Map

- `src/EquipmentTracking.Api/Program.cs` -- existing authenticated list/detail endpoints and `EquipmentResponse`; add search handling and a stable availability/custody projection without changing startup or mutation routes.
- `src/EquipmentTracking.Api/Data/ApplicationDbContext.cs` -- current Equipment and ApprovalRule persistence; reuse stored counters/rules and do not add checkout tables before Epic 2.
- `src/EquipmentTracking.Api/Data/SeedData.cs` -- deterministic inventory and drone approval rule; preserve seed values.
- `src/equipment-tracking-web/src/main.ts` -- standalone authenticated shell and catalogue loading; add query input, projection labels, detail selection, and explicit loading/empty/error states.
- `src/equipment-tracking-web/src/styles.css` -- currently minimal global styling; add responsive catalogue/detail layout and visible focus states.
- `tests/EquipmentTracking.IntegrationTests/EquipmentTests.cs` -- existing authenticated catalogue/create coverage; add search, projection shape, invariant, approval, and unauthorized/error assertions.
- `tests/EquipmentTracking.IntegrationTests/AuthTests.cs` -- preserve existing authenticated session and `401` patterns.
- `src/equipment-tracking-web/package.json` -- production build is the available Angular verification command; no Angular test runner is configured.

## Tasks & Acceptance

**Execution:**
- [x] `src/EquipmentTracking.Api/Program.cs` -- implement authenticated searchable projection and stable nullable custody/due fields, with server-side invariant validation -- keep future checkout integration authoritative.
- [x] `src/equipment-tracking-web/src/main.ts` and `styles.css` -- implement searchable catalogue/detail rendering with labels, focus, responsive layout, and loading/empty/error states -- do not derive custody facts.
- [x] `tests/EquipmentTracking.IntegrationTests/EquipmentTests.cs` -- cover authenticated search/detail, individual and pooled projection fields, approval restriction, quantity invariant, and safe unauthorized/not-found behavior.
- [x] `README.md` -- document the availability projection and explicitly defer checkout/return workflows.

**Acceptance Criteria:**
- Given authenticated access, when a user searches or opens equipment, then individual equipment shows availability, condition, approval restriction, current custodian, due/overdue, and unresolved-custody indicators, with absent future custody represented as no current custodian and no due state.
- Given pooled equipment is returned, then Total, Available, Checked-Out, and Damaged are shown from authoritative backend values and `Available + CheckedOut + Damaged = Total`.
- Given a phone-sized or desktop viewport, when the user navigates/searches the catalogue, then labels, keyboard focus, contrast, loading, empty, validation, and API error states remain usable.
- Given an anonymous request or missing asset, when list/detail is requested, then the API returns `401` or `404` and the UI presents a safe, actionable state.

## Implementation Notes

Added server-side case-insensitive catalogue search and validated quantity
invariants. Equipment projections now expose explicit nullable custody/due
fields, approval restriction, and individual availability without inventing
checkout data. The Angular shell provides search, selectable detail views,
responsive styling, and safe loading, empty, validation, and API error states.

## Verification

API and integration verification passed; the Angular production build passed.

**Commands:**
- `dotnet test tests/EquipmentTracking.IntegrationTests/EquipmentTracking.IntegrationTests.csproj --framework net8.0` -- expected: all API/auth/startup/equipment tests pass.
- `dotnet build src/EquipmentTracking.Api/EquipmentTracking.Api.csproj --framework net8.0` -- expected: successful API build.
- `npm run build` in `src/equipment-tracking-web` -- expected: Angular production build succeeds.
- `powershell -ExecutionPolicy Bypass -File scripts/start-review.ps1 -NoLaunch` -- expected: startup, health, auth, seed, and catalogue verification remain green.

## Review Triage Log

| Finding | Verdict | Evidence |
|---|---|---|
| Filtered catalogue search could hide invalid persisted quantity rows | patch | Validation now loads and checks all persisted equipment before applying the search filter; integration coverage asserts a filtered list still returns `500`. |
| Catalogue reload failures could leave stale equipment and detail visible | patch | Failed and network reloads now clear both catalogue and selected detail before rendering the error state. |
| Empty search results used the generic empty-catalogue message | patch | The template now distinguishes no matches from no equipment. |
| Anonymous detail authorization was not verified | patch | Integration coverage now asserts `401` for anonymous detail requests. |
