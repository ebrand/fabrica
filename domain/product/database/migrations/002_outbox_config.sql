-- Migration: Add outbox_config table for controlling CDC capture
-- Provides granular control over which tables and actions write to the outbox

-- Ensure uuid-ossp extension exists
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Ensure cdc schema exists
CREATE SCHEMA IF NOT EXISTS cdc;

-- Create the outbox_config table
CREATE TABLE IF NOT EXISTS cdc.outbox_config (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    schema_name VARCHAR(100) NOT NULL,
    table_name VARCHAR(100) NOT NULL,
    capture_insert BOOLEAN NOT NULL DEFAULT true,
    capture_update BOOLEAN NOT NULL DEFAULT true,
    capture_delete BOOLEAN NOT NULL DEFAULT true,
    is_active BOOLEAN NOT NULL DEFAULT true,
    description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_outbox_config_schema_table UNIQUE (schema_name, table_name)
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_outbox_config_active ON cdc.outbox_config (is_active);

-- Add comment
COMMENT ON TABLE cdc.outbox_config IS 'Configuration for CDC outbox pattern - controls which tables/actions are captured';

-- Insert default config for product table (captures all actions)
INSERT INTO cdc.outbox_config (schema_name, table_name, capture_insert, capture_update, capture_delete, is_active, description)
VALUES ('fabrica', 'product', true, true, true, true, 'Capture all product entity changes')
ON CONFLICT (schema_name, table_name) DO NOTHING;
