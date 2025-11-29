-- Admin Database - User Table
-- Stores system user accounts for authentication and authorization
-- Users can be associated with multiple tenants for accessing tenant contexts

-- Create user table in fabrica schema
CREATE TABLE IF NOT EXISTS fabrica.user (
    user_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL UNIQUE,
    stytch_user_id VARCHAR(255) UNIQUE,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    display_name VARCHAR(200),
    avatar_media_id UUID,  -- References content domain media table (cross-domain, no FK)
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_system_admin BOOLEAN NOT NULL DEFAULT false,
    last_login_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID
);

-- Create index on email for faster lookups
CREATE INDEX IF NOT EXISTS idx_user_email ON fabrica.user(email);

-- Create index on stytch_user_id for OAuth lookups
CREATE INDEX IF NOT EXISTS idx_user_stytch_id ON fabrica.user(stytch_user_id);

-- Create index on is_active for filtering active users
CREATE INDEX IF NOT EXISTS idx_user_is_active ON fabrica.user(is_active);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION fabrica.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger to automatically update updated_at
CREATE TRIGGER update_user_updated_at
    BEFORE UPDATE ON fabrica.user
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- Create user_tenant junction table for multi-tenant access
-- This will relate users to tenants they have access to
CREATE TABLE IF NOT EXISTS fabrica.user_tenant (
    user_tenant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES fabrica.user(user_id) ON DELETE CASCADE,
    tenant_id UUID NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'viewer',
    is_active BOOLEAN NOT NULL DEFAULT true,
    granted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    granted_by UUID,
    revoked_at TIMESTAMP WITH TIME ZONE,
    revoked_by UUID,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_user_tenant UNIQUE(user_id, tenant_id)
);

-- Create indexes for user_tenant
CREATE INDEX IF NOT EXISTS idx_user_tenant_user_id ON fabrica.user_tenant(user_id);
CREATE INDEX IF NOT EXISTS idx_user_tenant_tenant_id ON fabrica.user_tenant(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_tenant_is_active ON fabrica.user_tenant(is_active);

-- Create trigger for user_tenant updated_at
CREATE TRIGGER update_user_tenant_updated_at
    BEFORE UPDATE ON fabrica.user_tenant
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- Insert seed data for development ONLY if no users exist
-- This prevents re-seeding if users have been intentionally deleted
INSERT INTO fabrica.user (email, first_name, last_name, display_name, is_active, is_system_admin, stytch_user_id)
SELECT * FROM (VALUES
    ('admin@fabrica.dev', 'System', 'Administrator', 'System Admin', true, true, NULL::VARCHAR),
    ('john.doe@example.com', 'John', 'Doe', 'John Doe', true, false, NULL::VARCHAR),
    ('jane.smith@example.com', 'Jane', 'Smith', 'Jane Smith', true, false, NULL::VARCHAR),
    ('bob.wilson@example.com', 'Bob', 'Wilson', 'Bob Wilson', true, false, NULL::VARCHAR),
    ('alice.brown@example.com', 'Alice', 'Brown', 'Alice Brown', false, false, NULL::VARCHAR)
) AS seed_data(email, first_name, last_name, display_name, is_active, is_system_admin, stytch_user_id)
WHERE NOT EXISTS (SELECT 1 FROM fabrica.user LIMIT 1);

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.user TO fabrica_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.user_tenant TO fabrica_admin;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA fabrica TO fabrica_admin;

-- OUTBOX NOTIFY TRIGGER
-- Notifies the outbox_events channel when new outbox entries are created
CREATE OR REPLACE FUNCTION cdc.notify_outbox_insert()
RETURNS TRIGGER AS $$
BEGIN
    PERFORM pg_notify('outbox_events', NEW.id::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER outbox_notify_trigger
    AFTER INSERT ON cdc.outbox
    FOR EACH ROW EXECUTE FUNCTION cdc.notify_outbox_insert();
