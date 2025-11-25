-- ============================================================================
-- Migration: 003_cdc_outbox.sql
-- Description: Add CDC schema and outbox table for the outbox pattern
-- Date: 2024-11-24
-- ============================================================================

-- Ensure uuid-ossp extension is available
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create the CDC (Change Data Capture) schema
CREATE SCHEMA IF NOT EXISTS cdc;

-- ============================================================================
-- CDC SCHEMA: Change Data Capture / Outbox Pattern
-- ============================================================================

CREATE TABLE cdc.outbox (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- 'user', 'role', 'permission', etc.
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- 'user.created', 'user.updated', etc.
    event_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending' -- pending, processing, processed, failed
);

-- ============================================================================
-- INDEXES for Performance
-- ============================================================================

CREATE INDEX idx_outbox_tenant ON cdc.outbox(tenant_id);
CREATE INDEX idx_outbox_status ON cdc.outbox(status);
CREATE INDEX idx_outbox_created ON cdc.outbox(created_at);
CREATE INDEX idx_outbox_aggregate ON cdc.outbox(aggregate_type, aggregate_id);

-- ============================================================================
-- COMMENTS
-- ============================================================================

COMMENT ON TABLE cdc.outbox IS 'Outbox pattern table for reliable event publishing';
COMMENT ON COLUMN cdc.outbox.aggregate_type IS 'Type of the domain aggregate (user, role, permission, etc.)';
COMMENT ON COLUMN cdc.outbox.aggregate_id IS 'ID of the aggregate that was changed';
COMMENT ON COLUMN cdc.outbox.event_type IS 'Type of event (aggregate_type.action, e.g., user.created)';
COMMENT ON COLUMN cdc.outbox.event_data IS 'JSON representation of the entity state at the time of the event';
COMMENT ON COLUMN cdc.outbox.status IS 'Processing status: pending, processing, processed, failed';
