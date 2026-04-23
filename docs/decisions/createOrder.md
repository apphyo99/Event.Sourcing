# Decision: Create Order — acceptance criteria

## Scope

“Create order” is the command that starts a new **Order** aggregate in **Draft** status, persists an **OrderCreated** domain event (append-only), and returns the new order identifier to the caller. Read-model availability follows the platform’s **eventual consistency** guarantees (outbox → messaging → projections).

---

## API contract

1. **Endpoint:** `POST /api/v1/orders` (versioned Command API).
2. **Request body (JSON):** exactly one business field from the client:
   - `customerId` (string, required): non-empty after trim; identifies the purchasing customer.
3. **Successful response:** HTTP **200 OK** with JSON body `{ "orderId": "<id>" }`.
4. **Order id format:** `order-` followed by a 32-character lowercase hex string (GUID without dashes), matching `OrderId.New()` generation.
5. **Error responses:**
   - **400 Bad Request** when validation fails (e.g. missing or empty `customerId`, or invalid command metadata after pipeline validation).
   - **401 Unauthorized** when the caller is not authenticated (controller is `[Authorize]`).
   - **500 Internal Server Error** on unhandled failures during processing.

---

## Authentication and actor context

1. The operation requires an **authenticated** principal.
2. **User id** for the domain event actor is taken from JWT claims `sub` or `id`; if absent, the API falls back to `"anonymous"` (still satisfies non-empty validation when combined with display name).
3. **User name** is taken from claim `name` or `Identity.Name`; if absent, falls back to `"Anonymous User"`.
4. **Correlation id** comes from request correlation middleware (`HttpContext.Items["CorrelationId"]`); if missing, a new GUID (`N` format) is generated so tracing always has a value.

---

## Validation (application layer)

Before the handler runs, **FluentValidation** must pass:

1. `CustomerId` — not empty (“Customer ID is required”).
2. `CorrelationId` — not empty (“Correlation ID is required”).
3. `UserId` — not empty (“User ID is required”).
4. `UserName` — not empty (“User name is required”).

*(Note: `UserId` / `UserName` / `CorrelationId` are populated server-side from context; the HTTP client only supplies `customerId`.)*

---

## Domain behavior

1. A **new** order identifier is generated; it must not collide with normal GUID usage (same statistical guarantees as `Guid.NewGuid()`).
2. The aggregate is created via `Order.Create(...)`, which applies a single **`OrderCreated`** event whose payload includes:
   - `orderId`, `customerId`,
   - `correlationId`,
   - **actor** (user id and display name from command context).
3. Immediately after creation, order **status** is **Draft** (implicit from event application / aggregate rules).
4. No line items or totals exist until subsequent commands (`AddOrderItem`, etc.).

---

## Persistence and reliability

1. **Save** persists uncommitted domain events through `IOrderRepository.SaveAsync` (event store + transactional **outbox** per solution architecture).
2. On persistence or infrastructure failure, the handler returns **`Result.Failure`** with an error message; the API maps this to **400 Bad Request** via existing `HandleResult` behavior unless changed elsewhere.
3. Downstream **read models** are updated asynchronously; callers must not assume the new order appears instantly in the Query API.

---

## Observability

1. **Information** logs when creation starts (customer id + correlation id) and when it succeeds (order id + customer id).
2. **Error** logs with exception detail when creation fails.

---

## Automated verification (existing tests)

These behaviors are already covered or implied by tests and should stay green when refactoring:

- Integration: valid request → **200**, body contains non-empty `orderId` prefixed with `order-`.
- Integration: empty `customerId` → **400**.
- Contract: JSON schemas for request/response include `CustomerId` / `OrderId` as stable property names.

---

## Out of scope for this command

- Verifying that the customer exists in an external system (unless a future story adds that invariant).
- Creating line items, confirming, shipping, or cancelling (separate commands).
- Choosing HTTP **201 Created** vs **200 OK** (current implementation uses **200**; changing status is a breaking contract change unless versioned/coordinated).

---

## Implementation plan

This plan maps work to the acceptance criteria above. **Most of the write path is already implemented** (`CreateOrderCommand`, validator, `CreateOrderCommandHandler`, `OrdersController`, `Order.Create` / `OrderCreated`, PostgreSQL event store + outbox wiring). Use the phases below to **verify**, **close gaps**, and **harden** before treating the feature as production-complete.

### Current baseline (reference)

| Area | Location (indicative) |
|------|------------------------|
| HTTP API | `EventSourcing.Command.Api` — `OrdersController.CreateOrder` |
| Command + validation | `CreateOrderCommand`, `CreateOrderCommandValidator` |
| Handler | `CreateOrderCommandHandler` |
| Domain | `Order.Create`, `OrderCreated`, `CustomerId` / `OrderId` |
| Correlation | Command API `Program.cs` middleware — `X-Correlation-ID` → `HttpContext.Items["CorrelationId"]` |
| Read projection | `OrderProjectionHandler.HandleOrderCreatedAsync` → `OrderSummaryReadModel` |

---

### Phase 1 — Prove the command path (short)

**Goal:** Every acceptance row under **API contract**, **Authentication**, **Validation**, **Domain behavior**, and **Observability** is demonstrably true in a running environment.

1. **Run automated tests**
   - Execute integration tests for `OrdersController` (`CreateOrder_*`).
   - Execute contract tests for `CreateOrderRequest` / `CreateOrderResponse` JSON shape.
2. **Manual or scripted smoke**
   - Authenticated `POST /api/v1/orders` with valid `customerId` → **200** and `orderId` matching `order-[0-9a-f]{32}`.
   - Same request with header `X-Correlation-ID: <value>` → confirm the same value appears on the response (per middleware) and flows into logs / persisted event metadata as expected.
3. **Inspect persisted facts**
   - After a successful create, confirm **one** new event stream for the order with event type **`OrderCreated`** and payload fields matching **Domain behavior** (customer id, correlation id, actor).

**Exit criteria:** Tests green; one documented smoke run; at least one DB-level confirmation of `OrderCreated` append.

---

### Phase 2 — Close documented gaps vs acceptance

**Goal:** Remove ambiguity between “spec” and “code,” especially around validation and HTTP semantics.

1. **`customerId` normalization**
   - Acceptance calls for non-empty **after trim**. Today validation uses `NotEmpty()` without trimming.
   - **Plan:** Add `.Must(id => !string.IsNullOrWhiteSpace(id?.Trim()))` or transform `CustomerId` in the validator/command mapping so whitespace-only input fails validation consistently (and optionally persist trimmed value).
2. **Unauthorized behavior**
   - Add an integration test that calls `POST /api/v1/orders` **without** authentication and expects **401** (mirrors **API contract**).
3. **Failure classification (optional product decision)**
   - Today, handler catch-all failures return `Result.Failure` → **400** via `HandleResult`. True infrastructure faults may warrant **503**/**500** with a stable error code.
   - **Plan:** Decide whether persistence failures should remain **400** or map to **5xx**; update handler + acceptance doc together if you change behavior.
4. **Projection correctness for `OrderCreated`**
   - `OrderProjectionHandler` routes on `domainEvent.GetType().Name == "OrderCreated"` and uses `ExtractProperty` for payload fields.
   - **Plan:** Confirm deserialization from the message/event envelope populates **CustomerId** reliably (integration test or worker-level test); replace stringly-typed routing with **`OrderCreated`** concrete type registration when the pipeline supports it (see `HandledEventTypes` placeholder).

**Exit criteria:** Trim/whitespace behavior documented and tested; 401 covered; projection path for `OrderCreated` verified end-to-end at least once (write → outbox → bus → worker → Cosmos/read store).

---

### Phase 3 — End-to-end “feature done” checklist

**Goal:** Operators and downstream teams can rely on **Persistence and reliability** and eventual read-side visibility.

1. **Transactional guarantee**
   - Confirm `SaveAsync` commits **event append + outbox row** in one transaction (align with `architecture.md`).
2. **Outbox publisher**
   - Deploy/run outbox publisher against the same DB; verify `OrderCreated` reaches the configured topic with **CorrelationId** (and other required application properties).
3. **Projection worker**
   - Consume message; confirm **idempotent** upsert of `OrderSummaryReadModel` for the new `orderId`.
4. **Query API**
   - After projection lag, **GET** (or list) returns the new order summary with **Draft** status and zero items/total — or document maximum acceptable lag for your SLA.

**Exit criteria:** Documented happy-path trace: Command API → Postgres → Outbox → Service Bus → Projection → Query/read model; rollback/retry behavior understood.

#### Phase 3 — Implementation / verification notes (this repo)

| Step | What was done |
|------|----------------|
| **1. Single transaction** | `EventSourcedRepository.SaveAsync` wraps append + outbox in one EF Core transaction. Because PostgreSQL is configured with `EnableRetryOnFailure`, the transaction runs inside `Database.CreateExecutionStrategy().ExecuteAsync(...)` so retries remain valid (see `EventSourcedRepository.cs`). On failure before commit, neither `event_store` rows nor `outbox_messages` rows persist. |
| **2. Outbox → Service Bus** | Outbox rows carry headers including **CorrelationId** (see `CreateOutboxMessage` in `EventSourcedRepository`). `ServiceBusMessagePublisher` maps headers to **application properties**. Operators should run `EventSourcing.OutboxPublisher` against the same Postgres instance and confirm the topic receives JSON payload + properties. |
| **3. Projection** | `OrderProjectionHandler` upserts `OrderSummaryReadModel` by stream id (Cosmos `UpsertAsync`) — replays are **idempotent** for the same event payload. Worker deserializes using `EventSourcing.Command.Domain.Orders.{EventType}` (assembly scan, same idea as the command-side event reload). |
| **4. Query API** | Eventual consistency: max lag = outbox poll interval + Service Bus + projection processing + Cosmos RU (not hard-coded in code). Validate with: create order → wait for projection → `GET` order summary shows **Draft**, **TotalAmount** 0, **ItemCount** 0. |

**Rollback / retry:** If `SaveAsync` throws, the aggregate’s uncommitted events are **not** marked committed. Outbox publisher retries failed publishes per `OutboxPublisherService`; projection worker uses Polly retries then dead-letters after max attempts.

---

### Phase 4 — Optional improvements (non-blocking)

1. **HTTP 201 Created** — only if API consumers agree; include `Location` header if adopted.
2. **Strong typing in projections** — register real `OrderCreated` in `HandledEventTypes` and deserialize to typed events.
3. **Customer existence** — only if product adds **Out of scope** as a new invariant (external service or read-model lookup).

#### Phase 4 — Status in this repo

| Item | Status |
|------|--------|
| **201 + Location** | Not implemented (remains **200 OK** per current contract; changing status is a breaking change unless versioned). |
| **Strong typing** | Implemented: `OrderProjectionHandler.HandledEventTypes` lists `OrderCreated`, `OrderItemAdded`, `OrderConfirmed`, `OrderShipped`, `OrderCancelled`; handlers use typed `switch` on domain events. `ProjectionWorkerService` resolves CLR types via the same assembly-qualified name pattern as the command-side event store. |
| **Customer existence** | Still out of scope (no new invariant). |

---

### Suggested sequencing

```text
Phase 1 (verify) → Phase 2 (gaps: trim, 401, projection typing/verification)
                 → Phase 3 (full pipeline checklist)
                 → Phase 4 (optional)
```

Dependencies: Phase 3 requires infrastructure (Postgres + broker + worker + read DB) available in the target environment; Phase 1–2 can proceed mostly with tests and local/docker compose.
