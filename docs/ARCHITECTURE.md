# Architecture Notes

## Domain boundaries

The application is split into four backend projects:

- **Billing.Domain** contains entities and enums only.
- **Billing.Application** contains DTOs, interfaces, use cases and billing calculations.
- **Billing.Infrastructure** implements persistence, Redis, Elasticsearch and the payment provider.
- **Billing.Api** exposes HTTP endpoints, auth, Swagger and the retry worker.

This keeps business rules testable and prevents controllers from becoming the domain layer.

## Data consistency

SQL Server is authoritative. Important invariants are backed by database constraints/indexes:

- `User.Email` is unique.
- `UsageEvent.ExternalEventId` is unique.
- `Payment.IdempotencyKey` is unique.
- `IdempotencyRecord.Key + Operation` is unique.
- An invoice is unique per organization and billing period.

Payments and invoice state transitions use EF Core transactions when multiple rows must change atomically.

## Usage flow

1. Authenticated customer posts a usage event.
2. API resolves the caller's organization.
3. SQL insert is attempted.
4. Duplicate `ExternalEventId` returns `duplicate=true` without incrementing usage again.
5. Redis counter is incremented only after the SQL insert succeeds.
6. Dashboard reads Redis first. If missing, it aggregates SQL and repopulates Redis.

## Billing flow

1. Load active subscription and plan.
2. Aggregate usage for the billing period.
3. Calculate overage: `max(usage - included, 0)`.
4. Create base-price invoice item.
5. Create overage item when needed.
6. Persist invoice and items.
7. Audit the generation action.

## Upgrade proration

For an upgrade:

```text
remainingRatio = remainingSeconds / periodSeconds
creditOldPlan  = oldMonthlyPrice * remainingRatio
costNewPlan    = newMonthlyPrice * remainingRatio
adjustment     = max(costNewPlan - creditOldPlan, 0)
```

The subscription changes immediately. If `adjustment > 0`, a proration invoice is generated.

Downgrades are scheduled for the next cycle. A background worker finalizes due periods, applies scheduled plan changes and advances the subscription dates.

## Payment idempotency

The caller sends an `Idempotency-Key`.

Before charging, the service queries `IdempotencyRecords` using `(key, operation)`. If present, the saved response is returned. Otherwise a payment row is created, the provider is called, the attempt is recorded, invoice status is updated, and the serialized response is saved under that key.

A unique index makes simultaneous duplicate requests converge at the database level as well.

## Retry model

A hosted background service wakes periodically and fetches failed payments with fewer than three attempts. It retries through the same provider abstraction and records another `PaymentAttempt`.

The demo backoff is intentionally short so the behavior is easy to observe. Production systems should use durable queues/jobs and explicit retry schedules.

## Redis

Key format:

```text
usage:{organizationId}:{yyyy-MM}
```

Redis is only a performance layer. Losing Redis does not lose billing data.

## Elasticsearch

Audit events are written to SQL first. Indexing to Elasticsearch is best-effort and is deliberately allowed to fail without failing the underlying business transaction. This avoids making billing correctness dependent on the search index.
