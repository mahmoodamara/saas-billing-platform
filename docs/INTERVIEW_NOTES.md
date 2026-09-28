# Interview Notes

Use these points to explain the project clearly.

## Why SQL Server?

Billing data has strong relationships and correctness requirements. SQL Server provides transactions, unique constraints, foreign keys and predictable relational queries. I used it as the source of truth.

## Why Redis?

Usage dashboards are read frequently. Recomputing `SUM(quantity)` over many usage rows on every request is wasteful. Redis keeps a fast billing-period counter. SQL remains authoritative, and the cache can be rebuilt.

## How do you avoid counting a message twice?

Every usage message has an external event ID. That column has a unique index. A repeated event is treated as a duplicate and does not increment Redis again.

## How do you avoid charging twice?

The payment endpoint requires an idempotency key. The server stores the first result. Replaying the same request key returns that result rather than calling the payment provider again. Database uniqueness protects against concurrent duplicates.

## Why is the payment provider behind an interface?

The billing domain should not depend directly on Stripe or another vendor. `IPaymentProvider` lets the application use a mock provider now and add Stripe later without rewriting billing logic.

## What happens if payment fails?

The payment and attempt are persisted with `Failed` status. A background worker retries eligible payments and adds a new attempt record. The invoice stays `PastDue` until payment succeeds.

## How is an upgrade handled?

Upgrades are immediate. The application calculates the remaining fraction of the current billing period and creates a proration adjustment based on the price difference. Downgrades are scheduled for the next period.

## What would change in production?

I would add real provider webhooks, an outbox/inbox pattern, a durable message queue, secrets management, rate limiting, EF migrations, OpenTelemetry, stronger integration tests and a resilient job processor.
