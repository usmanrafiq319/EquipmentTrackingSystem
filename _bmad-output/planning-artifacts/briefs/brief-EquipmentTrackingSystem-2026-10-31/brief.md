---
title: Equipment Tracking System Product Brief
status: draft
created: 2026-10-31
updated: 2026-10-31
---

# Product Brief: Equipment Tracking System

## Executive Summary

Horizon Survey Services needs a lightweight internal system that makes equipment custody visible without adding administrative friction for field staff. The immediate business problem is not simply inventory counting: it is uncertainty about who has equipment, whether it is available for another job, when it is due back, and whether it returned in usable condition.

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

## The Solution

Build a simple internal web application that:

1. Shows the current status and custodian of equipment.
2. Lets an authorized user check out an individual item or a quantity of pooled equipment.
3. Captures an expected return date and supports return confirmation.
4. Captures condition or damage at return so an item is not silently presented as ready.
5. Applies approval to restricted equipment.
6. Provides a recent history view for office staff.
7. Supports reminders through an intentionally scoped notification approach.

## Who This Serves

### Primary users

- **Field staff:** need to record a checkout or return in seconds, from a phone-sized screen, with minimal typing.
- **Saima / office staff:** need a reliable current view and history so they can answer questions without calling multiple people.

### Secondary users

- **Hamza:** needs to approve drone access as the licensed operator.
- **Tariq / manager:** needs visibility, accountability, and a way to control restricted equipment.

## Success Criteria

Initial success should be measured by whether the system can:

- Answer who currently has a selected individually tracked item.
- Distinguish available, checked-out, overdue, pending approval, and damaged/unavailable states.
- Record a complete checkout and return with the responsible person and timestamps.
- Prevent restricted equipment from being checked out without the required approval.
- Represent pooled equipment by remaining quantity rather than forcing fake individual identities.
- Show a useful recent history for a selected item or period.
- Keep the common field workflow short enough that a user can complete it in a few seconds after authentication and equipment selection.

The exact numeric targets, notification channel, identity model, and reminder behavior remain open decisions for discovery.

## Scope

### Candidate first-release scope

- Equipment catalogue with individually tracked and quantity-tracked equipment.
- Current availability and custodian view.
- Checkout, approval where required, return, and condition/damage capture.
- Due date and overdue status.
- Recent custody history.
- Minimal role and permission model for field staff, office staff, manager, and approver.

### Explicitly not yet committed

- Automated WhatsApp integration.
- GPS or live location tracking.
- Procurement, depreciation, or financial asset accounting.
- Full maintenance/work-order management.
- Offline synchronization.
- Complex multi-level approval workflows.
- SSO or enterprise identity integration.

These exclusions are boundaries for analysis, not final decisions, until the relevant assumptions and alternatives are reviewed.

## Open Questions

- What is the smallest acceptable authentication flow for the field team?
- Is checkout self-service, office-entered, or both?
- Should a person be allowed to hold multiple active checkouts of the same item type?
- What does “reminds people” mean for the first release: in-app status, email, WhatsApp, or a manual office workflow?
- Does a return require a condition selection every time, or only when damaged?
- What evidence is needed for manager approval and auditability?
- Should the “new rover maybe” rule be configurable by an administrator or left as a documented future change?
- What exact submission scope can be implemented and demonstrated reliably within the assignment timebox?
