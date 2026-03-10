DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'promotion_status') THEN
        CREATE TYPE promotion_status AS ENUM (
            'Requested', 'Approved', 'InProgress', 'Completed', 'RolledBack', 'Cancelled');
    END IF;
END
$$;

-- Create the table only if it's missing
CREATE TABLE IF NOT EXISTS deployment_environments (
    name VARCHAR(20) PRIMARY KEY,
    sort_order INT NOT NULL
);

-- Insert rows only if they don't already exist
-- If the name (Primary Key) exists, we do nothing
INSERT INTO deployment_environments (name, sort_order)
VALUES 
    ('Dev', 1),
    ('Staging', 2),
    ('Production', 3)
ON CONFLICT (name) DO NOTHING;