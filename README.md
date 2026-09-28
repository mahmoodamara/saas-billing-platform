# SaaS Billing & Subscription Platform

A portfolio-ready full-stack SaaS billing system built for realistic subscription and usage-based billing workflows.

## What this project demonstrates

- React + TypeScript customer/admin UI
- ASP.NET Core 8 Web API in C#
- SQL Server as the transactional source of truth
- Entity Framework Core for relational persistence
- Redis for fast usage counters
- Elasticsearch for searchable audit events
- JWT authentication and role-based authorization
- Subscription plans, upgrades, downgrades, cancellation
- Usage metering with duplicate-event protection
- Invoice generation with base price + overage billing
- Proration for mid-cycle upgrades
- Idempotent payment processing
- Failed-payment retries via a background worker
- Automatic billing-cycle advancement and scheduled downgrade application
- Refund validation and audit logging
- Docker Compose for the complete stack
- Unit tests for billing calculations

## Architecture

```text
React + TypeScript
       |
       | HTTPS / REST
       v
ASP.NET Core Web API
       |
       +-------------------+------------------+
       |                   |                  |
       v                   v                  v
 SQL Server              Redis          Elasticsearch
(source of truth)   (usage counters)   (audit search)
       |
       v
Background payment retry worker
       |
       v
Mock Payment Provider
```

## Demo accounts

After the first startup the API seeds:

- Admin: `admin@demo.local` / `Admin123!`
- Customer: `customer@demo.local` / `Customer123!`

The customer belongs to **Acme Messaging** and starts on the **Pro** plan with seeded usage so you can immediately generate an invoice.

> These credentials are intentionally demo-only. Change them before deploying publicly.

## Quick start with Docker

Requirements: Docker Desktop / Docker Engine with Compose.

```bash
docker compose up --build
```

Then open:

- Web app: http://localhost:3000
- Swagger: http://localhost:8080/swagger
- API health: http://localhost:8080/health
- Elasticsearch: http://localhost:9200

SQL Server, Redis, Elasticsearch, the API and the React frontend are all started by Compose.

## Run locally without Docker

### Backend

Requirements: .NET 8 SDK, SQL Server, Redis, Elasticsearch.

```bash
cd backend
cp ../.env.example .env   # reference only; set equivalent environment variables in your shell
cd Billing.Api
dotnet restore
dotnet run
```

The default development configuration expects local SQL Server, Redis on `localhost:6379`, and Elasticsearch on `http://localhost:9200`.

### Frontend

Requirements: Node.js 20+.

```bash
cd frontend
npm install
npm run dev
```

Vite proxies `/api` requests to `http://localhost:8080`.

## Smoke test

After `docker compose up --build` is healthy, run:

```bash
./scripts/smoke-test.sh
```

The script logs in, reads plans/usage, records a usage event, generates an invoice, pays it twice with the same idempotency key, and verifies that the same payment is returned.

## Main workflows

### 1. Plans and subscriptions

A customer can subscribe to a plan, upgrade immediately, schedule a downgrade for the next billing period, or cancel.

For an upgrade, the backend computes the prorated price difference using the remaining fraction of the current billing period. If the new plan is more expensive, a proration invoice is created.

### 2. Usage metering

Usage events are posted to:

```http
POST /api/usage/events
```

Example:

```json
{
  "externalEventId": "evt_932234",
  "channel": "WhatsApp",
  "eventType": "Message",
  "quantity": 1,
  "timestamp": "2026-09-28T10:32:00Z"
}
```

`ExternalEventId` has a unique SQL index, so the same event cannot be counted twice. Redis stores the current billing-period usage counter for fast dashboard reads, while SQL Server remains the source of truth.

### 3. Invoice generation

For a Pro plan:

```text
Monthly price       $99
Included messages   10,000
Actual usage        12,500
Overage              2,500
Overage price        $0.01/message
Overage cost         $25
Invoice total       $124
```

The API creates separate invoice items for the base subscription and overage usage.

### 4. Idempotent payments

Payments require an `Idempotency-Key` header.

```http
POST /api/invoices/{invoiceId}/pay
Idempotency-Key: 821dd79c-...
```

If the same key is replayed, the API returns the original payment result instead of charging again.

### 5. Failed payment retries

The demo payment endpoint accepts `simulateFailure: true`. Failed payments are marked for retry. A background worker periodically retries eligible failed payments and records every attempt.

### 6. Refunds

Admins can refund all or part of a successful payment. The API rejects refunds greater than the remaining refundable amount.

## API overview

```text
POST   /api/auth/login

GET    /api/plans
POST   /api/plans                         Admin
PUT    /api/plans/{id}                    Admin

GET    /api/subscriptions/current
POST   /api/subscriptions
POST   /api/subscriptions/{id}/upgrade
POST   /api/subscriptions/{id}/downgrade
POST   /api/subscriptions/{id}/cancel

POST   /api/usage/events
GET    /api/usage/current

GET    /api/invoices
GET    /api/invoices/{id}
POST   /api/invoices/generate-current
POST   /api/invoices/{id}/pay

GET    /api/payments
POST   /api/payments/{id}/refund          Admin
POST   /api/payments/{id}/retry           Admin

GET    /api/dashboard
GET    /api/audit/search?q=...            Admin
GET    /api/admin/payments                Admin
POST   /api/admin/payments/{id}/retry     Admin
POST   /api/admin/payments/{id}/refund    Admin
```

## Project structure

```text
backend/
  Billing.Api/             Controllers, middleware, DI, background worker
  Billing.Application/     DTOs, interfaces, business services, billing calculator
  Billing.Domain/          Entities and enums
  Billing.Infrastructure/  EF Core, SQL Server, Redis, Elasticsearch, payment provider
  Billing.Tests/           xUnit tests

frontend/
  src/
    components/
    pages/
    services/
    types/

docs/
  ARCHITECTURE.md
  INTERVIEW_NOTES.md
```

## Important engineering decisions

- **SQL Server is the source of truth** because billing needs relational consistency, constraints and transactions.
- **Redis is an optimization**, not authoritative storage. The API can rebuild a usage counter from SQL if Redis is empty.
- **Usage idempotency** is enforced with a unique external event ID.
- **Payment idempotency** uses an idempotency record persisted in SQL.
- **Sensitive state changes are audited** and optionally indexed into Elasticsearch.
- **Provider abstraction** allows the mock payment provider to be replaced by Stripe or another provider without changing billing services.
- **Background retries** are separated from request handling.

## Production improvements

This repository is intentionally portfolio-sized. For a production billing platform, add:

- EF Core migrations instead of `EnsureCreated`
- Secrets manager / managed identity
- Real payment provider webhooks with signature validation
- Distributed queue for usage events and retry jobs
- Outbox/inbox pattern for cross-service reliability
- OpenTelemetry traces and metrics
- Rate limiting and WAF rules
- More integration/end-to-end tests
- Stronger user onboarding/password reset flows

See `docs/ARCHITECTURE.md` and `docs/INTERVIEW_NOTES.md` for deeper details.
