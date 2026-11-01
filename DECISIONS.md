## 1. Checkout workflow

- **The gap:** The client said, “Anyone in the team can take anything, we don't have a system,” and “If it takes more than a few seconds they won't do it.” The requirements do not say who must record a checkout or return.
- **What I decided:** Use self-service checkout and return for field staff, with office staff able to create or correct records when needed.
- **The alternative:** Require Saima or another office user to record every checkout and return.
- **Why this one:** Self-service removes the office bottleneck and best fits the few-seconds adoption constraint, while the office override protects data quality when field staff forget or need help.
- **What would change my mind:** Evidence that field staff cannot reliably authenticate or use the workflow on site, or that company policy requires centralized custody recording.

## 2. Inventory representation

- **The gap:** The client said, “We have 4 tripods and 6 field tablets, those are not individually tracked, we just need to know how many are left.” The requirements do not fully classify every equipment type.
- **What I decided:** Use a mixed model: individually track high-value or identity-sensitive assets such as total stations, GPS rovers, drones, and laptops; track explicitly pooled equipment by quantity.
- **The alternative:** Assign an identity and label to every physical item, or track every type only as an aggregate quantity.
- **Why this one:** It preserves accountability where loss or approval matters while honoring the explicit pooled-equipment requirement and keeping setup and use simple.
- **What would change my mind:** A complete client inventory showing that pooled items need individual damage or custody history, or evidence that labeling all assets is operationally easy.

## 3. Reminder scope

- **The gap:** The client said, “It would be good if the system reminds people. We mostly use WhatsApp for everything here.” The required reminder channel and delivery behavior are unspecified.
- **What I decided:** Include due-soon and overdue status in the application for the first release; defer automated WhatsApp or email delivery.
- **The alternative:** Implement WhatsApp reminders or email reminders in the MVP.
- **Why this one:** It delivers visible accountability without making external messaging integration, credentials, delivery failures, and privacy handling part of the assignment-critical path.
- **What would change my mind:** A confirmed testable messaging account and a demonstrated requirement that passive in-app status is insufficient for adoption.

## 4. Restricted equipment approval

- **The gap:** The client said, “The drone is different, that needs Hamza's approval before anyone takes it,” and “Same for the new rover maybe, I will decide later.” The system must support one confirmed rule and one undecided rule.
- **What I decided:** Support simple roles and configurable restricted-equipment rules; configure the drone as requiring Hamza's approval and leave the new rover rule configurable but not assumed.
- **The alternative:** Hard-code Hamza and the drone only, or defer all approval behavior.
- **Why this one:** It satisfies the confirmed drone constraint and represents the unresolved rover requirement without embedding a guess or blocking future configuration.
- **What would change my mind:** A requirement that approval rules will never change, or evidence that configuration would create more complexity than the assignment can safely demonstrate.

## 5. Authentication / identity

- **The gap:** The client said, “Half of them just use WhatsApp, that's it,” and “If it takes more than a few seconds they won't do it.” The requirements do not specify how users identify themselves.
- **What I decided:** Use a simple named-user login for the first release, without SSO integration, so each checkout and return is attributable to a person.
- **The alternative:** Use shared PIN/kiosk access, or integrate email/password/SSO.
- **Why this one:** Named users are necessary for reliable custody history, while a simple login avoids the complexity and setup burden of enterprise identity integration.
- **What would change my mind:** Evidence that the field team cannot manage individual credentials, or a confirmed existing identity provider that is simpler and safer to use.

## 6. Due-date behavior

- **The gap:** The client said, “I want it back on time, people keep things for weeks when they needed it for one day,” and “If it takes more than a few seconds they won't do it.” The requirements do not define how due dates are entered.
- **What I decided:** Require an expected return date/time, default it to tomorrow for a fast one-day checkout, allow the borrower to change it, show due-soon and overdue states, and allow office staff to correct it with an audit record. The tomorrow default is a proposed usability assumption, not an explicit client requirement.
- **The alternative:** Make the due date optional, require manual date selection every time, or enforce a fixed one-day loan.
- **Why this one:** It addresses the overdue-equipment problem while keeping the common one-day transaction fast and preserving accountability when dates are changed.
- **What would change my mind:** Evidence that typical loans are not one day, that tomorrow is an unsafe default, or that staff routinely need time-of-day precision beyond the proposed interaction.

## 8. Pooled equipment checkout

- **The gap:** The client said pooled equipment “just need[s] to know how many are left,” while also asking, “I want to know who has what.” The requirements do not define whether pooled quantities need borrower history.
- **What I decided:** Record the named borrower and quantity, decrement available stock at checkout, and restore quantity on return; do not assign individual serial identities to pooled items.
- **The alternative:** Decrement quantity without borrower history, or label and track every pooled item individually.
- **Why this one:** Named quantity transactions preserve accountability while honoring the explicit pooled-inventory requirement and avoiding unnecessary labeling work.
- **What would change my mind:** Evidence that pooled items require individual damage or custody history, or that quantity-only records are sufficient for the office to answer custody questions.

## 7. Return condition and missing equipment

- **The gap:** The client said, “Sometimes equipment comes back damaged and nobody says anything until the next person takes it to site and finds out.” The requirements do not define how condition is recorded or how an item that is not returned should be represented.
- **What I decided:** A completed return must select either **Good** or **Damaged**, with an optional note. Damaged equipment becomes unavailable until reviewed. Missing equipment is not marked as returned; its custody record remains open and associated with the borrower. A full missing-item incident workflow is deferred from the four-day MVP.
- **The alternative:** Ask for condition only when damage is reported; mark missing equipment as returned with a missing flag; or build a full maintenance and missing-item incident workflow.
- **Why this one:** Requiring only the Good/Damaged choice keeps the normal return fast and exposes damage at handoff. Keeping missing custody open preserves the truth that the borrower still has unresolved responsibility, while deferring a full incident workflow protects the MVP timebox.
- **What would change my mind:** Evidence that the Good/Damaged choice causes abandonment, a policy requiring immediate formal incident records, or enough time and validated workflow detail to implement missing-item handling safely.

## 9. Permissions and multiple roles

- **The gap:** The requirements identify different responsibilities—field staff use equipment, Saima handles office calls, Hamza approves the drone, and Tariq represents management—but do not define authorization. The client also says, “Anyone in the team can take anything.”
- **What I decided:** Use four simple roles—Field Staff, Office Staff, Approver, and Manager/Admin—and allow one user to hold multiple roles. Keep authorization explicit and small rather than implementing a granular permission matrix.
- **The alternative:** Give everyone one shared role, or create a detailed permission matrix with many independent permissions.
- **Why this one:** Multiple roles accurately represents users such as an approver who is also field staff, while four bounded roles provide accountability within the four-day MVP.
- **What would change my mind:** Evidence that the role boundaries produce unauthorized actions, or a validated need for department-, equipment-, or field-level permissions.

## 10. Relational custody records and append-only audit log

- **The gap:** The client said, “I want to be able to look back at last month and see what was out and who had it,” and office staff must be able to correct records. The requirements do not prescribe event sourcing or an audit model.
- **What I decided:** Use standard relational checkout and return records as the operational source of truth. Preserve historical records and append audit entries for meaningful corrections and approvals, recording the actor, timestamp, reason where applicable, and old/new values. Perform a correction and its audit entry transactionally. Do not implement full event sourcing.
- **The alternative:** Store only the latest equipment state, allow direct historical edits, or make an append-only event stream the source of truth for every state transition.
- **Why this one:** Relational records are faster to implement and query for the MVP, while the append-only audit log preserves accountability and supports historical review. A transaction keeps the record and audit entry consistent without the complexity of event-sourced reads.
- **What would change my mind:** A requirement to reconstruct every historical state solely from events, a need for distributed/offline writes, or audit/compliance requirements stronger than this assignment establishes.

## 11. Optional declared location

- **The gap:** The client wants to know where equipment is, gives the example of an item being “in somebody's car boot for three weeks,” and asks for current custody visibility. The requirements do not define whether location must be captured or tracked live.
- **What I decided:** Record borrower identity automatically from the authenticated user. Offer an optional declared location category—such as office, vehicle, or site—with a short description. Do not implement GPS tracking and do not make location entry mandatory for every checkout.
- **The alternative:** Track only the borrower, require location for every checkout, or implement GPS/device-based live tracking.
- **Why this one:** Automatic identity answers who has the item; optional declared location adds useful context without slowing the few-seconds workflow. It is honest about location becoming stale or absent and fits the four-day MVP.
- **What would change my mind:** Evidence that staff cannot locate equipment without mandatory location capture, or a validated operational need and available device/integration for live tracking.

## 12. Restricted-equipment approval lifecycle

- **The gap:** The client said, “The drone is different, that needs Hamza's approval before anyone takes it.” The requirements do not define what happens while approval is pending or how concurrent requests avoid double assignment.
- **What I decided:** A restricted-equipment request must be approved before checkout. Pending requests do not reserve equipment. At approval, the system must recheck availability and create the checkout atomically; rejections close the request without checkout. Approval decisions are recorded in the audit trail.
- **The alternative:** Reserve equipment while pending, allow checkout before approval, or approve first without rechecking availability.
- **Why this one:** It enforces the confirmed approval requirement, avoids promising unavailable equipment, and prevents double assignment through an atomic availability check and checkout.
- **What would change my mind:** A business rule requiring reservations, evidence that approval delays are unacceptable, or a validated need for a more advanced reservation queue.

## 13. Separate availability, approval, due, and missing concepts

- **The gap:** The client describes current custody, overdue returns, damage, and missing equipment separately: “I want to know who has what,” “people keep things for weeks,” and “Sometimes equipment comes back damaged.” The requirements do not define whether these are one status field or separate concepts.
- **What I decided:** Model physical availability, approval-request state, active checkout due/overdue indicators, and unresolved missing-equipment reports separately. The UI may show derived combined labels for simplicity, but the data model will not force them into one mutually exclusive equipment-status enum.
- **The alternative:** Use one bounded equipment-status enum for all conditions, or use free-text/manual statuses.
- **Why this one:** Separate concepts represent simultaneous truths—for example, an active checkout can be overdue while a missing report remains unresolved—and avoid contradictory state transitions. Derived UI labels retain usability for office staff.
- **What would change my mind:** A validated workflow proving the concepts are always mutually exclusive, or a scope reduction that removes approvals, overdue indicators, or missing reports entirely.

## 14. Core custody-loop MVP boundary

- **The gap:** The client's core request is, “I want to know who has what,” but the narrative also mentions reminders, damage, and future asset-management possibilities. The assignment states that not every described feature must be implemented, and the delivery time is limited.
- **What I decided:** Make the first release the smallest demonstrable custody loop: named login, current inventory, checkout with approval where required, due/overdue indication, return condition, and history/audit. Defer WhatsApp/email reminders, GPS, procurement, depreciation, maintenance work orders, offline synchronization, and a full missing-item incident workflow.
- **The alternative:** Broaden the MVP to include external notifications, GPS, maintenance, or broader asset management.
- **Why this one:** The core loop directly solves the stated visibility problem and is more likely to be reliable and demonstrable within four days than a broad but shallow system.
- **What would change my mind:** A confirmed review criterion making one deferred capability mandatory, or enough validated time and integration access to add it without weakening the custody loop.

## 15. Lightweight survey-kit shortcut

- **The gap:** The client said, “Sometimes they take the whole survey kit, sometimes just one piece.” The requirements do not define whether a kit is a separate asset or how its components should be checked out consistently.
- **What I decided:** Include a lightweight shortcut that displays predefined kit components and reuses the existing individual-asset and pooled-quantity checkout process; do not introduce a separate kit asset model. The shortcut must run availability and approval checks for every component. If any component requires approval, no component is checked out until approval. On approval, all component checkout records are created atomically; if that cannot be implemented safely within the four-day MVP, defer the shortcut rather than weaken consistency.
- **The alternative:** Defer kit support and require separate checkouts, create a new atomic kit asset model, or issue components sequentially.
- **Why this one:** It supports the client's common kit workflow without duplicating custody logic, while preflight and atomic creation prevent partial kit issuance and preserve restricted-equipment controls.
- **What would change my mind:** Evidence that predefined kits change frequently, that partial kit checkout is the dominant workflow, or that safe atomic multi-component processing cannot be demonstrated within the timebox.

## 16. Duplicate active checkouts and transactional availability

- **The gap:** The client said, “Sometimes they take the whole survey kit, sometimes just one piece,” and “We have 4 tripods and 6 field tablets, those are not individually tracked, we just need to know how many are left.” The requirements do not define duplicate behavior for individual assets versus pooled quantities.
- **What I decided:** Prevent an individual physical asset from having more than one active checkout. Allow multiple pooled-equipment checkouts when sufficient quantity is available. Enforce both rules transactionally with availability checks.
- **The alternative:** Permit duplicates based only on UI checks, allow only one transaction per equipment type per person, or apply the same rule to pooled and individual equipment.
- **Why this one:** It preserves physical-asset integrity while allowing legitimate borrowing of multiple pooled units, and transactional checks prevent concurrent requests from oversubscribing inventory.
- **What would change my mind:** A validated workflow requiring shared custody of one physical asset, or evidence that pooled quantities need a stricter one-transaction rule.

## 17. Deterministic demo data and local credentials

- **The gap:** The client provides inventory facts—“We have 4 tripods and 6 field tablets”—but no actual user list, asset identifiers, or credentials. Reviewers must run the application within five minutes.
- **What I decided:** Include a small deterministic demo dataset containing clearly invented sample staff and roles, individually tracked equipment, four tripods, six field tablets, and a drone requiring Hamza's approval. Clearly label invented records as demo data. Document local-only, non-production demo credentials without committing real secrets.
- **The alternative:** Require manual setup, seed only generic examples, or mix invented records with client facts without labeling them.
- **Why this one:** Deterministic data makes the core workflow demonstrable quickly while preserving honesty about what came from the client. Local-only credentials support repeatable testing without creating a secret-management or production-authentication concern.
- **What would change my mind:** A reviewer requirement for an empty initial database, a prohibition on seeded users, or access to a validated non-demo identity provider.

## 18. Four-day implementation planning constraint

- **The gap:** The assignment materials contain conflicting timing signals: a “7 calendar days from receipt” deadline, submission “24 hours before scheduled interview,” and a separately communicated “4 days (to be confirmed with HR).” The client also said, “If it takes more than a few seconds they won't do it,” reinforcing the need for a focused workflow.
- **What I decided:** Plan and prioritize implementation against the four-day timebox. Treat the seven-day and interview-relative deadlines as unconfirmed logistics until verified with HR; do not expand product scope based on the longer possibility.
- **The alternative:** Plan against seven calendar days, or treat the interview deadline as the governing constraint.
- **Why this one:** The four-day constraint is the most conservative actionable planning input and protects a reliable core custody loop; deadline confirmation remains a logistics issue rather than a reason to add unvalidated features.
- **What would change my mind:** HR confirmation of a different binding deadline or a formal submission requirement that makes additional scope mandatory.