CREATE TYPE promotion_status AS ENUM (
    'Requested', 'Approved', 'InProgress', 'Completed', 'RolledBack', 'Cancelled'
);

CREATE TYPE deployment_environment AS ENUM (
    'Dev', 'Staging', 'Production'
);