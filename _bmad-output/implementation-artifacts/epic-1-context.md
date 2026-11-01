# Epic 1 Context: Open the System and See Trusted Inventory

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver a runnable local vertical slice that uses the approved stack, supports named role-aware login, seeds deterministic demo data, provides catalogue setup for individual and pooled equipment, and exposes trustworthy availability and custody information.

## Stories

- Story 1.1: Start the seeded review environment
- Story 1.2: Sign in with named roles
- Story 1.3: Create individual and pooled equipment
- Story 1.4: View availability and current custody

## Requirements & Constraints

- A single documented `scripts/start-review.ps1` command must restore dependencies, build Angular, use the .NET 8 API, apply SQLite migrations, load idempotent seed data, and start a reviewable process within five minutes.
- The local review environment must require no external database, cloud service, secret, or manual database step. Repeated startup and SQLite reset must produce deterministic schema and data without duplicates.
- Seeded local credentials must establish an authenticated session; `/api/auth/me` must return the named user and roles. Invalid credentials and unauthenticated protected requests must fail safely with `401`, without credential disclosure.
- Authorization must be enforced server-side for Field Staff, Office Staff, Approver, and Manager/Admin roles; client route guards alone are insufficient.
- Individual assets require a unique asset ID and start Good/Available with no open custody. Pooled equipment requires a positive integer total and starts with Available equal to Total and zero Checked-Out and Damaged.
- Reject duplicate IDs, invalid pool quantities, invalid references, and unauthorized operations with appropriate client-visible errors (including `400` for invalid create requests).
- Deterministic seed data must include four tripods, six field tablets, a usable individual demo asset, and the drone with its restricted rule data.
- Availability projections must reflect authoritative database state. For pooled equipment, preserve `Available + Checked-Out + Damaged = Total` and do not invent individual asset identities.
- Verification must cover startup, migration, seed idempotency, health/readiness, login/logout, role claims, `401`/`403`, attribution, catalogue validation, projections, and UI behavior.

## Technical Decisions

- Use the approved .NET 8 API and Angular 21 client with SQLite for the local review environment.
- Use migrations and deterministic, idempotent initialization rather than manual database setup.
- Model individual and pooled equipment as distinct tracking modes: individual records expose custody and condition; pooled records expose quantity counters.
- Treat the database as the authority for availability and custody projections.
- Use authenticated named users and server-enforced role authorization for all protected operations.

## UX & Interaction Patterns

- Provide a searchable catalogue usable by any authenticated user.
- Individual equipment views show availability, custodian, condition, due/overdue state, approval, and unresolved-custody indicators.
- Pooled equipment views show Total, Available, Checked-Out, and Damaged quantities.
- Support phone-sized and desktop viewports with usable labels, keyboard focus, contrast, loading, empty, validation, and API error states.

## Cross-Story Dependencies

- Story 1.1 has no dependencies.
- Story 1.2 depends on Story 1.1.
- Story 1.3 depends on Story 1.2.
- Story 1.4 depends on Story 1.3.
