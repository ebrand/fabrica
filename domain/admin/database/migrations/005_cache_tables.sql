-- Migration: Add cache schema tables for consuming events from other domains
-- The cache schema stores local copies of data from other domains for read-only access

-- Ensure cache schema exists
CREATE SCHEMA IF NOT EXISTS cache;

-- Create the cache_config table (controls which events to listen for)
CREATE TABLE IF NOT EXISTS cache.cache_config (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_domain VARCHAR(100) NOT NULL,      -- e.g., "product", "order"
    source_schema VARCHAR(100) NOT NULL,      -- e.g., "fabrica"
    source_table VARCHAR(100) NOT NULL,       -- e.g., "product"
    listen_create BOOLEAN NOT NULL DEFAULT true,
    listen_update BOOLEAN NOT NULL DEFAULT true,
    listen_delete BOOLEAN NOT NULL DEFAULT true,
    is_active BOOLEAN NOT NULL DEFAULT true,
    cache_ttl_seconds INTEGER,                -- Optional TTL for cache entries
    description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_cache_config_source UNIQUE (source_domain, source_schema, source_table)
);

-- Create indexes for cache_config
CREATE INDEX IF NOT EXISTS idx_cache_config_active ON cache.cache_config (is_active);

-- Create the cache table (stores the actual cached data)
CREATE TABLE IF NOT EXISTS cache.cache (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_domain VARCHAR(100) NOT NULL,      -- e.g., "product"
    source_table VARCHAR(100) NOT NULL,       -- e.g., "product"
    aggregate_id UUID NOT NULL,               -- ID from source system
    tenant_id VARCHAR(100) NOT NULL,
    last_event_type VARCHAR(100) NOT NULL,    -- e.g., "product.created"
    cache_data JSONB NOT NULL,                -- The cached entity data
    version BIGINT NOT NULL DEFAULT 1,        -- For ordering/concurrency
    is_deleted BOOLEAN NOT NULL DEFAULT false,-- Soft delete flag
    cached_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,      -- Optional TTL
    source_event_id UUID,                     -- Original outbox event ID
    source_event_time TIMESTAMP WITH TIME ZONE, -- Original event timestamp
    CONSTRAINT uq_cache_source_aggregate UNIQUE (source_domain, source_table, aggregate_id)
);

-- Create indexes for cache table
CREATE INDEX IF NOT EXISTS idx_cache_tenant ON cache.cache (tenant_id);
CREATE INDEX IF NOT EXISTS idx_cache_deleted ON cache.cache (is_deleted);
CREATE INDEX IF NOT EXISTS idx_cache_expires ON cache.cache (expires_at);
CREATE INDEX IF NOT EXISTS idx_cache_lookup ON cache.cache (source_domain, source_table, tenant_id, is_deleted);

-- Add comments
COMMENT ON TABLE cache.cache_config IS 'Configuration for consuming events from other domains';
COMMENT ON TABLE cache.cache IS 'Local cache of data from other domains';

-- Insert example config: Admin domain listens for product events
INSERT INTO cache.cache_config (source_domain, source_schema, source_table, listen_create, listen_update, listen_delete, is_active, description)
VALUES ('product', 'fabrica', 'product', true, true, true, true, 'Cache product data from product domain')
ON CONFLICT (source_domain, source_schema, source_table) DO NOTHING;
