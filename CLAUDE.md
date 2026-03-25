# CLAUDE.md

## 1. Project Overview

This project implements an **Event Sourcing + CQRS architecture** with:

- Write database: PostgreSQL (source of truth)
- Read database: Azure Cosmos DB (denormalized projections)
- Messaging: Azure Service Bus (pub/sub)
- Backend: .NET 8 (ASP.NET Core)
- Frontend: React + TypeScript

The system follows:
- Event sourcing for all state changes
- CQRS for separation of write and read concerns
- Outbox pattern for reliable event publishing
- Asynchronous projections for read models

---

## 2. Core Architecture Principles

### 2.1 Source of Truth
- PostgreSQL event store is the **only source of truth**
- Read models (Cosmos DB) are **rebuildable**
- Never rely on read DB for business logic

### 2.2 Event Sourcing Rules
- Events are **immutable**
- Events are always **past tense** (e.g., `OrderCreated`)
- Never update or delete events
- New changes = new events

### 2.3 CQRS Separation
- Command API:
  - Handles business logic
  - Writes to PostgreSQL only
- Query API:
  - Reads from Cosmos DB only
  - No domain logic

### 2.4 Outbox Pattern
- Events + outbox messages must be written in **same transaction**
- Publishing must be handled by background worker
- No direct publishing from API

### 2.5 Eventual Consistency
- Read side is eventually consistent
- Do not try to make it strongly consistent

---

## 3. Coding Rules (STRICT)

### 3.1 General
- Follow Clean Architecture:
  - Domain
  - Application
  - Infrastructure
  - API
- Keep functions small and focused
- Prefer clarity over cleverness

### 3.2 Naming Conventions
- C# classes: `PascalCase`
- Variables: `camelCase`
- Events: `PastTense` (e.g., `OrderShipped`)
- Commands: `Imperative` (e.g., `CreateOrder`)

### 3.3 API Rules
- Always return structured responses
- Use consistent error format
- Validate input using FluentValidation
- No business logic in controllers

### 3.4 Time Handling
- Always use UTC (`DateTime.UtcNow`)
- Never use local time

### 3.5 Logging
- Use structured logging
- Include correlationId in logs

---

## 4. Event Design Rules

Each event must include:

- eventId
- streamId
- streamType
- version
- eventType
- occurredAt (UTC)
- correlationId
- causationId
- actor
- payload

### Example
```json
{
  "eventType": "OrderCreated",
  "occurredAt": "2026-03-24T10:00:00Z",
  "payload": {
    "orderId": "order-123",
    "customerId": "cust-1"
  }
}
```

---

## 5. Write Side Rules

### 5.1 Aggregate
- All business logic must live in aggregate
- Aggregates must enforce invariants

### 5.2 Event Store
- Use PostgreSQL
- Store events in `JSONB`
- Use optimistic concurrency (version check)

### 5.3 Command Handling
- Validate command
- Load aggregate from event stream
- Apply business rules
- Produce events
- Save events + outbox in one transaction

---

## 6. Read Side Rules

### 6.1 Projections
- Must be **idempotent**
- Must handle duplicate events safely
- Must not assume ordering guarantees beyond partition

### 6.2 Cosmos DB
- Store denormalized documents
- Design per query/use-case
- Do NOT normalize like relational DB

### 6.3 Query API
- Only read from Cosmos DB
- No domain logic
- Optimize for UI

---

## 7. Outbox & Messaging Rules

- Outbox table must be written in same transaction as events
- Publisher must:
  - retry on failure
  - support dead-letter handling
- Use Azure Service Bus Topics
- Each projection must use its own subscription

---

## 8. Testing Requirements (MANDATORY)

Every feature must include:

### 8.1 Unit Tests
- Aggregate behavior
- Event generation
- Validation logic

### 8.2 Integration Tests
- Event store append/load
- Outbox writing
- Projection updates

### 8.3 Projection Tests
- Idempotency
- Duplicate handling
- Partial failure recovery

### 8.4 API Tests
- Command endpoints
- Query endpoints

❗ Do not mark a task complete if tests are missing or failing

---

## 9. Security Rules

- Never hardcode secrets
- Use environment variables / Key Vault
- Do not expose internal data models
- Validate all inputs

---

## 10. What Claude MUST DO

When working on any task:

### ALWAYS:
- Read `architecture.md` before making changes
- Follow this CLAUDE.md strictly
- Work in small, incremental steps
- Add/update tests
- Update documentation if needed
- Explain all changes clearly

### NEVER:
- Rewrite large parts of system without approval
- Introduce new architecture patterns without discussion
- Mix read/write concerns
- Skip validation or error handling

---

## 11. Required Workflow (VERY IMPORTANT)

### Step 1 — PLAN FIRST
Claude MUST first respond with:

- goal
- files to change
- step-by-step plan
- risks
- test strategy

❗ DO NOT WRITE CODE YET

---

### Step 2 — WAIT FOR APPROVAL

Only after approval:
- proceed to implementation

---

### Step 3 — IMPLEMENT

- Make minimal, precise changes
- Follow architecture strictly
- Add tests

---

### Step 4 — REPORT

Return:

- files changed
- summary of changes
- commands run
- test results
- risks / TODOs

---

## 12. Definition of Done

A task is complete only if:

- Code compiles
- Tests pass
- Feature works end-to-end
- Architecture rules followed
- Documentation updated
- No shortcuts taken

---

## 13. Prompt Modes

Use these modes explicitly:

### Planner Mode
- Design only
- No file changes

### Builder Mode
- Implement approved plan

### Reviewer Mode
- Analyze code quality and risks

### Tester Mode
- Identify missing tests and edge cases

---

## 14. Project-Specific Rules (Event Sourcing)

- Never skip event creation
- Never update state directly
- Always rebuild state from events
- Always persist events before publishing
- Always use outbox

---

## 15. Preferred Implementation Order

1. Event store
2. Aggregate logic
3. Command API
4. Outbox
5. Messaging
6. Projection workers
7. Read models
8. Query API
9. UI integration

---

## 16. Repository Discipline

- Small commits only
- One feature per branch
- Never bundle large changes
- Always review diffs

---

## 17. If Unsure

Claude must:
- Ask questions
- Highlight assumptions
- Suggest options

Never guess silently.

---

## END OF FILE
