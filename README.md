# ReleasePilot

ReleasePilot is an internal platform that manages application version promotions through fixed environments (`Dev → Staging → Production`) using DDD + CQRS + async domain events (Kafka).

## Prerequisites

- .NET SDK (this repo targets `net10.0`)
- Docker Desktop

## Start infrastructure

From the repo root:

```bash
docker compose -f docker/docker-compose.yaml up --build -d
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
curl --location --request PUT 'http://localhost:5000/promotions' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver' \
--header 'Content-Type: application/json' \
--data '{
  "appName": "ReleasePilot.Api",
  "version": "1.2.4-beta",
  "targetEnv": "Dev",
  "workItemIds": [
    "JIRA-101",
    "JIRA-102"
  ]
}'
```

### ApprovePromotion

```bash
curl --location --request POST 'http://localhost:5000/promotions/63f99f79-a4d4-4a77-8e67-a30763f5f309/approve' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### StartDeployment

```bash
curl --location --request POST 'http://localhost:5000/promotions/63f99f79-a4d4-4a77-8e67-a30763f5f309/start' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### CompletePromotion

```bash
curl --location --request POST 'http://localhost:5000/promotions/63f99f79-a4d4-4a77-8e67-a30763f5f309/complete' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### RollbackPromotion

```bash
curl --location 'http://localhost:5000/promotions/63f99f79-a4d4-4a77-8e67-a30763f5f309/rollback' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver' \
--header 'Content-Type: application/json' \
--data '{
    "reason": "test"
}'
```

### CancelPromotion

```bash
curl --location --request POST 'http://localhost:5000/promotions/63f99f79-a4d4-4a77-8e67-a30763f5f309/cancel' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### GetPromotionById

```bash
curl --location 'http://localhost:5000/promotions/63f99f79-a4d4-4a77-8e67-a30763f5f309' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### GetEnvironmentStatus

```bash
curl --location 'http://localhost:5000/promotions/ReleasePilot.Api/status' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### ListPromotionsByApplication

```bash
curl --location 'http://localhost:5000/promotions/ReleasePilot.Api/list/1/5' \
--header 'X-User-Name: pepe' \
--header 'X-Role: Approver'
```

### Swagger
http://localhost:5000/openapi/v1.json