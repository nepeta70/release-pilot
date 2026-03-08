CREATE TABLE IF NOT EXISTS promotions (
    id UUID PRIMARY KEY,
    application_name VARCHAR(100) NOT NULL,
    version VARCHAR(50) NOT NULL,
    target_env deployment_environment NOT NULL,
    current_status promotion_status NOT NULL DEFAULT 'Requested',
    work_items JSONB NOT NULL DEFAULT '[]', -- References to external issue tracker
    metadata JSONB, -- For extensibility (e.g. rollback reasons)
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- The "Only one InProgress per App+Env" constraint (Functional Index)
CREATE UNIQUE INDEX IF NOT EXISTS uidx_promotion_lock 
ON promotions (application_name, target_env) 
WHERE (current_status = 'InProgress');

-- Audit Log Table (Required by Section 2.5)
CREATE TABLE IF NOT EXISTS promotion_audit_logs (
    id BIGSERIAL PRIMARY KEY,
    promotion_id UUID NOT NULL REFERENCES promotions(id),
    from_status promotion_status,
    to_status promotion_status NOT NULL,
    acting_user VARCHAR(100),
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    context JSONB -- Stores the event payload
);

-- 1. Create the function that handles the audit insertion
CREATE OR REPLACE FUNCTION fn_audit_promotion_status_change()
RETURNS TRIGGER AS $$
BEGIN
    -- Only insert if the status has actually changed
    IF (TG_OP = 'INSERT') OR (OLD.current_status IS DISTINCT FROM NEW.current_status) THEN
        INSERT INTO promotion_audit_logs (
            promotion_id, 
            from_status, 
            to_status, 
            occurred_at,
            context
        )
        VALUES (
            NEW.id,
            CASE WHEN TG_OP = 'INSERT' THEN NULL ELSE OLD.current_status END,
            NEW.current_status,
            NOW(),
            jsonb_build_object(
                'version', NEW.version,
                'op', TG_OP,
                'metadata', NEW.metadata
            )
        );
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 2. Attach the trigger to the promotions table
DROP TRIGGER IF EXISTS trg_audit_promotion_status ON promotions;
CREATE TRIGGER trg_audit_promotion_status
AFTER INSERT OR UPDATE ON promotions
FOR EACH ROW
EXECUTE FUNCTION fn_audit_promotion_status_change();