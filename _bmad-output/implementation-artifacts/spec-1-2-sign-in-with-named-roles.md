---
title: 'Sign in with named roles'
type: 'feature'
created: '2026-11-01'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4fa9fe26d69dc67321c0ad57d18bc88d8aaceb02'
context:
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\epic-1-context.md'
  - 'C:\Users\usman\EquipmentTrackingSystem\_bmad-output\implementation-artifacts\spec-1-1-start-seeded-review-environment.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 1.1 seeds named users and roles, but the application has no authentication flow, so reviewers cannot establish an attributable session or prove server-side authorization.

**Approach:** Add ASP.NET Core Identity cookie authentication with REST login/logout/current-user endpoints, role-protected API behavior, and a minimal Angular login/session shell that uses the seeded local accounts. Preserve the existing startup, SQLite migration, seed, health, and review-summary behavior.

## Boundaries & Constraints

**Always:** Use ASP.NET Core Identity’s password hashing and role claims; return safe `401` responses for invalid or missing credentials; expose the authenticated user and roles through `/api/auth/me`; enforce authorization on the server; keep the browser session same-origin and local; preserve all Story 1.1 tests and startup behavior.

**Never:** Implement equipment CRUD, availability views, checkout, return, approvals, corrections, history, damage review, external identity, real credentials, or client-only authorization. Do not expose password hashes or credential details in errors.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Valid login | Seeded email and `Review123!` | `200`, authenticated session, named user and roles available from `/api/auth/me` | No password or hash in response |
| Invalid login | Unknown email or incorrect password | `401` with a generic failure | Do not reveal whether the account exists |
| Current user without session | No auth cookie | `401` | No user details returned |
| Logout | Authenticated session | Session is invalidated and subsequent `/api/auth/me` is `401` | Safe repeated logout |
| Role enforcement | Authenticated user calls role-protected review action | Allowed only when the server role requirement matches; otherwise `403` | Angular guards cannot bypass server decision |

</frozen-after-approval>

## Code Map

- `src/EquipmentTracking.Api/Program.cs` -- existing host, Identity registration, middleware, and minimal endpoints; extend without changing Story 1.1 startup sequencing.
- `src/EquipmentTracking.Api/Data/SeedData.cs` -- existing deterministic users/roles and password; reuse seeded accounts and role names.
- `src/equipment-tracking-web/src/main.ts` -- existing standalone shell; replace placeholder with login/session state and explicit authentication errors only.
- `tests/EquipmentTracking.IntegrationTests/StartupTests.cs` -- existing Story 1.1 health/seed tests; preserve them and add auth integration coverage in a separate focused test file.
- `README.md` -- existing local account documentation; add login endpoint/UI usage without claiming later workflows.

## Tasks & Acceptance

**Execution:**
- [x] `src/EquipmentTracking.Api/Program.cs` and auth support files -- configure Identity cookie authentication, login/logout/me endpoints, safe status codes, and a role-protected review endpoint -- make authentication and authorization server authoritative.
- [x] `src/equipment-tracking-web/src/main.ts` and related client config -- add a minimal login form, session projection, logout control, and safe error states -- let reviewers sign in without implementing later feature screens.
- [x] `tests/EquipmentTracking.IntegrationTests/AuthTests.cs` -- cover valid/invalid login, `/api/auth/me`, logout, `401`, `403`, role claims, and action attribution/session identity -- verify the full acceptance matrix.
- [x] `README.md` -- document the Story 1.2 login behavior and seeded role accounts -- keep local-only credential guidance accurate.

**Acceptance Criteria:**
- Given a seeded valid local credential, when the user submits the login form, then an authenticated session is established and `/api/auth/me` returns the named user and roles.
- Given invalid credentials or an unauthenticated request, when the user attempts login or a protected API action, then login fails safely or the API returns `401` without revealing credential details.
- Given Field Staff, Office Staff, Approver, and Manager/Admin roles, when each role calls a protected operation, then server authorization, not only Angular route guards, permits or rejects the operation according to the approved role rules.

## Implementation Notes

## Verification

**Commands:**
- `dotnet test tests/EquipmentTracking.IntegrationTests/EquipmentTracking.IntegrationTests.csproj --framework net8.0` -- expected: Story 1.1 and Story 1.2 integration tests pass.
- `dotnet build src/EquipmentTracking.Api/EquipmentTracking.Api.csproj --framework net8.0` -- expected: successful API build.
- `npm ci` and `npm run build` in `src/equipment-tracking-web` -- expected: Angular production build succeeds.
- `powershell -ExecutionPolicy Bypass -File scripts/start-review.ps1 -NoLaunch` -- expected: existing startup verification remains green.

All commands passed on 2026-11-01; the integration suite reports 4 passing tests.

## Review Triage Log

| Finding | Verdict | Evidence |
|---|---|---|
| Frontend logout ignored failed requests | medium | Real failure path; logout now checks the response and reports network/server errors without clearing a live session. |
| Frontend logout had no rejection handling | medium | Same root cause as the failed-request finding; fixed with explicit `try/catch` handling. |
| Unauthenticated protected action was not tested | medium | Acceptance matrix requires `401`; added an assertion before login. |
| Review action actor attribution was not tested | medium | The endpoint returned the actor but the test asserted only status; added response-body verification. |
| `/api/auth/me` roles were not tested | medium | Session restoration must expose roles; added the expected `Manager/Admin` assertion. |
| Login body omission could produce framework `400` | false | The matrix covers invalid credentials and missing credential values; the endpoint explicitly returns `401` for empty typed fields, while malformed JSON is framework input validation outside the specified request shape. |
| Current-user non-401 response was not surfaced by the shell | low | This is a degraded-service presentation detail not required by the story's session or authorization contract; no user-visible correctness failure in the specified paths. |
| Initial session discovery could race with login | low | The shell has no concurrent login interaction before initialization completes in the reachable review UI; adding coordination would add complexity without a demonstrated story failure. |
| Mutating cookie endpoints lacked antiforgery tokens | defer | This is a broader deployment hardening concern; the approved story explicitly constrains the session to same-origin local review use and no external hosting topology is in scope. |
| Temporary integration databases were not deleted | low | Cleanup affects test workspace hygiene only and does not affect the story's runtime behavior; the proposed cleanup is unrelated to the acceptance contract. |
