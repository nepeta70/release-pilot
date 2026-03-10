# ReleasePilot

ReleasePilot is an internal platform that manages application version promotions through fixed environments (`Dev → Staging → Production`) using DDD + CQRS + async domain events (Kafka).

## Prerequisites

- .NET SDK (this repo targets `net10.0`)
- Docker Desktop

## Start infrastructure

From the repo root:

```bash
docker compose up -d
```

This starts:

- PostgreSQL on `localhost:5432` (db `release_pilot`, user/pass `postgres`)
- Kafka on `localhost:9092`

## Run the API

```bash
dotnet run --project src/ReleasePilot.Api
```

The API runs on the URLs shown in `src/ReleasePilot.Api/Properties/launchSettings.json`.

## Notes on async events

- Every state transition raises a domain event.
- Events are written to `outbox_events` in the same DB transaction as the promotion update.
- A background outbox publisher publishes to Kafka topic `promotion-events`.
- A decoupled Kafka consumer persists an audit row into `promotion_event_audit`.

## Example requests

### RequestPromotion

```bash
curl -X POST http://localhost:5299/promotions ^
  -H "Content-Type: application/json" ^
  -d "{ \"appName\": \"payments\", \"version\": \"1.2.3\", \"sourceEnv\": \"Dev\", \"targetEnv\": \"Staging\", \"workItemIds\": [\"WI-1\",\"BUG-22\"], \"requestedBy\": \"alice\" }"
```

### ApprovePromotion

```bash
curl -X POST http://localhost:5299/promotions/{PROMOTION_ID}/approve ^
  -H "Content-Type: application/json" ^
  -d "{ \"userRole\": \"Approver\", \"userName\": \"bob\" }"
```

### StartDeployment

```bash
curl -X POST http://localhost:5299/promotions/{PROMOTION_ID}/start-deployment ^
  -H "Content-Type: application/json" ^
  -d "{ \"userName\": \"deploy-bot\" }"
```

### CompletePromotion

```bash
curl -X POST http://localhost:5299/promotions/{PROMOTION_ID}/complete ^
  -H "Content-Type: application/json" ^
  -d "{ \"userName\": \"deploy-bot\" }"
```

### RollbackPromotion

```bash
curl -X POST http://localhost:5299/promotions/{PROMOTION_ID}/rollback ^
  -H "Content-Type: application/json" ^
  -d "{ \"reason\": \"Health checks failing\", \"userName\": \"oncall\" }"
```

### CancelPromotion

```bash
curl -X POST http://localhost:5299/promotions/{PROMOTION_ID}/cancel ^
  -H "Content-Type: application/json" ^
  -d "{ \"userName\": \"alice\" }"
```

### Queries

```bash
curl http://localhost:5299/promotions/{PROMOTION_ID}
curl http://localhost:5299/applications/payments/environments/status
curl "http://localhost:5299/applications/payments/promotions?page=1&pageSize=10"
```

## What I would do next

- Add aggregate unit tests (state machine + invariants) and contract tests for API error mapping.
- Expand the read model to include work item details via `IIssueTrackerPort` (still stubbed).
- Implement a real Kafka publisher (schema/versioning, retries) and make the consumer deployable as a separate worker process.

### Swagger
http://localhost:5000/openapi/v1.json