---
status: final
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-EquipmentTrackingSystem-2026-10-31/prd.md
  - _bmad-output/planning-artifacts/architecture/architecture-EquipmentTrackingSystem-2026-11-01/ARCHITECTURE-SPINE.md
  - _bmad-output/planning-artifacts/briefs/brief-EquipmentTrackingSystem-2026-10-31/brief.md
  - reference/client-requirements.md
  - DECISIONS.md
---

# EquipmentTrackingSystem - Epic Breakdown

## Overview

This document defines the prioritized epics and implementation-ready stories for
the two-day EquipmentTrackingSystem MVP. It preserves the approved Must,
Should, and Deferred scope and the finalized architecture.

## Requirements Inventory

### Functional Requirements

- FR-1: Named local users can authenticate; custody and audit actions are attributed to the acting user and timestamp.
- FR-2: Users can have bounded Field Staff, Office Staff, Approver, and Manager/Admin roles, including multiple roles.
- FR-3: Authorized office users can create individually tracked equipment with a unique asset ID.
- FR-4: Authorized office users can create pooled equipment with a positive integer total quantity; required seed data includes four tripods and six field tablets.
- FR-5: Authenticated users can search equipment and view availability, custodian, due state, approval state, condition, and unresolved-custody indicators.
- FR-6: Authenticated users can check out available individual or pooled equipment with tomorrow as the editable default due date and optional declared location; pooled quantities must be positive and available.
- FR-7: Restricted equipment requires a configured approver decision before checkout; approval rechecks availability atomically and rejection creates no checkout.
- FR-8: Office Staff or Manager/Admin can create or correct custody records with actor/reason attribution, preserved originals, audit evidence, and all normal availability/approval rules.
- FR-9: Authorized actors can fully or partially return equipment with required Good/Damaged condition; pooled returns allocate quantities and create immutable Return Events.
- FR-10: Authorized users can review pooled damaged stock; only Manager/Admin can restore or retire damaged individual assets; reviews are audited.
- FR-11: Due Soon and Overdue indicators are derived from expected return time and shown in current custody views; overdue does not mutate custody facts.
- FR-12: Authenticated users can search custody history by equipment or period, including checkout, return events, approvals, corrections, actors, timestamps, quantities, conditions, and notes.
- FR-13: Meaningful approvals, rejections, corrections, and reviews create append-only, attributable Audit Records.
- FR-14: Optional predefined survey-kit checkout may atomically process existing components, but it is deferred unless every Must journey passes first.

### NonFunctional Requirements

- NFR-1 Usability: responsive phone-sized and desktop UI; common authenticated checkout and return each complete in under 30 seconds in seeded data.
- NFR-2 Correctness: transactional availability changes prevent duplicate individual custody and pooled over-allocation.
- NFR-3 Security: authentication and server-enforced role/approver checks protect actions; no real credentials or secrets are committed.
- NFR-4 Auditability: meaningful approval and correction history is append-only and attributable to named users.
- NFR-5 Run readiness: one documented command or short accurate sequence starts the app and makes deterministic demo journeys runnable within five minutes.
- NFR-6 Accessibility: core controls have readable labels, usable keyboard focus, and sufficient contrast for the demonstrated flows.

### Additional Requirements

- Use a single deployable modular monolith with feature-oriented application services.
- Preserve the approved stack: ASP.NET Core Web API `net8.0`, EF Core 8, SQLite, ASP.NET Core Identity 8, and Angular 21 standalone components.
- Angular is a thin responsive client; backend application services own all custody, approval, return, correction, and damage-review mutations.
- Keep individual and pooled inventory as separate models. Maintain `Available + CheckedOut + Damaged = Total` and pooled checked-out reconciliation with open custody.
- Use relational Custody Records, immutable Return Events, and append-only Audit Records; never overwrite or delete prior custody facts.
- Every availability-changing command uses one SQLite transaction with current-row reload, validation, mutation, audit where required, and commit/rollback behavior.
- Database constraints backstop application checks for unique asset IDs, active individual custody uniqueness, non-negative quantities, foreign keys, and concurrency/version handling.
- Pending approval does not reserve stock. All self-service, office-assisted, manager-created, and future kit paths use the same approval gate; manager authority does not substitute for Hamza's configured drone approval.
- Use ASP.NET Core Identity local seeded users, secure password hashing, role claims, and Angular-compatible bearer/session authentication.
- Seed restricted-equipment rules as data, including the drone/Hamza rule; no rule-management UI/API is required for Must.
- Use UTC persistence, configured `Asia/Karachi` business-time calculations/display, one injected server clock, and server-derived due labels.
- API uses `/api` REST JSON DTOs, problem-details errors, and explicit `400/401/403/404/409` semantics.
- Provide deterministic idempotent demo data, a writable ignored SQLite path, a tested one-command `scripts/start-review.ps1`, explicit .NET 8 framework selection, and README credentials/reset guidance.
- Organize backend features for Auth, Equipment, Custody, Approvals, History, and Administration; organize Angular by corresponding features with shared API/auth/error/responsive services.
- Keep survey-kit shortcuts, external notifications, GPS, offline sync, procurement, depreciation, maintenance, SSO, and full missing-item incidents deferred.

### UX Design Requirements

No separate UX design contract was provided. The PRD and architecture require:

- Responsive phone-sized and desktop layouts for the core workflows.
- Fast, low-friction checkout and return with readable labels, keyboard focus, and clear validation/error states.
- Separate visible concepts for availability, approval pending, due/overdue, damage, and unresolved custody.
- Server-derived status labels and explicit conflict/error feedback rather than client-side success assumptions.

### FR Coverage Map

FR-1: Epic 1, Story 1.2 - named Identity login and attribution  
FR-2: Epic 1, Story 1.2 - bounded roles and authorization  
FR-3: Epic 1, Story 1.3 - individual asset creation  
FR-4: Epic 1, Story 1.3 - pooled equipment creation and required seed quantities  
FR-5: Epic 1, Story 1.4 - availability and custodian projection  
FR-6: Epic 2, Story 2.2 - shared checkout command and availability rules  
FR-7: Epic 2, Story 2.2; Epic 3, Story 3.1 - enforced gate and approval lifecycle  
FR-8: Epic 3, Story 3.3 - safe office correction  
FR-9: Epic 2, Story 2.3 - full/partial returns and immutable Return Events  
FR-10: Epic 3, Story 3.2 - damage review and quarantine  
FR-11: Epic 2, Story 2.2 - server-derived due and overdue labels  
FR-12: Epic 3, Story 3.4 - custody and audit history  
FR-13: Epic 3, Story 3.1, 3.2, 3.3 - append-only audit evidence  
FR-14: Epic 4, Story 4.2 - optional atomic survey-kit shortcut, gated after Must  
NFR-1: Epic 1, Story 1.4; Epic 2, Story 2.4 - responsive and fast flows  
NFR-2: Epic 2, Story 2.2 and 2.3 - transactional mutations and conflict tests  
NFR-3: Epic 1, Story 1.2; Epic 2, Story 2.2; Epic 3, Story 3.1 - server enforcement  
NFR-4: Epic 3, Stories 3.1-3.4 - attributable append-only history  
NFR-5: Epic 1, Story 1.1 - tested one-command startup  
NFR-6: Epic 1, Story 1.4; Epic 2, Story 2.4 - labels, focus, contrast, errors  

## Epic List

### Epic 1: Open the System and See Trusted Inventory
Users can start the local application, sign in with named seeded accounts, and
see searchable individual and pooled equipment availability.
**FRs covered:** FR-1, FR-2, FR-3, FR-4, FR-5.

### Epic 2: Take and Return Equipment Safely
Field staff can check out available equipment and return all or part of it while
preserving due status, condition, custody, and quantity invariants. The core
checkout service enforces restricted-equipment approval even before the full
approval workflow UI is delivered.
**FRs covered:** FR-6, FR-7, FR-9, FR-11.

### Epic 3: Control Restricted Equipment and Explain Custody
Approvers and office/manager users can decide restricted requests, review
damage, make safe corrections, and answer who has what from history.
**FRs covered:** FR-7, FR-8, FR-10, FR-12, FR-13.

### Epic 4: Post-MVP Refinements (Gated)
Optional refinements are attempted only after every Must journey passes.
Survey-kit processing is deferred and is cut first if atomicity cannot be
demonstrated.
**FRs covered:** FR-14 and Should refinements related to FR-5, FR-8, FR-11, FR-12.

## Epic 1: Open the System and See Trusted Inventory

**Goal:** Deliver a runnable vertical slice with the approved stack, local
Identity, deterministic demo data, role-aware login, catalogue setup, and
availability visibility.

### Story 1.1: Start the seeded review environment

As a reviewer,
I want one tested startup command,
So that I can run the system and reach demo journeys within five minutes.

**Acceptance Criteria:**

**Given** a supported Windows machine with the documented .NET 8 and Node/Angular 21 prerequisites  
**When** the reviewer runs `scripts/start-review.ps1`  
**Then** dependencies restore, Angular builds, the API uses `net8.0`, SQLite migrations apply, idempotent seed data loads, and the reviewable process starts.

**Given** the startup command is run twice or after the SQLite file is reset  
**When** initialization completes  
**Then** schema and demo data are deterministic without duplicate users, roles, equipment, or approval rules.

**Given** the reviewer follows the README  
**When** they use the documented local demo credentials  
**Then** the app is reachable without an external database, cloud service, secret, or manual database step.

**Dependencies:** None.  
**Test expectations:** PowerShell smoke test or scripted verification of framework selection, build, migration, seed idempotency, health/readiness, and five-minute startup.

### Story 1.2: Sign in with named roles

As a named field, office, approver, or manager user,
I want to sign in and be recognized by my roles,
So that every protected action is attributable and correctly authorized.

**Acceptance Criteria:**

**Given** a seeded valid local credential  
**When** the user submits the login form  
**Then** an authenticated session is established and `/api/auth/me` returns the named user and roles.

**Given** invalid credentials or an unauthenticated request  
**When** the user attempts login or a protected API action  
**Then** login fails safely or the API returns `401` without revealing credential details.

**Given** Field Staff, Office Staff, Approver, and Manager/Admin roles  
**When** each role calls a protected operation  
**Then** server authorization, not only Angular route guards, permits or rejects the operation according to the approved role rules.

**Dependencies:** Story 1.1.  
**Test expectations:** Identity integration tests for valid/invalid login, role claims, `401`, `403`, logout, and action attribution.

### Story 1.3: Create individual and pooled equipment

As an Office Staff or Manager/Admin user,
I want to create individual assets and pooled equipment,
So that the catalogue represents both uniquely identified items and quantity stock.

**Acceptance Criteria:**

**Given** an authorized office user  
**When** they create an individual asset with a name/type and asset ID  
**Then** it starts Good/Available with no open custody and a duplicate asset ID is rejected with `400`.

**Given** an authorized office user  
**When** they create pooled equipment with a positive integer total  
**Then** Available equals Total and Checked-Out and Damaged equal zero; zero, negative, fractional, non-numeric, and unauthorized requests are rejected.

**Given** the deterministic seed runs  
**When** the catalogue is queried  
**Then** it includes four tripods, six field tablets, a usable individual demo asset, and the drone with its restricted rule data.

**Dependencies:** Story 1.2.  
**Test expectations:** Domain and EF integration tests for tracking-mode validation, uniqueness, quantities, foreign keys, authorization, and seed contents.

### Story 1.4: View availability and current custody

As any authenticated user,
I want a searchable availability view,
So that I can answer who has equipment and whether it can be used.

**Acceptance Criteria:**

**Given** authenticated access  
**When** the user searches or opens equipment  
**Then** individual equipment shows availability, custodian, condition, due/overdue, approval, and unresolved-custody indicators; pooled equipment shows Total, Available, Checked-Out, and Damaged quantities without invented asset identities.

**Given** pooled counters or individual state are read  
**When** the response is returned  
**Then** the projection reflects the authoritative database state and preserves `Available + Checked-Out + Damaged = Total`.

**Given** a phone-sized or desktop viewport  
**When** the user navigates the catalogue  
**Then** labels, focus, contrast, loading, empty, validation, and API error states remain usable.

**Dependencies:** Story 1.3.  
**Test expectations:** API projection tests plus Angular component/service tests for search, labels, responsive critical controls, keyboard focus, and error rendering.

## Epic 2: Take and Return Equipment Safely

**Goal:** Deliver the first complete custody loop while making restricted approval
enforcement a non-bypassable backend invariant.

### Story 2.1: Establish shared custody and approval-gate foundations

As the system,
I want one backend command boundary for custody mutations,
So that every checkout path applies the same availability and approval rules.

**Acceptance Criteria:**

**Given** a checkout command for individual or pooled equipment  
**When** the application service handles it  
**Then** it reloads current rows, validates role, availability, quantity, due/location data, and restricted status inside one SQLite transaction.

**Given** equipment is restricted and no matching Approved Approval Request exists  
**When** any self-service, office-assisted, manager-created, or future kit checkout command is submitted  
**Then** the command is rejected and creates no Custody Record, inventory change, or success-shaped response.

**Given** a pending restricted request  
**When** it exists without an approval decision  
**Then** it does not reserve stock or change availability.

**Dependencies:** Epic 1 Stories 1.1-1.3.  
**Test expectations:** Unit/policy tests for gate decisions and integration tests proving every checkout entry point cannot bypass the gate; no approval UI is required yet.

### Story 2.2: Check out available equipment

As a field user,
I want to check out available individual or pooled equipment quickly,
So that custody is recorded with me and an expected return time.

**Acceptance Criteria:**

**Given** an authenticated borrower and available unrestricted individual equipment  
**When** they confirm checkout without choosing a borrower  
**Then** the system records the authenticated borrower, checkout actor/time, editable tomorrow default, optional declared location, and one active custody record.

**Given** available pooled equipment  
**When** the borrower submits a positive integer quantity no greater than Available  
**Then** Available decreases, Checked-Out increases, and the open custody outstanding quantity equals the checkout quantity.

**Given** duplicate individual checkout, invalid quantity, insufficient pooled stock, restricted equipment without approval, or a concurrent conflict  
**When** checkout is attempted  
**Then** the API returns explicit `400` or `409`, rolls back all changes, and requires reload on conflict.

**Dependencies:** Story 2.1 and Epic 1 Story 1.4.  
**Test expectations:** API and transaction/concurrency tests for duplicate individual checkout, pooled over-allocation, due calculation, `409`, and under-30-second seeded flow.

### Story 2.3: Return equipment with immutable events

As a borrower or authorized office actor,
I want to return all or part of a custody record with condition,
So that availability and damage status remain trustworthy.

**Acceptance Criteria:**

**Given** an active individual checkout  
**When** the authorized actor returns it as Good or Damaged with an optional note  
**Then** one immutable Return Event records actor/time/condition, Good restores availability, and Damaged makes the asset unavailable.

**Given** an active pooled checkout  
**When** the actor returns a positive quantity with Good plus Damaged allocation equal to the returned quantity  
**Then** Checked-Out decreases, Good quantity becomes Available, Damaged quantity is quarantined, and a Return Event is appended.

**Given** a pooled partial return or over-return  
**When** the command is submitted  
**Then** the remaining custody stays open for the outstanding quantity, or an invalid over-return returns `400` with no event or state change.

**Dependencies:** Story 2.2.  
**Test expectations:** Domain, API, and transaction tests for mixed-condition partial returns, immutable/multiple events, over-return rollback, reconciliation, and under-30-second seeded flow.

### Story 2.4: Use the responsive custody workflow

As a field user,
I want concise checkout and return screens,
So that I can complete the common flow in seconds from a phone.

**Acceptance Criteria:**

**Given** an authenticated user viewing available equipment or active custody  
**When** they open checkout or return  
**Then** the form exposes only required fields plus optional location/note, shows server-derived due labels, and gives a clear success state with refreshed availability.

**Given** validation, authorization, or `409` conflict failure  
**When** the API responds  
**Then** the UI displays an actionable error and reloads authoritative state without guessing that the mutation succeeded.

**Given** seeded demo data and an already authenticated session  
**When** a user performs standard checkout and return  
**Then** each common path completes in under 30 seconds in the documented test environment.

**Dependencies:** Stories 2.2 and 2.3.  
**Test expectations:** Angular tests and a focused browser/manual timing check for phone/desktop layout, labels, focus, contrast, success, validation, and conflict states.

## Epic 3: Control Restricted Equipment and Explain Custody

**Goal:** Complete the approval, damage review, correction, and history capabilities
needed for office accountability and the seeded drone scenario.

### Story 3.1: Request and decide restricted equipment

As a requester and configured approver,
I want restricted checkout requests to be approved or rejected,
So that the drone cannot be issued without Hamza's recorded decision.

**Acceptance Criteria:**

**Given** a requester submits a drone checkout  
**When** the request is created  
**Then** it returns pending status, captures borrower/equipment/quantity/due/location/rule snapshot, creates no custody, and does not reserve the drone.

**Given** Hamza is the seeded configured approver  
**When** he approves a pending request with availability still valid  
**Then** approval rechecks availability and atomically creates exactly one active custody and an audit/decision record.

**Given** a non-configured user, the requester without configured approver authority, a rejected request, or lost availability  
**When** they attempt to decide or approval fails  
**Then** the API returns `403` or a safe rejection, creates no checkout, and records the decision/reason.

**Dependencies:** Epic 2 Stories 2.1 and 2.2.  
**Test expectations:** API authorization and transaction tests for Hamza-only approval, self-approval rules, rejection, approval-time availability loss, duplicate decisions, and all checkout paths.

### Story 3.2: Review damaged individual and pooled stock

As a Manager/Admin or authorized office reviewer,
I want to restore or retire damaged stock,
So that quarantined equipment returns to service only after an accountable review.

**Acceptance Criteria:**

**Given** a damaged individual asset  
**When** Manager/Admin chooses exactly Restore or Retire with an optional reason  
**Then** the asset becomes Good/Available or permanently Retired/Unavailable and an append-only audit record is written.

**Given** a damaged individual asset and a Field Staff, Office Staff, or Approver actor  
**When** they attempt review  
**Then** the API returns `403` and state is unchanged.

**Given** pooled Damaged Quantity  
**When** an authorized office or manager actor restores a positive quantity not greater than damaged stock  
**Then** Damaged decreases, Available increases, and the review is audited atomically; excessive quantities are rejected.

**Dependencies:** Epic 2 Story 2.3.  
**Test expectations:** Domain/API authorization tests, pooled reconciliation tests, audit immutability tests, and restore/retire lifecycle tests.

### Story 3.3: Correct custody safely

As an Office Staff or Manager/Admin user,
I want to correct an operational custody mistake,
So that the record is usable without erasing accountability or bypassing approval.

**Acceptance Criteria:**

**Given** an authorized correction actor and a non-empty reason  
**When** they submit a correction  
**Then** the original custody facts remain preserved, old/new values and actor/time/reason are appended to audit history, and normal inventory constraints are revalidated transactionally.

**Given** a correction would duplicate an individual checkout, over-allocate pooled stock, or activate restricted equipment without matching approval  
**When** it is submitted  
**Then** it is rejected with no partial state or audit success record.

**Given** Field Staff or an actor without the required role  
**When** they submit a correction  
**Then** the API returns `403`.

**Dependencies:** Stories 2.1-2.3 and Story 3.1.  
**Test expectations:** API authorization, correction audit, rollback, restricted-gate, and pooled reconciliation integration tests.

### Story 3.4: Search custody and decision history

As an office user or manager,
I want to search custody history,
So that I can answer who had equipment and what decisions or returns occurred.

**Acceptance Criteria:**

**Given** an authenticated user selects equipment or a period  
**When** history is queried  
**Then** results show named borrower/actors, checkout time, expected return, due state, every immutable Return Event, conditions/quantities/notes, approvals, corrections, and relevant audits.

**Given** multiple pooled partial returns  
**When** history is displayed  
**Then** events remain individually ordered and outstanding quantity is derived from the original checkout minus all returned quantities.

**Given** a seeded last-month history query  
**When** the reviewer searches  
**Then** named demo actors and timestamps are returned without overwriting newer custody activity.

**Dependencies:** Stories 2.3, 3.1, 3.2, and 3.3.  
**Test expectations:** API projection tests, seed-history smoke test, and Angular tests for filters, empty/error states, and readable history presentation.

## Epic 4: Post-MVP Refinements (Gated)

**Goal:** Add only low-risk Should improvements after Must acceptance scenarios
pass; these stories are optional and must not displace the working MVP.

### Story 4.1: Refine due, filters, and correction presentation (Optional)

As an office user,
I want clearer Due Soon labels and richer filters,
So that routine follow-up is easier after the Must release is stable.

**Acceptance Criteria:**

**Given** all Must journeys pass from clean seeded data  
**When** Due Soon, equipment, and history views are refined  
**Then** the one-calendar-day policy, Overdue precedence, and server-derived values remain unchanged.

**Given** a user filters history or availability  
**When** results load, are empty, or fail  
**Then** the UI provides clear filter state, empty state, and explicit error behavior without changing custody rules.

**Dependencies:** All Epic 1-3 Must stories.  
**Test expectations:** Regression suite for due boundaries, filters, history, and all four minimum journeys.

### Story 4.2: Evaluate atomic survey-kit shortcut (Optional/Deferred)

As a field user,
I want a predefined survey-kit shortcut,
So that I can request known components together only if the operation is safe.

**Acceptance Criteria:**

**Given** every Must journey passes and kit contents are explicitly defined  
**When** a kit request is implemented  
**Then** every component is preflighted through the existing availability and approval services and all component custody records commit atomically.

**Given** any component is unavailable, approval is unresolved, or atomic rollback cannot be demonstrated within the timebox  
**When** the kit path is attempted  
**Then** no component is checked out and the feature remains explicitly Deferred while separate checkout remains available.

**Dependencies:** All Epic 1-3 Must stories and Story 4.1 only if its shared UI changes are adopted.  
**Test expectations:** Transaction rollback and approval/availability integration tests; do not begin this story until the Must regression suite is green.

## Implementation Order and Scope Gate

Implement stories in numeric order. After each story, run its stated tests and
keep the application runnable. The recommended first implementation story is
**Story 1.1: Start the seeded review environment**. The first end-to-end
vertical slice is complete when Stories 1.1-1.4 and 2.1-2.4 pass: a named user
can sign in, see inventory, check out and return individual or pooled
equipment, and the backend cannot bypass restricted approval. Implement Epic 3
before any Epic 4 work. Survey-kit processing and all other Deferred items must
not delay the Must release.
