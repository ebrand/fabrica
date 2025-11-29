-- Migration: Create invitation table for tenant user invitations
-- Tenant invitation system for Fabrica Commerce Cloud

-- Create invitation table
CREATE TABLE IF NOT EXISTS fabrica.invitation (
    invitation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    tenant_id UUID NOT NULL REFERENCES fabrica.tenant(tenant_id) ON DELETE CASCADE,
    invited_by UUID NOT NULL REFERENCES fabrica.user(user_id),
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    accepted_at TIMESTAMP WITH TIME ZONE,
    accepted_by_user_id UUID REFERENCES fabrica.user(user_id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT chk_invitation_status CHECK (status IN ('pending', 'accepted', 'expired', 'revoked'))
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_invitation_email ON fabrica.invitation(email);
CREATE INDEX IF NOT EXISTS idx_invitation_tenant ON fabrica.invitation(tenant_id);
CREATE INDEX IF NOT EXISTS idx_invitation_status ON fabrica.invitation(status);
CREATE INDEX IF NOT EXISTS idx_invitation_expires ON fabrica.invitation(expires_at) WHERE status = 'pending';

-- Prevent duplicate pending invitations for same email+tenant
CREATE UNIQUE INDEX IF NOT EXISTS idx_invitation_unique_pending
    ON fabrica.invitation(email, tenant_id)
    WHERE status = 'pending';

-- Auto-update trigger for updated_at
CREATE TRIGGER update_invitation_updated_at
    BEFORE UPDATE ON fabrica.invitation
    FOR EACH ROW
    EXECUTE FUNCTION fabrica.update_updated_at_column();

-- Grant permissions
GRANT SELECT, INSERT, UPDATE, DELETE ON fabrica.invitation TO fabrica_admin;
