CREATE TABLE IF NOT EXISTS promotion_event_audit (
    id BIGSERIAL PRIMARY KEY,
    promotion_id UUID NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL,
    acting_user VARCHAR(100) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_promotion_event_audit_promotion_id
    ON promotion_event_audit (promotion_id);

