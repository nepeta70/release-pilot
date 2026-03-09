DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'promotion_status') THEN
        CREATE TYPE promotion_status AS ENUM (
            'Requested', 'Approved', 'InProgress', 'Completed', 'RolledBack', 'Cancelled');
    END IF;
END
$$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'deployment_environment') THEN
        CREATE TYPE deployment_environment AS ENUM (
            'Dev', 'Staging', 'Production');
    END IF;
END
$$;