-- Authorization Tables for Fabrica Admin
-- Provides role-based access control (RBAC) and permission management

-- ============================================================================
-- Roles Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS fabrica.role (
    role_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name VARCHAR(100) NOT NULL UNIQUE,
    role_description TEXT,
    is_system_role BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID
);

-- Create index on role_name for faster lookups
CREATE INDEX IF NOT EXISTS idx_role_name ON fabrica.role(role_name);
CREATE INDEX IF NOT EXISTS idx_role_is_active ON fabrica.role(is_active);

-- Create trigger for role updated_at
CREATE TRIGGER update_role_updated_at
    BEFORE UPDATE ON fabrica.role
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- ============================================================================
-- Permissions Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS fabrica.permission (
    permission_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    permission_name VARCHAR(100) NOT NULL UNIQUE,
    permission_description TEXT,
    resource VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID,
    CONSTRAINT unique_resource_action UNIQUE(resource, action)
);

-- Create indexes for permissions
CREATE INDEX IF NOT EXISTS idx_permission_name ON fabrica.permission(permission_name);
CREATE INDEX IF NOT EXISTS idx_permission_resource ON fabrica.permission(resource);
CREATE INDEX IF NOT EXISTS idx_permission_is_active ON fabrica.permission(is_active);

-- Create trigger for permission updated_at
CREATE TRIGGER update_permission_updated_at
    BEFORE UPDATE ON fabrica.permission
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- ============================================================================
-- Role-Permission Junction Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS fabrica.role_permission (
    role_permission_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL REFERENCES fabrica.role(role_id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES fabrica.permission(permission_id) ON DELETE CASCADE,
    granted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    granted_by UUID,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_role_permission UNIQUE(role_id, permission_id)
);

-- Create indexes for role_permission
CREATE INDEX IF NOT EXISTS idx_role_permission_role_id ON fabrica.role_permission(role_id);
CREATE INDEX IF NOT EXISTS idx_role_permission_permission_id ON fabrica.role_permission(permission_id);

-- ============================================================================
-- User-Role Junction Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS fabrica.user_role (
    user_role_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES fabrica.user(user_id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES fabrica.role(role_id) ON DELETE CASCADE,
    tenant_id VARCHAR(100),
    is_active BOOLEAN NOT NULL DEFAULT true,
    granted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    granted_by UUID,
    revoked_at TIMESTAMP WITH TIME ZONE,
    revoked_by UUID,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT unique_user_role_tenant UNIQUE(user_id, role_id, tenant_id)
);

-- Create indexes for user_role
CREATE INDEX IF NOT EXISTS idx_user_role_user_id ON fabrica.user_role(user_id);
CREATE INDEX IF NOT EXISTS idx_user_role_role_id ON fabrica.user_role(role_id);
CREATE INDEX IF NOT EXISTS idx_user_role_tenant_id ON fabrica.user_role(tenant_id);
CREATE INDEX IF NOT EXISTS idx_user_role_is_active ON fabrica.user_role(is_active);

-- Create trigger for user_role updated_at
CREATE TRIGGER update_user_role_updated_at
    BEFORE UPDATE ON fabrica.user_role
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- ============================================================================
-- Seed Data - System Roles
-- ============================================================================
INSERT INTO fabrica.role (role_name, role_description, is_system_role, is_active)
VALUES
    ('System Admin', 'Full system access with all permissions', true, true),
    ('Tenant Admin', 'Full access within assigned tenants', true, true),
    ('Manager', 'Management access with read/write permissions', true, true),
    ('Editor', 'Edit access to assigned resources', true, true),
    ('Viewer', 'Read-only access to assigned resources', true, true)
ON CONFLICT (role_name) DO NOTHING;

-- ============================================================================
-- Seed Data - System Permissions
-- ============================================================================
INSERT INTO fabrica.permission (permission_name, permission_description, resource, action, is_active)
VALUES
    -- User Management
    ('users.read', 'View users', 'users', 'read', true),
    ('users.create', 'Create users', 'users', 'create', true),
    ('users.update', 'Update users', 'users', 'update', true),
    ('users.delete', 'Delete users', 'users', 'delete', true),

    -- Product Management
    ('products.read', 'View products', 'products', 'read', true),
    ('products.create', 'Create products', 'products', 'create', true),
    ('products.update', 'Update products', 'products', 'update', true),
    ('products.delete', 'Delete products', 'products', 'delete', true),

    -- Category Management
    ('categories.read', 'View categories', 'categories', 'read', true),
    ('categories.create', 'Create categories', 'categories', 'create', true),
    ('categories.update', 'Update categories', 'categories', 'update', true),
    ('categories.delete', 'Delete categories', 'categories', 'delete', true),

    -- Role Management
    ('roles.read', 'View roles', 'roles', 'read', true),
    ('roles.create', 'Create roles', 'roles', 'create', true),
    ('roles.update', 'Update roles', 'roles', 'update', true),
    ('roles.delete', 'Delete roles', 'roles', 'delete', true),

    -- Permission Management
    ('permissions.read', 'View permissions', 'permissions', 'read', true),
    ('permissions.assign', 'Assign permissions', 'permissions', 'assign', true),

    -- Tenant Management
    ('tenants.read', 'View tenants', 'tenants', 'read', true),
    ('tenants.create', 'Create tenants', 'tenants', 'create', true),
    ('tenants.update', 'Update tenants', 'tenants', 'update', true),
    ('tenants.delete', 'Delete tenants', 'tenants', 'delete', true)
ON CONFLICT (resource, action) DO NOTHING;

-- ============================================================================
-- Assign Permissions to System Admin Role
-- ============================================================================
INSERT INTO fabrica.role_permission (role_id, permission_id)
SELECT
    r.role_id,
    p.permission_id
FROM fabrica.role r
CROSS JOIN fabrica.permission p
WHERE r.role_name = 'System Admin'
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ============================================================================
-- Assign Permissions to Tenant Admin Role
-- ============================================================================
INSERT INTO fabrica.role_permission (role_id, permission_id)
SELECT
    r.role_id,
    p.permission_id
FROM fabrica.role r
CROSS JOIN fabrica.permission p
WHERE r.role_name = 'Tenant Admin'
AND p.resource NOT IN ('roles', 'permissions', 'tenants')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ============================================================================
-- Assign Permissions to Manager Role
-- ============================================================================
INSERT INTO fabrica.role_permission (role_id, permission_id)
SELECT
    r.role_id,
    p.permission_id
FROM fabrica.role r
CROSS JOIN fabrica.permission p
WHERE r.role_name = 'Manager'
AND p.resource IN ('users', 'products', 'categories')
AND p.action IN ('read', 'create', 'update')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ============================================================================
-- Assign Permissions to Editor Role
-- ============================================================================
INSERT INTO fabrica.role_permission (role_id, permission_id)
SELECT
    r.role_id,
    p.permission_id
FROM fabrica.role r
CROSS JOIN fabrica.permission p
WHERE r.role_name = 'Editor'
AND p.resource IN ('products', 'categories')
AND p.action IN ('read', 'create', 'update')
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ============================================================================
-- Assign Permissions to Viewer Role
-- ============================================================================
INSERT INTO fabrica.role_permission (role_id, permission_id)
SELECT
    r.role_id,
    p.permission_id
FROM fabrica.role r
CROSS JOIN fabrica.permission p
WHERE r.role_name = 'Viewer'
AND p.action = 'read'
ON CONFLICT (role_id, permission_id) DO NOTHING;

-- ============================================================================
-- Assign System Admin Role to Seed Users
-- ============================================================================
INSERT INTO fabrica.user_role (user_id, role_id, tenant_id, is_active)
SELECT
    u.user_id,
    r.role_id,
    NULL as tenant_id,
    true as is_active
FROM fabrica.user u
CROSS JOIN fabrica.role r
WHERE u.email = 'admin@fabrica.dev'
AND r.role_name = 'System Admin'
ON CONFLICT (user_id, role_id, tenant_id) DO NOTHING;

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.role TO fabrica_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.permission TO fabrica_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.role_permission TO fabrica_admin;
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.user_role TO fabrica_admin;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA fabrica TO fabrica_admin;
