# Adversarial Architecture Review

## Verdict

**Revise before implementation.** The spine establishes useful atomicity and pooled/individual rules, but it does not yet make endpoint ownership, entity ownership, or several state transitions unambiguous enough for independently built features to converge. The highest-risk gaps concern approval failure, pooled quantity reconciliation, corrections, unresolved missing custody, and the absence of explicit command ownership.

## Findings

### 1. Endpoint ownership is contradicted by the dependency direction

- **Location:** `Design Paradigm` dependency diagram; `Backend Organization and Responsibilities`
- **Trigger condition:** The diagram permits API endpoints to depend directly on `Infrastructure`, while AD-1 says every custody mutation must enter one application service.
- **Guard snippet:** Make `API -> Application` the only mutation path; forbid endpoint handlers from resolving `DbContext`, repositories, or infrastructure mutation ports. Assign each mutation to exactly one command owner (for example `Custody.Checkout`, `Approvals.Decide`, `Custody.Return`, `Custody.Correct`, `Equipment.ReviewDamage`).
- **Potential consequence:** Two independently built endpoints can implement different availability, approval, or audit behavior while both appear to comply with the architecture.

### 2. The approval request creation endpoint and owner are missing

- **Location:** `REST API Contract Seed`, `Approvals`; PRD FR-7
- **Trigger condition:** The contract lists pending/approve/reject endpoints but no command for creating an Approval Request; the checkout endpoint is described as both standard and office-assisted.
- **Guard snippet:** Define `POST /api/approvals/requests` (or explicitly make checkout create the request), its owning application service, request identity/borrower rules, and the response/state returned for `Pending`.
- **Potential consequence:** One client may create a pending request through checkout while another creates it through Approvals, producing duplicate requests or a direct restricted checkout.

### 3. Approved-but-unfulfilled requests have no valid transition

- **Location:** AD-6; `Checkout and approval lifecycle` state diagram
- **Trigger condition:** Approval transitions `Requested -> Approved`, then availability failure can occur during the atomic activation, but no failure, retry, cancellation, or terminal state is defined.
- **Guard snippet:** Specify the atomic decision outcomes: either `Pending -> Rejected/Cancelled` with reason, or `Pending -> Active` in the same transaction; if an intermediate `Approved` state is retained, define its owner, expiry, retry, and availability-loss transition.
- **Potential consequence:** A request can remain Approved forever, be retried into duplicate custody, or be silently reported as approved when no equipment was issued.

### 4. “Manager authority never substitutes” conflicts with the office/manager checkout API

- **Location:** AD-6; PRD FR-6 and FR-7; `POST /api/custody/checkouts`
- **Trigger condition:** The same checkout endpoint accepts standard, office-assisted, and manager-created operations, but the contract does not state whether a restricted request is created, submitted, or rejected for each actor.
- **Guard snippet:** Define a single authorization matrix keyed by actor, borrower, equipment restriction, and approval state; require the configured approver decision for every restricted path, including office-assisted checkout.
- **Potential consequence:** Independently implemented office and manager flows can create restricted custody without approval or allow a manager to approve implicitly.

### 5. Pooled custody has no reconciliation invariant

- **Location:** AD-2; compact domain model; ER diagram
- **Trigger condition:** `CheckedOutQuantity` is stored on `EquipmentType`, while pooled `CustodyRecord` rows each store outstanding quantity, but no rule says their sum must equal `CheckedOutQuantity`.
- **Guard snippet:** Add `EquipmentType.CheckedOutQuantity = SUM(outstanding quantity of all open pooled custody records)` and enforce it in every checkout, return, correction, missing, review, and repair path; preferably derive one side or validate it transactionally.
- **Potential consequence:** A successful-looking correction or partial return can leave the dashboard counts inconsistent with “who has what,” even when the three stored pool counters still sum to Total.

### 6. The entity relationship permits invalid individual/pooled custody shapes

- **Location:** `Core entity relationships`; `Compact Domain Model`
- **Trigger condition:** `CustodyRecord` can reference either `EquipmentType` or `IndividualAsset`, but the spine does not require exactly one reference, nor require that an asset’s parent type matches the equipment reference.
- **Guard snippet:** State and enforce XOR ownership: pooled custody has `EquipmentTypeId` and no `IndividualAssetId`; individual custody has `IndividualAssetId` and no pooled quantity; the asset’s type must be the selected type. Add database/application constraints.
- **Potential consequence:** A record can decrement pooled stock while also claiming an individual asset, or be orphaned from the availability projection.

### 7. Individual active-custody uniqueness does not define unresolved custody

- **Location:** AD-5; lifecycle state diagram; PRD missing-equipment rules
- **Trigger condition:** The filtered unique index refers to “active” custody, while the state diagram adds `Unresolved`; no definition says whether unresolved custody remains included in the uniqueness predicate.
- **Guard snippet:** Define `OpenCustody = Active | PartiallyReturned | Unresolved` (or equivalent), keep it unique per individual asset, and specify how unresolved custody affects availability and checkout.
- **Potential consequence:** A missing asset can be checked out again, or a later return/correction can create a second open custody record.

### 8. Missing-equipment transitions have no command, endpoint, or owner

- **Location:** `Checkout and approval lifecycle`; PRD FR-9 and operational guardrails; `REST API Contract Seed`
- **Trigger condition:** The architecture names `Active -> Unresolved`, but no actor can declare, resolve, or otherwise manage that state, and no endpoint exists.
- **Guard snippet:** Either remove the transition from MVP or define `DeclareMissing` and `ResolveMissing` commands, authorized roles, allowed evidence/audit, inventory effects, and whether resolution is a return, recovery, retirement, or correction.
- **Potential consequence:** Implementers will either falsely close missing custody as returned or invent incompatible ad hoc state changes.

### 9. Individual return cardinality is underspecified

- **Location:** AD-3; `ReturnEvent` in compact domain model; `POST /api/custody/:id/returns`
- **Trigger condition:** Individual return events “record condition instead of quantities,” but no invariant limits an individual custody record to one return event or rejects a second return after closure.
- **Guard snippet:** Require exactly one return event for an individual custody record, reject any later return transactionally, and define the allowed condition transition for Good, Damaged, and any unresolved resolution.
- **Potential consequence:** Two clients can both return the same asset, append contradictory conditions, and leave audit/history inconsistent despite the pooled over-return rule.

### 10. Correction semantics and ownership are not bounded

- **Location:** AD-1; `Features/Custody`; `POST /api/custody/:id/corrections`
- **Trigger condition:** “Correction” has no field allowlist, actor matrix, or inventory/state transition contract.
- **Guard snippet:** Define separately owned commands for due-date correction, borrower/actor correction, quantity correction, and state correction; specify which roles may invoke each, permitted states, old/new values, and the compensating inventory transaction.
- **Potential consequence:** One implementation may edit a due date only while another changes borrower or quantity directly, bypassing approval, reconciliation, and append-only history.

### 11. Performing actor ownership is absent from the relational model

- **Location:** `Compact Domain Model`; ER diagram; PRD FR-1/FR-2
- **Trigger condition:** `CustodyRecord` lists both borrower and performing actor, but the ER diagram only relates `USER` to custody as borrower; return, approval, correction, and review actor ownership is not consistently modeled.
- **Guard snippet:** Add explicit required actor FKs/relations for checkout performer, each return performer, approval decision maker, correction actor, and damage reviewer; distinguish borrower from operator in every command DTO.
- **Potential consequence:** Office-assisted actions can be attributed to the borrower, or history can omit who actually performed a privileged mutation.

### 12. Damage-review endpoint cannot identify its entity unambiguously

- **Location:** `POST /api/equipment/:id/damage-review`; PRD FR-10
- **Trigger condition:** The same route is intended for an individual asset and a pooled equipment type, but their legal actions, identifiers, and authorization differ.
- **Guard snippet:** Use explicit resources such as `POST /api/assets/{assetId}/damage-review` and `POST /api/equipment-types/{typeId}/damaged-stock/reviews`, with distinct DTOs and policies.
- **Potential consequence:** A client can submit a pooled quantity to an individual asset route, or an asset retirement request to a pooled type route, leading to unsafe interpretation or inconsistent authorization.

### 13. Pooled damage review and return use different ownership language without a shared command contract

- **Location:** AD-1; PRD FR-10; `Features/Equipment` and `Features/Custody`
- **Trigger condition:** Damage review belongs to Equipment, while damaged returns belong to Custody, yet both transfer units between Checked-Out, Damaged, and Available and no shared stock-transfer policy is named.
- **Guard snippet:** Define one domain stock-transfer policy with allowed source/destination, positive quantity, actor policy, and audit requirement; have both commands invoke it inside their transaction.
- **Potential consequence:** Independently built return and review services can apply different counter updates or permit restoring units that were never quarantined.

### 14. Due-date semantics are not deterministic across builders

- **Location:** `Consistency Conventions` IDs and time; AD-7; PRD FR-11; DECISIONS.md decision 6
- **Trigger condition:** Timestamps are persisted UTC, but “tomorrow” and the one-calendar-day Due Soon window have no business timezone, cutoff time, or DST rule.
- **Guard snippet:** Declare the MVP business timezone, define the exact default due instant and inclusive/exclusive Due Soon boundaries, and require all projections to use the same clock/timezone policy.
- **Potential consequence:** The API and Angular client can disagree on due/overdue labels, especially around midnight or daylight-saving changes.

### 15. Approval rule ownership and uniqueness are incomplete

- **Location:** AD-9; compact model `ApprovalRule`; ER diagram
- **Trigger condition:** The spine says rules and approver assignments are data, but does not define one active rule per equipment type, multiple approver semantics, rule versioning, or what happens when the configured approver is inactive.
- **Guard snippet:** Specify the rule key and uniqueness constraint, active/inactive semantics, whether one or all approvers may decide, self-approval policy, and the snapshot of rule/approver identity stored on the request and decision.
- **Potential consequence:** Two independently seeded or configured rules can disagree about whether a drone is restricted or who may approve it, and historical decisions can become unverifiable after configuration changes.

### 16. The kit extension point has no endpoint or atomic identity contract

- **Location:** AD-12; PRD FR-14; API contract
- **Trigger condition:** AD-1 says kit operations use the application service and AD-12 promises all component custody records, but no kit command, request identity, borrower/approver behavior, or rollback response is defined.
- **Guard snippet:** If deferred, explicitly remove kit routes and acceptance from the MVP contract. If implemented, define one `POST` command, idempotency key, component snapshot, shared borrower/due/location values, approval aggregation, and all-or-nothing response.
- **Potential consequence:** A future implementer can expose a kit UI that performs sequential component checkouts, recreating the partial-kit failure the AD is intended to prevent.

### 17. Append-only audit is a convention, not a protected ownership boundary

- **Location:** AD-3; `Consistency Conventions` Audit; `AuditRecord`
- **Trigger condition:** The architecture requires append-only audit rows but does not state database permissions/constraints or which command owns audit creation for every meaningful mutation.
- **Guard snippet:** Make audit creation part of each owning command transaction, prohibit update/delete through repositories and endpoints, and add an integration invariant that audit rows are immutable and reference the exact target and old/new values.
- **Potential consequence:** A correction or approval endpoint can update the domain row without an audit row, or later code can edit audit history while all tests for normal paths still pass.

### 18. Availability read ownership is not defined for concurrent writes

- **Location:** AD-4; `Features/Equipment` availability projection; AD-5
- **Trigger condition:** The spine requires reload-and-validate writes but does not define whether availability is read from counters, custody aggregation, or both, nor which projection is authoritative after a conflict.
- **Guard snippet:** Name one authoritative write model and one read projection; define refresh/recalculation behavior after every committed command and the response contract after `409`.
- **Potential consequence:** The catalogue may briefly or permanently show stale availability, and independently built queries can disagree about whether an item is checkoutable.

