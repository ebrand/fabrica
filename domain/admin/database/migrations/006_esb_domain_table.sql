-- Migration: Create fabrica.domain table for ESB domain registry
-- This table tracks all domains that participate in the Enterprise Service Bus

-- Ensure fabrica schema exists
CREATE SCHEMA IF NOT EXISTS fabrica;

-- Create the domain registry table
CREATE TABLE IF NOT EXISTS fabrica.domain (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    domain_name VARCHAR(100) NOT NULL,               -- e.g., "product", "admin", "order"
    display_name VARCHAR(255) NOT NULL,              -- e.g., "Product Domain", "Admin Domain"
    description VARCHAR(500),                        -- Description of the domain's responsibilities
    service_url VARCHAR(500),                        -- Base URL of the domain service (e.g., "http://product-service:3420")
    kafka_topic_prefix VARCHAR(100),                 -- Prefix for Kafka topics (usually same as domain_name)
    schema_name VARCHAR(100) NOT NULL DEFAULT 'fabrica', -- The schema used by this domain
    database_name VARCHAR(100),                      -- The database used by this domain (e.g., "fabrica-product-db")
    publishes_events BOOLEAN NOT NULL DEFAULT true,  -- Whether this domain publishes events to the ESB
    consumes_events BOOLEAN NOT NULL DEFAULT true,   -- Whether this domain consumes events from the ESB
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_domain_name UNIQUE (domain_name)
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_domain_active ON fabrica.domain (is_active);
CREATE INDEX IF NOT EXISTS idx_domain_publishes ON fabrica.domain (publishes_events);
CREATE INDEX IF NOT EXISTS idx_domain_consumes ON fabrica.domain (consumes_events);

-- Add comment
COMMENT ON TABLE fabrica.domain IS 'Registry of all domains participating in the Enterprise Service Bus (ESB)';

-- Insert initial domain records
INSERT INTO fabrica.domain (domain_name, display_name, description, service_url, kafka_topic_prefix, schema_name, database_name, publishes_events, consumes_events, is_active)
VALUES
    ('admin', 'Admin Domain', 'Handles user management, roles, permissions, and system administration', 'http://acl-admin:3600', 'admin', 'fabrica', 'fabrica-admin-db', true, true, true),
    ('product', 'Product Domain', 'Manages products, categories, variants, and inventory', 'http://product-service:3420', 'product', 'fabrica', 'fabrica-product-db', true, true, true)
ON CONFLICT (domain_name) DO NOTHING;
