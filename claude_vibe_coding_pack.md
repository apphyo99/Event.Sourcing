# CLAUDE.md (Project Instructions)

## Project Overview
This project implements an Event Sourcing + CQRS architecture using:
- .NET 8 (ASP.NET Core)
- PostgreSQL (write/event store)
- Azure Service Bus (event distribution)
- Cosmos DB (read models)
- React (frontend)

## Architecture Rules
- Follow CQRS: commands ≠ queries
- PostgreSQL is the **source of truth**
- Cosmos DB is **rebuildable**
- Use Outbox Pattern for event publishing
- All projections must be **idempotent**
- Events are **immutable**

## Coding Standards
- Clean Architecture (Domain / Application / Infrastructure)
- PascalCase for C# classes
- camelCase for JSON
- UTC timestamps only
- No business logic in controllers

## Event Rules
- Use past tense (OrderCreated, PaymentCaptured)
- Include metadata:
  - eventId
  - correlationId
  - causationId
  - occurredAt
  - actor

## Testing Rules
- Every feature must include:
  - unit tests (domain)
  - integration tests (DB/event store)
  - projection tests
- Tests must pass before completion

## Security Rules
- No secrets in code
- Use environment variables / Key Vault
- Validate all inputs

## Definition of Done
- Code implemented
- Tests passing
- Docs updated
- No architecture violations


---

# PROMPT PACK

## 1. Plan Feature
Read architecture.md and CLAUDE.md.
Do NOT write code.

Return:
- goal
- files to change
- step-by-step plan
- risks
- tests required


## 2. Implement Feature
Implement the approved plan.
- Keep changes minimal
- Follow architecture rules
- Add tests

Return:
- files changed
- test results
- risks


## 3. Review Code
Act as a reviewer.

Check:
- event sourcing correctness
- outbox reliability
- idempotency
- error handling
- test coverage


## 4. Refactor
Refactor for clarity only.
- Do NOT change behavior
- Keep contracts unchanged


---

# DEVELOPMENT WORKFLOW

## Phase 1: Setup
- Create repo structure
- Add CLAUDE.md
- Add architecture.md
- Setup CI/CD

## Phase 2: Write Side
- Aggregate
- Commands
- Events
- Event store (PostgreSQL)

## Phase 3: Outbox
- Outbox table
- Publisher worker
- Service Bus integration

## Phase 4: Read Side
- Projection workers
- Cosmos DB models

## Phase 5: Query API
- Read endpoints
- UI integration

## Phase 6: Non-functional
- Logging
- Monitoring
- Retry + DLQ
- Replay support


---

# DAILY WORKFLOW

1. Ask Claude for PLAN only
2. Approve plan
3. Implement small slice
4. Run tests
5. Review changes
6. Commit


---

# GIT STRATEGY

- One feature per branch
- Small commits
- Review diffs before merge

Example:
- feature/order-created
- feature/projection-worker


---

# BEST PRACTICES

- Never generate large code in one step
- Always review before merge
- Keep architecture stable
- Prefer simplicity over over-engineering
