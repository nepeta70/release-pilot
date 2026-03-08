CREATE TABLE IF NOT EXISTS outbox_events (
    id UUID PRIMARY KEY,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    occurred_on TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_on TIMESTAMPTZ
);