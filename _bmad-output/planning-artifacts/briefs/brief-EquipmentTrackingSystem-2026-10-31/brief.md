---
title: Equipment Tracking System Product Brief
status: complete
created: 2026-10-31
updated: 2026-10-31
---

# Product Brief: Equipment Tracking System

## Executive Summary

Horizon Survey Services, a land-surveying company in Lahore with approximately 25 employees and 14 field staff, needs a lightweight internal system that makes equipment custody visible without adding administrative friction for field staff. The immediate business problem is not simply inventory counting: it is uncertainty about who has equipment, whether it is available for another job, when it is due back, and whether it returned in usable condition.

The first release should prioritize a fast, mobile-usable checkout and return experience, a current equipment-location view for office staff, and a short history of custody. The solution must be deliberately simpler than a formal asset-management suite because the client has said that field workers will abandon a process that takes more than a few seconds.

## The Problem

- Equipment is taken to field sites on trust, without a shared system of record.
- Staff and the office cannot reliably answer who has a specific item.
- Equipment can remain in a vehicle or with a staff member for weeks without visibility.
- The company has bought a replacement after incorrectly believing a GPS rover was lost.
- Damage may be discovered only when the next person needs the item, delaying client work.
- The office receives status calls and needs a historical view of what was out and who held it.
- The drone requires approval from Hamza, and the new rover may require a future approval rule.
- Tripods and field tablets are quantity-tracked rather than individually tracked.

## Success Criteria

Initial success should be measured by whether the system can:

- Answer who currently has a selected individually tracked item.
- Show separate availability, approval-request, due/overdue, and unresolved-missing concepts through clear derived labels.
- Record a complete checkout and return with the responsible borrower or custodian and timestamps.
- Prevent restricted equipment from being checked out without the required approval.
- Represent pooled equipment by remaining quantity rather than forcing fake individual identities.
- Show a useful recent history for a selected item or period.
- Keep the common field workflow short enough that a user can complete it in a few seconds after authentication and equipment selection.

Numeric success targets and final demonstration data details remain to be set during implementation planning; the identity, reminder, and notification scope decisions are recorded in `DECISIONS.md`.

## The Solution

Build a simple internal web application that:

1. Shows current custody and availability for individual assets and pooled quantities.
2. Lets named users check out equipment with a fast due-date flow and optional declared location.
3. Applies approval to restricted equipment before checkout and records decisions.
4. Captures Good or Damaged at return; missing items remain associated with the borrower as unresolved custody records.
5. Provides due/overdue indicators, history, audit records, and a predefined survey-kit shortcut without creating a separate kit asset.

## Who This Serves

### Primary users

- **Field staff:** need to record a checkout or return in seconds, from a phone-sized device, with minimal typing.
- **Saima / office staff:** need a reliable view of current custody and history so they can answer questions without calling multiple people.

### Secondary users

- **Hamza:** needs to approve drone access as the licensed operator.
- **Tariq / manager:** needs visibility, accountability, and a way to control restricted equipment.

## Scope

### In scope

- Equipment catalogue with individually tracked and quantity-tracked equipment.
- Current availability and custodian view.
- Checkout, approval where required, return, and Good/Damaged condition capture.
- Due date and overdue status.
- Recent custody history.
- Four simple roles with multiple roles allowed per user: Field Staff, Office Staff, Approver, and Manager/Admin.
- Self-service field workflows with an office-staff override to create or correct records.
- Configurable restricted-equipment rules, with the drone requiring Hamza's approval.
- Standard relational custody records and an append-only audit log for meaningful corrections and approvals.
- Automatic borrower identity with optional declared location; no GPS tracking.
- Separate availability, approval-request, due/overdue, and unresolved-missing concepts, with simplified UI labels.
- A survey-kit shortcut that reuses existing component checkout records and is deferred if safe all-or-nothing processing cannot be demonstrated.
- Transactional prevention of duplicate individual-asset checkouts and pooled over-allocation.
- Clearly labeled deterministic demo data: invented users and roles, individual assets, four tripods, six field tablets, and a drone requiring Hamza's approval. Demo credentials are local-only and non-production.

### Deferred

- Automated WhatsApp or email reminders.
- GPS or live location tracking.
- Procurement, depreciation, or financial asset accounting.
- Full maintenance/work-order management.
- Offline synchronization.
- Hard-coded approval behavior beyond the configured restricted-equipment rules.
- SSO or enterprise identity integration.
- A full missing-item incident workflow beyond an open custody record.

These exclusions are deliberate first-release boundaries. Revisit them only when the specified conditions occur.

### Delivery constraints

- The eventual application must use .NET 8 or .NET 9 with C# and ASP.NET Core Web API.
- It must use a relational database, with SQLite or SQL Server LocalDB acceptable.
- The UI must be usable and runnable with one documented command or a short accurate setup.
- Reviewers should be able to run it within five minutes.
- Frontend technology remains an implementation choice for the later architecture phase; the proposed Angular preference is not a product requirement.

## Open Questions

- What evidence is needed for manager approval and auditability?
- Which equipment types and initial kit templates should be seeded for the demonstration?

## Vision

If the first release succeeds, Horizon can extend the same custody record into a dependable equipment operating picture: fewer replacement purchases, fewer avoidable site delays, and less time spent answering “who has it?” The system should remain lightweight enough for field use while gradually supporting richer reporting and maintenance decisions when the operational evidence justifies them.
