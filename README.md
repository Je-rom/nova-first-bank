# NovaWallet Ledger Service

A concurrency-safe wallet ledger for FirstBank NovaPay's NovaWallet module, built in ASP.NET Core (.NET 8/9) + PostgreSQL.

## Running it

```bash
docker compose up
```

This starts the API and Postgres together. On first run, migrations apply automatically

- Swagger/OpenAPI spec: `http://localhost:<port>/openapi/v1.json` (Development environment only)

### Getting a token to call the API

There's no real auth server — per the brief, a mock issuer is expected. Mint a short-lived test JWT:

```bash
curl -X POST "http://localhost:<port>/dev/token?customerId=alice"
```

Use the returned token as a Bearer token on every other endpoint.

### Example flow

```bash
# Create a wallet
curl -X POST http://localhost:<port>/wallets \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"customerId":"alice"}'

# Credit it (simulates an inbound NIP transfer)
curl -X POST http://localhost:<port>/wallets/<walletId>/credit \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -d '{"amountKobo": 10000000}'

# Transfer to another wallet
curl -X POST http://localhost:<port>/transfers \
  -H "Authorization: Bearer <token>" -H "Content-Type: application/json" \
  -H "Idempotency-Key: any-unique-string" \
  -d '{"fromWalletId":"<id>","toWalletId":"<id>","amountKobo": 500000}'
```

### Running tests

```bash
docker compose up -d          # tests run against the live API
dotnet test
```

The integration tests in `NovaWallet.Tests` hit the running instance over HTTP (base URL configurable via `NOVAWALLET_BASE_URL`, defaults to `http://localhost:8080`).

## Architecture

```
Controllers/   → HTTP endpoints, [Authorize], model validation
Services/      → business logic: locking, idempotency, limit checks, orchestration
Repositories/  → data access, including the row-locking SQL
Models/        → plain POCOs mapped by EF Core
Data/          → DbContext + fluent configuration (indexes, constraints, enum storage)
Dtos/          → request/response shapes, kept separate from persistence models
Exceptions/    → domain exceptions, each carrying its HTTP status + RFC 7807 problem type
Middleware/    → global exception handler mapping exceptions to ProblemDetails
Constants/     → policy values (daily limit, timezone)
```

Standard layered structure — controllers depend on service interfaces, services depend on repository interfaces, nothing skips a layer. EF Core config lives entirely in `Data/NovaWalletDbContext.cs` via `OnModelCreating`; the models themselves stay plain.

## Key decisions & trade-offs

**Money as `long` kobo, everywhere.** No `decimal`/`double` anywhere in a field or calculation that touches an amount. This is the single most important invariant in the whole service — every other bug is recoverable, a floating-point rounding error in a ledger is not.

**Pessimistic row locking (`SELECT ... FOR UPDATE`) over optimistic concurrency.** For a transfer, both wallets are locked, always in ascending `Id` order regardless of transfer direction — this is what prevents deadlocks between a concurrent `A→B` and `B→A`. I chose this over optimistic concurrency (a `RowVersion` column + retry-on-conflict) because it's easier to reason about and demonstrate correctness for under a live walkthrough, and because retry storms under high contention on a "hot" wallet (e.g. a popular merchant) are a worse failure mode for a ledger than a bit of lock waiting. Trade-off: lower throughput under low contention than optimistic concurrency would give.

**Idempotency via a unique DB constraint, not application-level locking.** The `Transfer.IdempotencyKey` column has a unique index. Two concurrent requests with the same key both attempt an `INSERT`; the database itself guarantees only one wins. The loser reads back the winner's row and compares `RequestHash` to distinguish a safe replay (same payload) from a genuine key collision (different payload → 409).

**The pending `Transfer` row is saved independently, before wallet locking begins.** This keeps duplicate-request rejection cheap (no need to lock two wallets just to reject a replay) and gives every transfer attempt a durable record even if the money-moving step later fails or the process crashes.

**Known gap:** if the process crashes between creating the pending `Transfer` and completing the money-moving transaction, that row is stuck at `Pending` with no reconciliation job to resolve it. In production this needs a background job that either completes or fails stale `Pending` transfers past some timeout. Out of scope for this take-home; flagging it here rather than pretending it doesn't exist.

**Daily limit computed live, not via a stored counter.** `SUM(amount) FROM WalletTransactions WHERE ... AND CreatedAt >= midnight_WAT` is computed inside the same locked transaction as the transfer itself, rather than maintaining a separately-updated counter that would need its own midnight-reset job (and its own race conditions). Correct by construction, at the cost of a slightly more expensive query per transfer — acceptable at this scale.

**Enums stored as strings in Postgres**, not `int`, for readability when inspecting the database directly. Minor storage cost, worth it for debuggability.

**JWT with a mock/symmetric issuer**, as explicitly permitted by the brief. A `/dev/token` endpoint (Development environment only) mints tokens for testing — this is not production auth and isn't meant to be.

## What I'd add with more time

- Rate limiting on `POST /transfers`
- Outbox pattern + `TransferCompleted` event
- Structured logging with correlation IDs across a request
- Health/readiness endpoints
- A background reconciliation job for stuck `Pending` transfers (see gap above)
- DB-level enforcement that the audit log table is truly append-only (revoke UPDATE/DELETE grants, or a trigger), rather than relying on the codebase never calling them
