-- Migration: Add component availability flags to domain table
-- Controls which deployment buttons are enabled for each domain

-- Add the component flags
ALTER TABLE fabrica.domain ADD COLUMN IF NOT EXISTS has_shell BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE fabrica.domain ADD COLUMN IF NOT EXISTS has_mfe BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE fabrica.domain ADD COLUMN IF NOT EXISTS has_bff BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE fabrica.domain ADD COLUMN IF NOT EXISTS has_acl BOOLEAN NOT NULL DEFAULT false;

-- Set values for existing domains
UPDATE fabrica.domain SET has_shell = true, has_mfe = true, has_bff = true, has_acl = true WHERE domain_name = 'admin';
UPDATE fabrica.domain SET has_shell = false, has_mfe = true, has_bff = true, has_acl = true WHERE domain_name = 'product';
UPDATE fabrica.domain SET has_shell = false, has_mfe = true, has_bff = true, has_acl = true WHERE domain_name = 'content';

-- Add comments
COMMENT ON COLUMN fabrica.domain.has_shell IS 'Whether this domain has a shell application';
COMMENT ON COLUMN fabrica.domain.has_mfe IS 'Whether this domain has a micro-frontend';
COMMENT ON COLUMN fabrica.domain.has_bff IS 'Whether this domain has a backend-for-frontend service';
COMMENT ON COLUMN fabrica.domain.has_acl IS 'Whether this domain has an anti-corruption layer service';
