# Epic 2 Context: Complete Core Custody Flows

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the core custody lifecycle so authenticated users can check equipment out and return it while inventory, borrower projections, approvals, and transactional consistency remain trustworthy.

## Stories

- Story 2.1: Establish shared custody and approval-gate foundations
- Story 2.2: Check out equipment with due and attribution
- Story 2.3: Return equipment with immutable events
- Story 2.4: Use the responsive custody workflow

## Requirements & Constraints

- All checkout and return mutations must use backend-owned application services and server-derived identity and roles.
- Individual assets have one active custody record; pooled assets use integer quantities and preserve `Available + CheckedOut + Damaged = Total`.
- Returns must preserve original checkout facts, append immutable return evidence, support full and partial returns, and keep outstanding custody accurate.
- Failed validation, authorization, duplicate, over-allocation, over-return, or write-conflict operations must leave inventory, custody, and event state unchanged and return an explicit client-visible error.
- SQLite mutations must be atomic, reload authoritative state inside the write transaction, and translate concurrency or write conflicts to retryable responses.
- Catalogue and custody projections must be refreshed from authoritative persisted state after successful mutations.

## Technical Decisions

- Use the .NET 8 modular monolith, EF Core, SQLite, ASP.NET Identity, and an injected `TimeProvider`.
- Feature application services own transaction boundaries; HTTP endpoints only map request and identity data to commands and map typed results.
- Keep individual condition/lifecycle state separate from pooled quantity counters.
- Model return history relationally as append-only `ReturnEvent` rows related to a custody record; do not overwrite checkout or earlier return facts.
- Retain filtered uniqueness for active individual custody and the existing equipment version concurrency token.

## Cross-Story Dependencies

- Story 2.3 depends on the shared checkout/custody model and transaction conventions established by Stories 2.1 and 2.2.
- Story 2.4 depends on authoritative checkout and return API projections.
- Restricted approval remains a checkout gate; return implementation must not bypass or alter it.
