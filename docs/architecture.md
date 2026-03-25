# Event Sourcing Solution Design

## Project goal
Build a production-ready event sourcing application with:

- **Write side:** RDBMS for transactional consistency
- **Read side:** NoSQL for scalable query models
- **Pattern:** CQRS + Event Sourcing + Outbox + Asynchronous Projections
- **Cloud:** Azure managed services, serverless-first where practical
- **Developer experience:** suitable to paste into a vibe-coding or AI-assisted project folder as a living architecture spec

---

## 1. Recommended technology stack

### Core stack
- **Frontend:** React + TypeScript + TanStack Query
- **Backend APIs:** .NET 8 + ASP.NET Core Web API / Minimal API
- **Write database:** PostgreSQL
- **Read database:** Azure Cosmos DB for NoSQL
- **Message bus:** Azure Service Bus Topics + Subscriptions
- **Background processing:** Azure Container Apps jobs/workers or Azure Functions on Container Apps
- **Authentication:** Microsoft Entra ID
- **Secrets:** Azure Key Vault
- **Observability:** Azure Monitor + Application Insights + OpenTelemetry
- **Cache:** Azure Cache for Redis
- **CI/CD:** GitHub Actions or Azure DevOps
- **Infrastructure as Code:** Terraform or Bicep

### Why this is the best fit
- **PostgreSQL** is ideal for the write side because the write model needs strict transactions, concurrency control, and durable storage of events.
- **Cosmos DB** is ideal for the read side because read models are usually denormalized, JSON-based, query-specific, and horizontally scalable.
- **Azure Service Bus** is the safest managed choice for durable pub/sub event distribution across multiple read projections and downstream handlers.
- **Azure Container Apps** is a strong hosting option when you want container flexibility with lower operational overhead than AKS.
- **.NET 8** is a mature choice for CQRS, event-driven services, clean APIs, background workers, and Azure-native integrations.

---

## 2. Architecture principles

This design follows these principles:

1. **The write database is the source of truth.**
2. **The read database is disposable and rebuildable.**
3. **Events are immutable facts.**
4. **Commands change state; queries never do.**
5. **Read models are optimized for screens/use cases, not normalization.**
6. **Messaging and projections are asynchronous and eventually consistent.**
7. **Every projection must be idempotent.**
8. **Publishing events must be reliable even if infrastructure fails.**

---

## 3. Solution overview

```text
┌────────────────────────────┐
│        React Frontend      │
│  Web app / Admin portal    │
└─────────────┬──────────────┘
              │ HTTPS
              v
┌────────────────────────────┐
│   API Gateway / BFF        │
│  Auth, routing, shaping    │
└──────────┬─────────┬───────┘
           │         │
           │         │
           v         v
┌────────────────┐  ┌────────────────┐
│  Command API   │  │   Query API    │
│  .NET 8        │  │   .NET 8       │
└───────┬────────┘  └───────┬────────┘
        │                    │
        │ writes events      │ reads views
        v                    v
┌──────────────────────────────────────┐
│ PostgreSQL                           │
│ - event_store                        │
│ - snapshots (optional)               │
│ - outbox_messages                    │
│ - command-side metadata tables       │
└───────────────┬──────────────────────┘
                │
                │ outbox polling / transactionally safe publish
                v
┌──────────────────────────────────────┐
│ Outbox Publisher Worker              │
│ .NET background service              │
└───────────────┬──────────────────────┘
                │ publishes
                v
┌──────────────────────────────────────┐
│ Azure Service Bus Topic              │
└───────┬────────────────┬─────────────┘
        │                │
        v                v
┌───────────────┐  ┌───────────────┐
│ Projection A  │  │ Projection B  │
│ Order View    │  │ Customer View │
└──────┬────────┘  └──────┬────────┘
       │                  │
       └────────┬─────────┘
                v
┌──────────────────────────────────────┐
│ Azure Cosmos DB                      │
│ Denormalized read projections        │
└──────────────────────────────────────┘
```

---

## 4. Why RDBMS for write and NoSQL for read

### Write side: PostgreSQL
Use PostgreSQL as the write store because the command side needs:
- ACID transactions
- strong consistency
- concurrency control
- durable append of events
- support for optimistic locking by stream version
- reliable writes for event store and outbox in one transaction

### Read side: Cosmos DB
Use Cosmos DB as the read store because the query side needs:
- denormalized JSON documents
- flexible schema per screen or report
- horizontal scaling for reads
- simple, fast document retrieval
- separate containers or partitions for different query models

This separation is exactly the practical value of **CQRS**:
- write side = correctness and invariants
- read side = speed and user-friendly query models

---

## 5. Why Service Bus sits in the middle

Azure Service Bus is used between write and read sides for these reasons:

- decouples command processing from projection updates
- supports multiple subscribers per event
- provides durable, reliable asynchronous delivery
- supports retries and dead-letter queues
- protects the write side from direct dependency on read-side availability
- lets you add more consumers later without changing the command side

Typical consumers:
- read-model projections
- notifications
- audit export
- analytics pipeline
- search indexing
- integration with external systems

---

## 6. Recommended domain pattern

Use the following architecture:

### Command side
- receives commands
- loads aggregate state from historical events (plus snapshot if needed)
- validates business rules
- emits new domain events
- appends events to PostgreSQL
- writes the same events to the outbox in the same database transaction

### Query side
- reads only from Cosmos DB read models
- never writes to aggregates
- returns UI-ready, denormalized payloads

### Messaging side
- outbox publisher sends events from PostgreSQL to Azure Service Bus
- projection workers consume events and update Cosmos DB documents

---

## 7. Detailed component design

## 7.1 Frontend
**Technology:** React + TypeScript

### Responsibilities
- login and token handling
- submit commands to Command API
- fetch views from Query API
- display near-real-time status from read models
- show eventual consistency states where needed

### Recommended packages
- React Router
- TanStack Query
- Zod for client-side schema validation
- Material UI or shadcn/ui for components

---

## 7.2 API Gateway / BFF
Optional but useful.

### Responsibilities
- route calls to Command API and Query API
- hide internal microservice layout from frontend
- enforce auth/session logic
- aggregate or reshape query responses for the UI

If the first version is small, you can skip a separate gateway and expose Command API and Query API directly behind a reverse proxy.

---

## 7.3 Command API
**Technology:** .NET 8 + ASP.NET Core

### Responsibilities
- authenticate and authorize users
- validate commands
- load aggregate stream
- enforce business invariants
- create events
- persist event stream
- write outbox records
- return command acceptance response

### Internal layers
- API layer
- Application layer (command handlers)
- Domain layer (aggregates, value objects, domain services)
- Infrastructure layer (PostgreSQL, outbox, Service Bus integration contracts)

### Suggested libraries
- MediatR for command handling
- FluentValidation for request validation
- Dapper or Npgsql for event store access
- Serilog or OpenTelemetry logging

---

## 7.4 PostgreSQL write database
**Role:** system of record

### Core tables

#### `event_store`
```sql
create table event_store (
    event_id uuid primary key,
    stream_id varchar(200) not null,
    stream_type varchar(100) not null,
    version int not null,
    event_type varchar(200) not null,
    event_data jsonb not null,
    metadata jsonb not null,
    created_at timestamptz not null default now(),
    unique(stream_id, version)
);
```

#### `snapshots` (optional)
```sql
create table snapshots (
    stream_id varchar(200) primary key,
    version int not null,
    snapshot_data jsonb not null,
    created_at timestamptz not null default now()
);
```

#### `outbox_messages`
```sql
create table outbox_messages (
    message_id uuid primary key,
    event_id uuid not null,
    topic_name varchar(200) not null,
    payload jsonb not null,
    headers jsonb not null,
    status varchar(50) not null,
    retry_count int not null default 0,
    created_at timestamptz not null default now(),
    published_at timestamptz null
);
```

### Indexing recommendations
```sql
create index ix_event_store_stream_id on event_store(stream_id, version);
create index ix_outbox_status_created_at on outbox_messages(status, created_at);
create index ix_event_store_event_type on event_store(event_type);
```

### Why this model works
- append-only history
- version-based concurrency control
- JSONB keeps event shape flexible
- outbox supports safe async publishing

---

## 7.5 Aggregate pattern

### Example aggregate: `Order`
Commands:
- `CreateOrder`
- `AddItem`
- `ConfirmOrder`
- `CancelOrder`
- `ShipOrder`

Events:
- `OrderCreated`
- `OrderItemAdded`
- `OrderConfirmed`
- `OrderCancelled`
- `OrderShipped`

### Aggregate rules
- cannot add items after confirmation
- cannot ship before confirmation
- cannot confirm empty order
- cannot cancel shipped order

### Aggregate process
1. Rebuild state from stream
2. Apply command
3. Emit one or more events
4. Append with expected stream version

---

## 7.6 Event schema guidance

Each event should contain:

```json
{
  "eventId": "uuid",
  "streamId": "order-1001",
  "streamType": "Order",
  "version": 4,
  "eventType": "OrderConfirmed",
  "occurredAt": "2026-03-23T10:00:00Z",
  "correlationId": "uuid",
  "causationId": "uuid",
  "actor": {
    "userId": "u123",
    "displayName": "Felicia"
  },
  "schemaVersion": 1,
  "payload": {
    "orderId": "order-1001",
    "confirmedAt": "2026-03-23T10:00:00Z"
  }
}
```

### Event naming rules
Use **past tense business facts**:
- `OrderCreated`
- `InvoiceIssued`
- `PaymentCaptured`

Avoid technical names like:
- `OrderTableUpdated`
- `RecordChanged`

---

## 7.7 Outbox publisher worker
**Technology:** .NET Worker Service in Container Apps

### Responsibilities
- poll `outbox_messages` with status `Pending`
- publish event payload to Service Bus topic
- update row status to `Published`
- retry failures with backoff
- dead-letter or mark poison messages after retry threshold

### Why outbox is required
Without outbox, this failure can happen:
1. DB transaction commits
2. app crashes before message publish
3. write side is correct, but read side never updates

Outbox solves this by storing publish work in the same transaction as event persistence.

---

## 7.8 Azure Service Bus topic design

### Topic naming
Use one domain topic or bounded-context topic, for example:
- `sales-events`
- `billing-events`
- `customer-events`

### Subscription examples
- `order-summary-projection`
- `customer-orders-projection`
- `notification-handler`
- `analytics-export`

### Message metadata
Headers should include:
- `eventType`
- `streamId`
- `streamType`
- `version`
- `correlationId`
- `causationId`
- `occurredAt`
- `schemaVersion`

### Delivery guidance
- use at-least-once semantics
- require idempotent consumers
- enable dead-letter queue handling

---

## 7.9 Projection workers
**Technology:** .NET Worker or Azure Functions on Container Apps

### Responsibilities
- subscribe to Service Bus subscription
- deserialize event
- check if event was already processed
- update or upsert a Cosmos DB read model
- store checkpoint / processed marker if needed

### Projection design rules
- keep each projection small and single-purpose
- projections must be replayable
- consumers must tolerate duplicate delivery
- projection failures must not corrupt the event store

### Idempotency options
- store processed event IDs in Cosmos DB or PostgreSQL
- use version checks inside the read model
- upsert only when incoming version is newer

---

## 7.10 Cosmos DB read side

### Read model philosophy
Do **not** mirror the relational schema.
Build documents around what the UI needs.

### Example read model 1: Order Summary
```json
{
  "id": "order-1001",
  "orderId": "order-1001",
  "customerId": "cust-55",
  "status": "Confirmed",
  "itemsCount": 3,
  "total": 420.00,
  "lastEventVersion": 4,
  "lastUpdated": "2026-03-23T10:10:00Z"
}
```

### Example read model 2: Customer Orders
```json
{
  "id": "cust-55",
  "customerId": "cust-55",
  "customerName": "Felicia",
  "orders": [
    {
      "orderId": "order-1001",
      "status": "Confirmed",
      "total": 420.00,
      "updatedAt": "2026-03-23T10:10:00Z"
    }
  ],
  "lastUpdated": "2026-03-23T10:10:00Z"
}
```

### Container strategy
Good options:
- one container per major read model
- partition key based on main access path

Examples:
- `/customerId` for customer-based views
- `/orderId` for order-heavy detail queries
- `/tenantId` for multi-tenant systems

---

## 7.11 Query API
**Technology:** .NET 8 + ASP.NET Core

### Responsibilities
- read from Cosmos DB only
- filter, paginate, sort, and shape results
- never apply domain business rules that belong to the write side
- return UI-friendly payloads

### Example endpoints
- `GET /orders/{id}`
- `GET /customers/{id}/orders`
- `GET /dashboard/orders?status=Confirmed`

---

## 8. End-to-end flow

## 8.1 Command flow
1. User submits a command from React
2. Command API validates request and auth
3. Aggregate stream is loaded from PostgreSQL
4. Domain logic runs
5. New events are generated
6. Events are appended to `event_store`
7. Matching outbox rows are inserted into `outbox_messages`
8. Transaction commits
9. Response returns command accepted / resource id / current version

## 8.2 Publish flow
1. Outbox worker reads `Pending` rows
2. Publishes each message to Service Bus topic
3. Marks row as `Published`
4. On failure, increments retry count
5. After threshold, route to operational handling

## 8.3 Projection flow
1. Projection worker receives event
2. Reads current document from Cosmos DB if needed
3. Applies transformation
4. Upserts latest document
5. Records processed version/event id

## 8.4 Query flow
1. Frontend requests view
2. Query API reads document(s) from Cosmos DB
3. Returns denormalized view model

---

## 9. Example event lifecycle

### Commands and events
```text
CreateOrder      -> OrderCreated
AddItem          -> OrderItemAdded
AddItem          -> OrderItemAdded
ConfirmOrder     -> OrderConfirmed
ShipOrder        -> OrderShipped
```

### Stored event stream for `order-1001`
```text
v1 OrderCreated
v2 OrderItemAdded
v3 OrderItemAdded
v4 OrderConfirmed
v5 OrderShipped
```

### Read model result
```text
Order 1001
Status: Shipped
Items: 2
Total: 420.00
```

---

## 10. Rebuild and replay strategy

One of the biggest benefits of event sourcing is that read models can be rebuilt.

### Why Cosmos DB is rebuildable
Because it is **not** the source of truth.
If a read model is wrong, outdated, or redesigned:
1. clear the read container
2. replay events from PostgreSQL in order
3. regenerate projections

### Replay worker responsibilities
- read streams in order
- republish historical events or call projection handlers directly
- throttle replay to protect Cosmos RU budget
- support full rebuild and selective rebuild

### When to rebuild
- projection bug fixed
- new read model introduced
- schema design changes
- corrupted read data

---

## 11. Hosting design on Azure

## Preferred hosting option: Azure Container Apps
Use Container Apps for:
- Command API
- Query API
- Outbox Publisher Worker
- Projection Workers

### Why Container Apps
- serverless-style scaling
- less infrastructure management than AKS
- container portability
- easy split between APIs and workers
- good fit for moderate-to-large enterprise services

### When to use AKS instead
Use AKS only if you need:
- highly customized Kubernetes networking/policies
- service mesh or deep Kubernetes operations
- large platform engineering standards already built on AKS

For most teams starting an event-sourced platform, Container Apps is simpler.

---

## 12. Recommended Azure resources

- **Azure Container Apps Environment**
- **Azure Database for PostgreSQL Flexible Server**
- **Azure Cosmos DB for NoSQL**
- **Azure Service Bus Premium**
- **Azure Cache for Redis**
- **Azure Key Vault**
- **Azure Monitor + Application Insights**
- **Microsoft Entra ID**
- **Log Analytics Workspace**
- **Storage Account** for diagnostics and optional backup tooling
- **Azure Front Door / API Management** if internet-facing at scale

### Why Service Bus Premium
For production use, Premium is preferred when you need stronger isolation, scale, and predictable performance.

---

## 13. Security design

### Identity and access
- authenticate users with Entra ID
- use managed identity for services talking to Azure resources
- use role-based access control for resource permissions

### Secrets and config
- store secrets in Key Vault
- inject into apps using managed identity
- do not store secrets in repo or pipeline variables when avoidable

### Network
- use private endpoints where required
- restrict database/firewall access
- separate dev, test, and prod environments

### Data protection
- encrypt in transit with TLS
- rely on platform encryption at rest
- mask sensitive fields in logs

---

## 14. Observability design

Use OpenTelemetry + Application Insights.

### Trace each request with
- `correlationId`
- `causationId`
- `streamId`
- `eventId`
- `userId` where appropriate

### Dashboard suggestions
- command success/failure rate
- outbox backlog size
- publish latency
- projection lag
- dead-letter count
- Cosmos RU consumption
- PostgreSQL CPU/storage/connections

### Alerts
- projection worker repeatedly failing
- outbox backlog above threshold
- dead-letter queue growth
- query latency spike
- PostgreSQL connection saturation

---

## 15. Reliability and failure handling

### Reliability rules
- command write and outbox insert happen in one transaction
- never publish directly inside request transaction without outbox fallback
- consumers must be idempotent
- failed messages must be observable and recoverable

### Failure scenarios and response

#### Scenario: DB commit succeeds, publish fails
**Mitigation:** outbox publisher retries later

#### Scenario: message delivered twice
**Mitigation:** idempotent projection handler using version/eventId checks

#### Scenario: projection code bug corrupts read model
**Mitigation:** fix code and replay events

#### Scenario: read store unavailable
**Mitigation:** write side still works; projection catches up later

#### Scenario: one projection fails but another succeeds
**Mitigation:** separate subscriptions isolate failures

---

## 16. Performance design

### PostgreSQL
- use append-only event table
- load streams by indexed `stream_id`
- add snapshots for very long-lived aggregates
- use connection pooling

### Cosmos DB
- design partition keys from query patterns
- denormalize heavily for hot queries
- keep documents bounded in size
- monitor RU costs and adjust projection shape

### APIs
- cache hot reference queries in Redis if needed
- paginate list queries
- separate large dashboards into smaller read models

---

## 17. Multi-tenant design considerations

If this becomes a SaaS platform, add:
- `tenantId` on every command, event, and document
- tenant-scoped authorization
- tenant-based partitioning strategy
- optionally separate Cosmos containers or databases by environment/tenant tier
- consider schema or database isolation in PostgreSQL for strict compliance needs

### Recommended default
Start with **shared infrastructure, tenant-aware data model**:
- event stream includes `tenantId`
- Cosmos partition key can be `/tenantId`
- service-layer auth ensures tenant isolation

---

## 18. Suggested repository structure

```text
repo/
  docs/
    architecture/
      solution-design.md
      adr-001-use-postgresql-for-write-store.md
      adr-002-use-cosmos-for-read-models.md
      adr-003-use-service-bus-and-outbox.md
  infra/
    terraform/
    bicep/
  src/
    frontend-web/
    api-command/
    api-query/
    worker-outbox-publisher/
    worker-projection-order-summary/
    worker-projection-customer-orders/
    shared-building-blocks/
      Domain/
      Application/
      Infrastructure/
      Contracts/
      Observability/
  tests/
    unit/
    integration/
    contract/
    replay/
```

---

## 19. Suggested clean architecture layout for backend

```text
src/api-command/
  Api/
  Application/
    Commands/
    Validators/
    Behaviors/
  Domain/
    Aggregates/
    Events/
    ValueObjects/
  Infrastructure/
    Persistence/
    Messaging/
    Identity/
```

### Notes
- keep aggregates free from Azure SDK dependencies
- keep event definitions in contracts/shared package if multiple services use them
- keep projection handlers separate from write-side domain model

---

## 20. Step-by-step build roadmap

## Phase 1 - foundation
1. Create repo and folder structure
2. Set up React frontend
3. Set up .NET 8 Command API and Query API
4. Provision local dev stack with Docker Compose
5. Add PostgreSQL, Cosmos emulator or dev account, and Service Bus namespace
6. Add basic CI pipeline
7. Configure Key Vault and App Insights

## Phase 2 - event store
1. Create `event_store`, `snapshots`, `outbox_messages`
2. Implement append-to-stream logic
3. Implement load-stream logic
4. Add optimistic concurrency checks
5. Create first aggregate and first command handler

## Phase 3 - outbox and messaging
1. Implement transactional outbox write
2. Build outbox publisher worker
3. Create Service Bus topic and subscriptions
4. Add retry, backoff, poison handling

## Phase 4 - first read projection
1. Design first UI screen
2. Create one Cosmos read model for that screen
3. Build one projection worker
4. Add Query API endpoint
5. Hook frontend to the query endpoint

## Phase 5 - replay and operations
1. Add replay/rebuild tool
2. Add observability dashboards
3. Add DLQ monitoring and alarms
4. Add backup and restore plan
5. Add load testing

## Phase 6 - production hardening
1. Add managed identities
2. Add private networking where needed
3. Tune PostgreSQL and Cosmos partitions/indexing
4. Add chaos/failure scenario tests
5. Document SRE runbooks

---

## 21. Local development setup

### Recommended local stack
- .NET 8 SDK
- Node.js LTS
- Docker Desktop
- PostgreSQL container
- Azurite optional for storage needs
- Service Bus emulator alternative is limited, so use dev Azure namespace if possible
- Cosmos DB emulator if your team works mainly on Windows, otherwise use Azure dev account

### Example local `docker-compose.yml`
```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_USER: app
      POSTGRES_PASSWORD: app
      POSTGRES_DB: appdb
    ports:
      - "5432:5432"
```

---

## 22. Testing strategy

### Unit tests
- aggregate command handling
- domain rules
- event application methods

### Integration tests
- append/load stream from PostgreSQL
- outbox transaction behavior
- Service Bus publish workflow
- Cosmos projection upsert logic

### Replay tests
- build read model from historical event stream
- validate deterministic projection output

### Contract tests
- event payload schema compatibility
- query API response stability

---

## 23. Event versioning strategy

Events evolve over time.

### Rules
- do not mutate historical events in storage
- add `schemaVersion`
- support upcasters or compatibility mappers where needed
- keep consumers backward compatible during deployment windows

### Example
- `OrderCreated v1` had `customerName`
- `OrderCreated v2` uses `customerId`

Projection handler can map old shapes into the current internal format.

---

## 24. Snapshot strategy

Use snapshots only when stream replay becomes too expensive.

### Good fit for snapshots
- very long-lived aggregates
- aggregates with hundreds or thousands of events
- hot streams loaded frequently

### Snapshot rules
- snapshot every N events, for example every 100
- snapshot stores state + latest version
- rebuild from snapshot + subsequent events

Do not add snapshots too early. Start without them unless performance proves they are needed.

---

## 25. Example ADRs to add later

### ADR-001: Use PostgreSQL as event store
Reason:
- transactional consistency
- strong version checking
- mature managed Azure service

### ADR-002: Use Cosmos DB for read models
Reason:
- denormalized JSON documents
- scalable query store
- flexible model per screen

### ADR-003: Use Service Bus + Outbox
Reason:
- reliable asynchronous event delivery
- independent consumers
- safe failure recovery

### ADR-004: Use Container Apps instead of AKS initially
Reason:
- lower operational overhead
- faster platform delivery
- enough capability for first production version

---

## 26. Recommended first use case for a POC

A good POC domain is **Order Management** because it clearly demonstrates:
- aggregates
- command validation
- event history
- read projections
- eventual consistency
- multi-subscriber event fan-out

### POC scope
Commands:
- CreateOrder
- AddItem
- ConfirmOrder
- CancelOrder

Queries:
- GetOrderById
- GetOrdersByCustomer
- GetDashboardSummary

Read models:
- OrderSummary
- CustomerOrders
- DashboardCounters

---

## 27. Minimum viable production backlog

- [ ] repo scaffold complete
- [ ] infrastructure provisioned
- [ ] auth integrated
- [ ] first aggregate implemented
- [ ] event store working
- [ ] outbox working
- [ ] Service Bus topic/subscriptions created
- [ ] one read projection working
- [ ] one replay tool working
- [ ] dashboards and alerts configured
- [ ] CI/CD pipeline deployed
- [ ] runbook for DLQ handling documented

---

## 28. Final recommendation

### Best overall stack
- **Frontend:** React + TypeScript
- **Backend:** .NET 8
- **Write side:** PostgreSQL
- **Messaging:** Azure Service Bus Premium
- **Read side:** Azure Cosmos DB for NoSQL
- **Hosting:** Azure Container Apps
- **Identity & Security:** Entra ID + Key Vault + Managed Identity
- **Observability:** Application Insights + Azure Monitor

### Final architecture decision
Use **CQRS + Event Sourcing + PostgreSQL Event Store + Transactional Outbox + Azure Service Bus + Cosmos DB Read Projections**.

This gives you:
- strong transactional writes
- flexible and scalable reads
- full event history and auditability
- replay/rebuild capability
- low-ops managed Azure architecture
- a clean path from POC to production

---

## 29. References

These references were used to validate current product suitability and platform capabilities:

- Azure Container Apps overview: https://learn.microsoft.com/en-us/azure/container-apps/overview
- Azure Service Bus queues, topics, and subscriptions: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-queues-topics-subscriptions
- Azure Service Bus messaging overview: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messaging-overview
- Azure Service Bus Premium messaging tier: https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-premium-messaging
- Azure Cosmos DB change feed: https://learn.microsoft.com/en-us/azure/cosmos-db/change-feed
- Use Change Feed with Azure Functions: https://learn.microsoft.com/en-us/azure/cosmos-db/change-feed-functions
- Azure Database for PostgreSQL overview: https://learn.microsoft.com/en-us/azure/postgresql/overview
- Azure PostgreSQL architecture best practices: https://learn.microsoft.com/en-us/azure/well-architected/service-guides/postgresql
- .NET 8 downloads: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

