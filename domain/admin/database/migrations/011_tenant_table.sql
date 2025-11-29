-- Migration: Create tenant table and update user_tenant FK
-- Multi-tenancy support for Fabrica Commerce Cloud

-- Create tenant table
CREATE TABLE IF NOT EXISTS fabrica.tenant (
    tenant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    logo_media_id UUID,  -- References content domain media (cross-domain, no FK)
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_personal BOOLEAN NOT NULL DEFAULT false,  -- Auto-created personal tenants
    owner_user_id UUID REFERENCES fabrica.user(user_id) ON DELETE SET NULL,
    settings JSONB DEFAULT '{}',  -- Flexible tenant settings
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_tenant_slug ON fabrica.tenant(slug);
CREATE INDEX IF NOT EXISTS idx_tenant_owner ON fabrica.tenant(owner_user_id);
CREATE INDEX IF NOT EXISTS idx_tenant_is_active ON fabrica.tenant(is_active);
CREATE INDEX IF NOT EXISTS idx_tenant_is_personal ON fabrica.tenant(is_personal);

-- Auto-update trigger for updated_at
CREATE TRIGGER update_tenant_updated_at
    BEFORE UPDATE ON fabrica.tenant
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- Seed system tenant
INSERT INTO fabrica.tenant (tenant_id, name, slug, description, is_active, is_personal)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'System',
    'system',
    'System-level tenant for platform administration',
    true,
    false
) ON CONFLICT (tenant_id) DO NOTHING;

-- Create personal tenants for existing users who don't have any tenant access
-- This handles existing users before multi-tenancy was implemented
DO $$
DECLARE
    r RECORD;
    new_tenant_id UUID;
    slug_base VARCHAR(100);
    slug_final VARCHAR(100);
    counter INTEGER := 0;
BEGIN
    FOR r IN
        SELECT u.user_id, u.email, u.display_name, u.first_name
        FROM fabrica.user u
        WHERE NOT EXISTS (
            SELECT 1 FROM fabrica.user_tenant ut WHERE ut.user_id = u.user_id
        )
    LOOP
        -- Generate slug from email (take part before @)
        slug_base := lower(regexp_replace(split_part(r.email, '@', 1), '[^a-z0-9]', '-', 'g'));
        slug_final := slug_base;
        counter := 0;

        -- Ensure unique slug
        WHILE EXISTS (SELECT 1 FROM fabrica.tenant WHERE slug = slug_final) LOOP
            counter := counter + 1;
            slug_final := slug_base || '-' || counter;
        END LOOP;

        -- Create personal tenant
        new_tenant_id := gen_random_uuid();
        INSERT INTO fabrica.tenant (tenant_id, name, slug, is_personal, owner_user_id, created_by)
        VALUES (
            new_tenant_id,
            COALESCE(r.display_name, r.first_name, split_part(r.email, '@', 1)) || '''s Workspace',
            slug_final,
            true,
            r.user_id,
            r.user_id
        );

        -- Link user to their personal tenant
        INSERT INTO fabrica.user_tenant (user_id, tenant_id, role, is_active, granted_by)
        VALUES (r.user_id, new_tenant_id, 'admin', true, r.user_id);
    END LOOP;
END $$;

-- Now add foreign key constraint to user_tenant
-- First, ensure all existing tenant_ids exist in tenant table
-- (The DO block above should have created tenants for all users)
ALTER TABLE fabrica.user_tenant
    ADD CONSTRAINT fk_user_tenant_tenant
    FOREIGN KEY (tenant_id) REFERENCES fabrica.tenant(tenant_id) ON DELETE CASCADE;

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.tenant TO fabrica_admin;
