---
title: Equipment Tracking System
status: final
created: 2026-10-31
updated: 2026-11-01
---

# PRD: Equipment Tracking System

## 0. Document Purpose

This PRD defines the smallest reliable, testable custody workflow for Horizon Survey Services that can be implemented and demonstrated within a two-day MVP build window. It is for product review and downstream UX, architecture, and story planning; it deliberately does not prescribe application code or frontend/backend technology. The Product Brief, `reference/client-requirements.md`, and approved decisions in `DECISIONS.md` are the source of truth. Any inference is marked `[ASSUMPTION]`.

## 1. Vision

Horizon Survey Services needs one dependable answer to “who has what?” without turning field work into administration. The Equipment Tracking System gives field staff a fast way to check equipment out and return it, while giving office staff a current view of custody, availability, due status, condition, and recent history.

The MVP succeeds by making the custody loop trustworthy before adding breadth: named users, individual or quantity-based inventory, approval for restricted equipment, clear due/overdue indicators, condition at return, and auditable history. It is a responsive web application usable from a phone-sized screen and a desktop browser. External reminders, GPS, offline support, and maintenance workflows are intentionally deferred.

## 2. Target User

### 2.1 Jobs To Be Done

- **Field staff:** When taking equipment to a job, record custody quickly so the team knows the equipment is with me and when it is expected back.
- **Field staff:** When returning equipment, report its condition so the next person does not discover damage at the worksite.
- **Office staff:** When someone asks where equipment is, answer from the current system instead of calling several people.
- **Approver:** When restricted equipment is requested, approve or reject it with a recorded decision.
- **Manager/Admin:** When reviewing accountability, see what was out, who held it, and which records were corrected.

### 2.2 Non-Users (MVP)

- External customers and vendors.
- Anonymous users without a named local account.
- Users needing GPS, offline synchronization, procurement, depreciation, or maintenance management.

### 2.3 Key User Journeys

- **UJ-1. Ali checks out a standard individual asset for tomorrow's site visit.**
  - **Persona + context:** Ali is field staff preparing for a one-day survey.
  - **Entry state:** Ali is authenticated on a responsive web app and sees available equipment.
  - **Path:** Select an available individual asset; accept the editable tomorrow due-date default; optionally choose a declared location category and description; confirm checkout.
  - **Climax:** The system confirms the checkout, identifies Ali as borrower, records the timestamp, and changes the asset to unavailable with a due date.
  - **Resolution:** Ali can see the active custody record and return it later.
  - **Edge case:** If the asset became unavailable concurrently, the checkout is rejected without creating a partial record.

- **UJ-2. Hamza approves a drone request before the borrower receives it.**
  - **Persona + context:** A field staff member needs the drone; Hamza is the configured approver and licensed operator.
  - **Entry state:** A drone checkout request is pending approval and does not reserve the drone.
  - **Path:** Hamza opens pending requests; reviews requester, equipment, due date, and optional location; approves or rejects.
  - **Climax:** Approval rechecks availability and atomically creates the active checkout, or rejection closes the request without checkout.
  - **Resolution:** The borrower sees the resulting approved checkout or rejection; the decision is in history and audit records.

- **UJ-3. Saima answers “who has it?” and checks last month's history.**
  - **Persona + context:** Saima is office staff responding to a status call.
  - **Entry state:** Saima opens the inventory or history view.
  - **Path:** Search/select an asset or pooled equipment type; view current custodian, quantity available, due/overdue state, condition or unresolved custody; filter recent history by item or period.
  - **Climax:** The system shows the current custody answer and historical checkout/return records with actors and timestamps.
  - **Resolution:** Saima can answer the caller without contacting multiple field staff.

- **UJ-4. Ali returns equipment and reports damage.**
  - **Persona + context:** Ali returns an individual asset after site work.
  - **Entry state:** Ali has an active custody record.
  - **Path:** Open active custody; choose Return; select Good or Damaged; optionally enter a note; confirm.
  - **Climax:** Good makes the asset available; Damaged makes it unavailable for review and preserves the note.
  - **Resolution:** The custody record closes with return actor/time and condition; missing equipment remains an open custody record until separately resolved.

- **UJ-5. Saima creates a new asset or pooled stock item.**
  - **Persona + context:** Saima is setting up inventory for a newly acquired or newly classified item.
  - **Entry state:** Saima has the equipment-management role.
  - **Path:** Enter equipment name/type; choose Individual or Quantity tracking; for Individual enter a unique asset ID; for Quantity enter an initial quantity; save.
  - **Climax:** The catalogue and availability view represent the item according to its selected tracking mode.
  - **Resolution:** Individual assets can be checked out one at a time; pooled stock can be checked out by quantity without fake identities.

- **UJ-6. Ali returns part of a pooled quantity with mixed condition.**
  - **Persona + context:** Ali returns two of three field tablets borrowed for a site visit; one is Good and one is Damaged.
  - **Entry state:** The pooled Equipment has an Active Checkout for three units and the remaining stock is visible.
  - **Path:** Ali opens the Active Checkout; enters a return quantity of two; assigns one unit Good and one Damaged; optionally records a note; confirms.
  - **Climax:** The Custody Record retains one outstanding unit, available quantity increases by one, damaged quantity increases by one, and neither damaged unit is available for Checkout.
  - **Resolution:** The partial return is visible in history; the remaining unit stays with Ali until returned or handled as unresolved missing.

## 3. Glossary

- **Equipment** — A catalogue entry that can be individually tracked or quantity tracked.
- **Individual Asset** — One physically identifiable Equipment unit with a unique asset ID and at most one active checkout.
- **Pooled Equipment** — Equipment tracked by available quantity rather than individual physical identities.
- **Custody Record** — A record of a checkout and its eventual return, including borrower, timestamps, due state, and return condition.
- **Return Event** — An immutable event linked to one Custody Record, recording one full or partial return action, actor, timestamp, quantities or condition, and optional note.
- **Checkout** — The action that creates or activates a Custody Record and reduces availability.
- **Return** — The action that closes a Custody Record and restores or blocks availability based on condition.
- **Approval Request** — A request for restricted Equipment that must be approved before Checkout.
- **Restricted Equipment** — Equipment governed by a configured approver rule.
- **Declared Location** — Optional borrower-entered location category and description; it is not live tracking.
- **Active Checkout** — A Custody Record that has not been returned.
- **Overdue** — An Active Checkout whose expected return date/time has passed.
- **Unresolved Missing** — An open custody situation where Equipment has not been returned; it is not a completed Return.
- **Audit Record** — An append-only record of a meaningful correction or approval decision, including actor and timestamp.
- **Total Quantity** — The configured count of a Pooled Equipment entry; it equals available, checked-out, damaged, and any other explicitly held quantities.
- **Available Quantity** — Pooled units eligible for Checkout now.
- **Checked-Out Quantity** — Pooled units assigned to Active Checkout records.
- **Damaged Quantity** — Pooled units returned Damaged and quarantined from Checkout until reviewed.
- **Return Allocation** — The Good or Damaged quantity assigned to a Pooled Equipment Return; its quantities must sum to the returned quantity.
- **Individual Asset Condition** — The state of an Individual Asset: Good/Available, Checked-Out, Damaged/Unavailable, or Retired/Unavailable.
- **Reviewed Stock** — Damaged Pooled Equipment units explicitly restored to Available Quantity by an authorized office action.
- **Due Soon** — An Active Checkout whose expected return is within the configured one-day warning window and is not Overdue. `[ASSUMPTION: The warning window is one calendar day for the MVP.]`

### 3.1 Domain Invariants and State Transitions

These transitions preserve the approved decisions: Pooled Equipment remains quantity-based, pending approval does not reserve stock, missing equipment is not falsely returned, and availability checks are transactional.

| Domain | Valid transition | Required invariant |
|---|---|---|
| Individual Asset | Good/Available → Checked-Out → Good/Available on Good Return; Checked-Out → Damaged/Unavailable on Damaged Return; Damaged/Unavailable → Good/Available or Retired/Unavailable on Manager/Admin review | At most one Active Checkout exists; a Damaged/Unavailable asset cannot be checked out; review outcome and audit data are retained. |
| Pooled Equipment | Available Quantity → Checked-Out Quantity on Checkout; Checked-Out Quantity → Available Quantity or Damaged Quantity on Return | `Available + Checked-Out + Damaged = Total Quantity`; all quantities are integers >= 0. |
| Pooled Return | Active Checkout quantity N → remaining Active Checkout quantity N - returned quantity through one or more Return Events | `0 < returned quantity <= outstanding quantity`; Good + Damaged return quantities equal returned quantity; a zero remainder closes the Custody Record; all Return Events remain immutable. |
| Damaged stock | Damaged Quantity → Available Quantity only through authorized Review Stock action | Review cannot restore more units than Damaged Quantity and creates an Audit Record. |
| Custody | Created/Active → Partially Returned → Closed | Partial Return preserves borrower, original checkout, Return Events, and remaining quantity; remaining borrowed quantity equals original Checked-Out Quantity minus all returned quantities; Closed requires all quantity returned or an explicitly recorded unresolved state. |
| Due state | Active Checkout → Due Soon → Overdue | Due labels are derived from expected return; they do not alter custody or availability. |
| Approval Request | Pending → Approved → Active Checkout, or Pending → Rejected/Cancelled | Pending does not reserve stock; approval rechecks availability and is atomic with Checkout; a rejected request never creates an Active Checkout. |

Every availability-changing Checkout, approval, Return, correction, and stock-review action must execute as one transaction. A failed concurrency or validation check commits no partial quantity, custody, approval, or audit change.

## 4. Features

### 4.1 Named Access and Roles

**Description:** Users sign in with simple named local credentials. A user may hold more than one bounded role. This supports attribution without SSO complexity. `[ASSUMPTION: The MVP uses a simple local login flow suitable for demo data; production identity integration is not required.]`

**Functional Requirements:**

#### FR-1: Named user authentication

An unexpired local user can sign in and the system attributes Checkout, Return, Approval Request, approval, and correction actions to that user.

**Consequences (testable):**
- A valid demo credential opens an authenticated session; invalid credentials do not.
- Each new Custody Record and Audit Record stores the acting named user and timestamp.

#### FR-2: Bounded roles

The system can assign Field Staff, Office Staff, Approver, and Manager/Admin roles, including multiple roles for one user.

**Consequences (testable):**
- An Approver can act on Approval Requests for configured Restricted Equipment.
- Office Staff or Manager/Admin can create or correct records; ordinary Field Staff cannot perform office corrections.

### 4.2 Equipment Catalogue and Availability

**Description:** Office users create Equipment as either an Individual Asset or Pooled Equipment. The catalogue exposes current availability and relevant derived labels without collapsing approval, due, condition, and missing concepts into one status.

#### FR-3: Create individually tracked Equipment

An authorized office user can create an Individual Asset with a name/type and unique asset ID.

**Consequences (testable):**
- The system rejects a duplicate asset ID.
- A new Individual Asset starts available and has no Active Checkout.

#### FR-4: Create quantity-tracked Equipment

An authorized office user can create Pooled Equipment with a name/type and positive integer Total Quantity.

**Consequences (testable):**
- The system rejects zero, negative, fractional, or non-numeric quantities and initializes Available Quantity to Total Quantity, with Checked-Out Quantity and Damaged Quantity equal to zero.
- Demo data includes four tripods and six field tablets as Pooled Equipment.

#### FR-5: Show current availability and custody

An authenticated user can view searchable Equipment with availability, current borrower/custodian where applicable, due/overdue state, approval state, and condition/unresolved-missing indicators.

**Consequences (testable):**
- An Individual Asset with an Active Checkout is shown unavailable and identifies its borrower.
- Pooled Equipment shows available quantity and does not invent individual asset identities.
- An Individual Asset with Damaged/Unavailable condition is unavailable until a Manager/Admin review restores or retires it; pooled Damaged Quantity is tracked separately.
- Pooled Equipment displays Total, Available, Checked-Out, and Damaged quantities, and the invariant `Available + Checked-Out + Damaged = Total` is always true.

### 4.3 Checkout and Restricted Approval

**Description:** Field staff use self-service Checkout. Restricted Equipment enters an Approval Request before Checkout. Office staff can create or correct records when field staff need help. A pending request does not reserve equipment.

#### FR-6: Checkout available Equipment

An authenticated user can Checkout available Equipment by selecting an expected return date/time, with tomorrow as the editable default, optionally entering a Declared Location, and, for Pooled Equipment, entering a positive integer checkout quantity.

**Consequences (testable):**
- The authenticated borrower is recorded automatically; the user cannot choose another borrower in self-service.
- The common authenticated Checkout path completes in under 30 seconds in the seeded demo environment, excluding sign-in and network startup.
- An Individual Asset cannot have more than one Active Checkout.
- A Pooled Equipment Checkout decrements Available Quantity and increments Checked-Out Quantity by the requested quantity; zero, fractional, negative, non-numeric, and greater-than-available quantities are rejected.
- A self-service Checkout, office-assisted Checkout, and manager-created Checkout all use the same backend availability and Restricted Equipment approval checks; no UI-only check is sufficient.

#### FR-7: Request and decide Restricted Equipment approval

An authenticated user, Office Staff user, or Manager/Admin user can create a Checkout request for Restricted Equipment, but an authorized configured Approver must approve it before any Checkout is created.

**Consequences (testable):**
- The seeded drone requires Hamza's approval before an Active Checkout exists.
- The backend rejects any direct or manager-created Restricted Equipment Checkout without an Approved Approval Request for the same Equipment, borrower, quantity, and request.
- Approval rechecks availability and creates the Checkout atomically; a failed availability check creates no Checkout.
- The requester cannot approve their own request unless they are the configured Approver for that Restricted Equipment; the seeded drone rule permits only Hamza to approve.
- Rejection closes the request and creates no Checkout.
- Approval and rejection are represented in history and Audit Records.

#### FR-8: Office-assisted custody correction

An authorized Office Staff or Manager/Admin user can create or correct a custody record when self-service was not possible.

**Consequences (testable):**
- A correction requires the acting user and reason, preserves the original record, and creates an append-only Audit Record with old and new values.
- A correction cannot bypass the Individual Asset duplicate or Pooled Equipment over-allocation rules.
- A correction cannot create or activate a Restricted Equipment Checkout without the same backend approval requirement as self-service; a manager's role alone is not an approval.

### 4.4 Return and Condition

**Description:** The borrower or authorized office user can close all or part of an Active Checkout. The normal return stays short by requiring only Good or Damaged, with an optional note. Individual Asset condition is separate from pooled quantity fields.

#### FR-9: Return Equipment with condition

An authorized actor can Return all or part of an Active Checkout by entering a positive integer returned quantity for Pooled Equipment, allocating that quantity between Good and Damaged, and optionally entering a note. Individual Assets use one Good or Damaged condition.

**Consequences (testable):**
- A Good Return closes the Custody Record and restores Individual Asset or pooled quantity availability.
- For Pooled Equipment, `Good Return Quantity + Damaged Return Quantity = Returned Quantity`; `0 < Returned Quantity <= outstanding quantity`.
- A pooled Good Return decreases Checked-Out Quantity and increases Available Quantity by the Good quantity.
- A pooled Damaged Return decreases Checked-Out Quantity and increases Damaged Quantity by the Damaged quantity; those units remain unavailable for Checkout.
- A pooled partial Return leaves the remaining quantity in the Active Checkout; a Return of the full outstanding quantity closes it.
- An Individual Asset Damaged Return sets Individual Asset Condition to Damaged/Unavailable; it does not create or use pooled quantity fields.
- Each Return action creates one immutable Return Event linked to the original Custody Record, recording actor, timestamp, Good quantity, Damaged quantity, and optional note. For an Individual Asset, the event records its condition instead of quantities.
- Multiple partial Return Events are supported; remaining borrowed quantity equals original Checked-Out Quantity minus the sum of all Return Event quantities.
- An over-return is rejected transactionally and creates neither a Return Event nor a quantity/state change.
- The common authenticated Return path completes in under 30 seconds in the seeded demo environment, excluding sign-in and network startup.
- A completed Return always includes a condition value; a missing item is not falsely marked returned.

#### FR-10: Review damaged equipment

Only a Manager/Admin can review a Damaged/Unavailable Individual Asset and either restore it to Good/Available or retire it as permanently unavailable. An authorized Office Staff or Manager/Admin user can review a pooled Damaged Quantity and restore a selected positive integer quantity to Available Quantity.

**Consequences (testable):**
- A Field Staff, Office Staff, or Approver cannot restore or retire a Damaged/Unavailable Individual Asset.
- A Manager/Admin review must choose exactly Restore to Good/Available or Retire Permanently Unavailable; no other state is valid.
- The review records actor, timestamp, outcome, and optional reason in an append-only Audit Record. A restored asset can be checked out; a retired asset can never be checked out.
- The system rejects a pooled review quantity greater than Damaged Quantity and atomically transfers the reviewed quantity from Damaged to Available.
- A pooled review records actor, timestamp, quantity, and optional note in an Audit Record; unreviewed Damaged Quantity remains unavailable.

### 4.5 Due Status, History, and Auditability

**Description:** The system provides visible in-application due-soon and overdue indicators and historical custody lookup. It does not send automated WhatsApp or email messages in the MVP.

#### FR-11: Due and overdue indicators

The system derives due-soon and Overdue indicators from the expected return date/time of each Active Checkout.

**Consequences (testable):**
- The default Checkout date is tomorrow and can be changed before confirmation.
- An Active Checkout past its expected return date/time is shown Overdue without changing the underlying custody facts.
- Due and Overdue state is visible in current Equipment and custody views.

#### FR-12: Search custody history

An authenticated user can view recent Custody Records and their immutable Return Events for an Equipment item or selected period, including borrower, checkout time, expected return, each return actor/time, returned Good/Damaged quantities or individual condition, notes, and relevant approval/correction history.

**Consequences (testable):**
- A seeded history query for last month returns records with named actors and timestamps.
- Every full or partial Return appears as a separate Return Event linked to the original Custody Record; multiple events are ordered by timestamp and are never overwritten by later Checkout activity.
- History derives the aggregate Custody Record state and remaining borrowed quantity from the original checkout and all Return Events.

#### FR-13: Append-only meaningful audit trail

The system records meaningful approvals, rejections, and corrections as append-only Audit Records.

**Consequences (testable):**
- An Audit Record includes actor, timestamp, action, and changed values or decision reason where applicable.
- A normal Checkout or Return does not require direct editing of prior history; corrections add audit evidence rather than deleting the original record.

### 4.6 Optional Survey-Kit Shortcut

**Description:** If safe within the two-day MVP, a predefined survey-kit shortcut selects existing component Equipment and reuses the same availability and approval rules. It is not a new kit asset model.

#### FR-14: Atomically process a predefined kit

The system may allow an authorized user to request a predefined set of components as one operation only when every component passes availability and approval checks.

**Consequences (testable):**
- If any component is unavailable or requires unresolved approval, no component is partially checked out.
- If implemented, all component Custody Records are created consistently; otherwise the UI explicitly leaves kit checkout out of MVP and separate checkout remains available.

**Out of Scope:** Dynamic kit authoring, partial kit checkout, and a separate kit asset identity.

## 5. Cross-Cutting NFRs

- **Usability:** The responsive web UI supports phone-sized and desktop layouts; common authenticated Checkout and Return each complete in under 30 seconds in the seeded demo environment.
- **Correctness:** Availability checks for Individual Assets and Pooled Equipment are transactional so concurrent actions cannot create duplicate individual custody or over-allocate pooled quantity.
- **Security:** Protected actions require authentication and role checks; no real credentials or secrets are committed.
- **Auditability:** Meaningful approval and correction history is append-only and attributable to a named user.
- **Run readiness:** The application can be started using one documented command or a short accurate sequence and explored with deterministic demo data within five minutes.
- **Accessibility:** Core controls have readable labels, usable keyboard focus, and sufficient contrast for the demonstrated flows. `[ASSUMPTION: Full accessibility conformance testing is outside the two-day MVP.]`

## 6. Constraints and Guardrails

### 6.1 Scope and delivery

- The binding implementation target is the reliable custody loop within two days; the broader assignment context may allow four days but must not be used to expand the MVP silently.
- The implementation baseline for this MVP is ASP.NET Core 8 Web API, Angular, EF Core, and SQLite, subject to architecture review; changing the baseline does not change product scope.
- The PRD defines behavior and acceptance contracts; it does not prescribe internal class, endpoint, or component design.

### 6.2 Privacy and data

- Use invented local demo users and clearly labeled demo records.
- Do not collect GPS or live location. Declared Location is optional and user-entered.
- Do not use production identity, customer data, or committed secrets.

### 6.3 Operational guardrails

- No WhatsApp or email delivery dependency in the MVP; due status is in-app.
- No offline synchronization, procurement, depreciation, maintenance work orders, or full missing-item incident workflow.
- Do not mark missing Equipment as returned merely to close a record.

## 7. Non-Goals (Explicit)

- Becoming a complete asset-management, procurement, depreciation, or financial accounting system.
- GPS/live location tracking or offline-first synchronization.
- Automated WhatsApp/email reminders.
- Full maintenance/work-order management.
- Enterprise SSO or external identity integration.
- A full missing-item incident, escalation, or recovery workflow.
- Individual labeling of explicitly Pooled Equipment.
- A separate persistent survey-kit asset model.

## 8. MVP Scope

The two-day implementation is judged against the Must slice first. Should items may be attempted only after every Must journey passes its acceptance criteria. No Should item may displace a working Must journey.

### 8.1 Must — minimum demonstrable release

- **M-1 Identity:** Seeded named login and the minimum role checks needed for Field Staff, Office Staff, Approver, and Manager/Admin.
- **M-2 Catalogue:** Create and display an Individual Asset with a unique asset ID and Pooled Equipment with a positive integer Total Quantity.
- **M-3 Visibility:** Show current Individual Asset custodian/availability and Pooled Equipment Total, Available, Checked-Out, and Damaged quantities.
- **M-4 Standard custody:** Self-service Individual Asset Checkout and Good/Damaged Return with tomorrow default, due/overdue display, borrower attribution, and history.
- **M-5 Pooled custody:** Quantity Checkout, full/partial Return, Good/Damaged Return Allocation, quantity invariants, and damaged-stock quarantine/review.
- **M-6 Approval:** Backend-enforced Restricted Equipment approval for self-service, office-assisted, and manager-created paths; seeded drone/Hamza scenario; no duplicate assignment.
- **M-7 Audit and correctness:** Transactional availability changes, approval decisions, corrections, and damaged-stock review with attributable history.
- **M-8 Demo readiness:** Deterministic local data and documented startup that lets a reviewer run the Must journeys within five minutes.

### 8.2 Should — only after Must passes

- **S-1:** Due Soon warning refinement beyond the required Overdue behavior.
- **S-2:** Office-assisted correction UI beyond the minimum backend-safe correction path.
- **S-3:** Predefined survey-kit shortcut using atomic component processing.
- **S-4:** Expanded filters, richer history presentation, and broader equipment seed data.

### 8.3 Deferred — not part of the two-day MVP

- WhatsApp/email reminders, GPS/live location, offline synchronization, procurement, depreciation, maintenance work orders, SSO, and a full missing-item incident workflow.
- Dynamic kit authoring, partial kit checkout as a separate kit concept, and individual labels for explicitly pooled stock.

### 8.4 Explicit cut criteria

- Cut all Should items before cutting any Must item.
- Cut the survey-kit shortcut first if atomic all-or-nothing processing is not demonstrated.
- Cut Due Soon refinement, richer filters, and nonessential seed data before reducing Checkout, Return, approval, quantity invariants, or history.
- If a Must item cannot be made reliable in the two-day window, report it as incomplete rather than weakening approval, transaction, or inventory guarantees.

### 8.5 Minimum demonstrable journeys

The MVP is ready to demonstrate only when these scenarios pass from clean seeded data:

1. **Standard Individual Asset:** Ali signs in, checks out an available asset, sees himself and tomorrow's due date, then returns it Good and the asset becomes available.
2. **Pooled mixed-condition Return:** Ali checks out three pooled units, returns two with one Good and one Damaged, and the system shows one remaining checked out, one available, and one damaged/unavailable.
3. **Restricted approval:** A requester, office user, or manager cannot activate a drone Checkout directly; Hamza approves, availability is rechecked, and exactly one auditable Checkout is created.
4. **Office visibility:** Saima sees current custodian/quantities and a historical record after the above transactions.

## 9. Success Metrics

**Primary**

- **SM-1:** In a clean seeded demo, a field user can complete a standard authenticated Checkout in under 30 seconds and the system shows the correct borrower, active custody, due date, and availability. Validates FR-5 and FR-6.
- **SM-2:** In a clean seeded demo, a field user can complete a full or partial pooled Return in under 30 seconds with Good/Damaged quantities, and availability changes correctly without making damaged stock available. Validates FR-9 and FR-10.
- **SM-3:** A reviewer can identify the current custodian or available quantity for a selected Equipment item and find a historical custody record without leaving the application. Validates FR-5 and FR-12.
- **SM-4:** A restricted drone request cannot become an Active Checkout through self-service, office-assisted, or manager-created paths without Hamza's backend-enforced approval, and the decision is auditable. Validates FR-7 and FR-13.

**Secondary**

- **SM-5:** Equipment setup correctly represents both an Individual Asset with a unique ID and Pooled Equipment with a quantity, including four tripods and six field tablets. Validates FR-3 and FR-4.
- **SM-6:** Repeat concurrent or sequential attempts cannot create duplicate Individual Asset custody or exceed Pooled Equipment availability. Validates FR-6 and FR-7.
- **SM-7:** A Manager/Admin can restore or retire a damaged Individual Asset with an audited outcome, and an authorized user can restore a selected quantity of damaged pooled stock while excess review quantity is rejected. Validates FR-10 and FR-13.

**Counter-metrics (do not optimize)**

- **SM-C1:** Do not optimize for the number of features or screens delivered; a smaller reliable custody loop is preferable to deferred or inconsistent breadth.
- **SM-C2:** Do not optimize for forced location completion or external reminder delivery; either could increase friction and reduce field adoption.

## 10. Open Questions

1. Which equipment types and initial kit templates should be seeded beyond the required drone, four tripods, and six field tablets?
2. If the two-day implementation cannot safely include the survey-kit shortcut, it is explicitly deferred under §8.4 and must be reported as incomplete in the README. `[NOTE FOR PM]`
3. **Contradiction flag:** The approved decision allows Manager/Admin users to create or correct records, while this revision requires every Restricted Equipment transaction—including manager-created transactions—to pass the configured approver gate. The revision preserves both by treating manager authority as record-management authority, not automatic approval authority.

## 11. Assumptions Index

- `[ASSUMPTION: The MVP uses a simple local login flow suitable for demo data; production identity integration is not required.]` — §4.1.
- `[ASSUMPTION: Full accessibility conformance testing is outside the two-day MVP.]` — §5.
- `[ASSUMPTION: The Due Soon warning window is one calendar day for the MVP.]` — §3.1.
