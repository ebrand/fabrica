-- Customer Domain Database Initialization
-- Database: fabrica-customer-db
-- Schemas: fabrica (domain data), cdc (change data capture/outbox)

-- Create schemas
CREATE SCHEMA IF NOT EXISTS fabrica;
CREATE SCHEMA IF NOT EXISTS cdc;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- FABRICA SCHEMA: Customer Domain Aggregate
-- ============================================================================

-- Customers table
CREATE TABLE fabrica.customer (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(255),
    phone_number VARCHAR(50),
    date_of_birth DATE,
    gender VARCHAR(20),
    avatar_url VARCHAR(500),
    status VARCHAR(50) DEFAULT 'active', -- active, inactive, suspended
    email_verified BOOLEAN DEFAULT false,
    phone_verified BOOLEAN DEFAULT false,
    accepts_marketing BOOLEAN DEFAULT false,
    marketing_opt_in_date TIMESTAMP,
    total_orders INTEGER DEFAULT 0,
    total_spent DECIMAL(12, 2) DEFAULT 0.00,
    notes TEXT,
    tags VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, email)
);

-- Customer addresses table
CREATE TABLE fabrica.customer_address (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES fabrica.customer(id) ON DELETE CASCADE,
    address_type VARCHAR(50) NOT NULL DEFAULT 'shipping', -- shipping, billing
    is_default BOOLEAN DEFAULT false,
    first_name VARCHAR(100),
    last_name VARCHAR(100),
    company VARCHAR(255),
    address_line1 VARCHAR(255) NOT NULL,
    address_line2 VARCHAR(255),
    city VARCHAR(100) NOT NULL,
    state_province VARCHAR(100),
    postal_code VARCHAR(20),
    country VARCHAR(100) NOT NULL,
    phone_number VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Customer notes/activity log
CREATE TABLE fabrica.customer_note (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES fabrica.customer(id) ON DELETE CASCADE,
    note_type VARCHAR(50) DEFAULT 'general', -- general, order, support, internal
    content TEXT NOT NULL,
    created_by UUID,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Customer segments/groups
CREATE TABLE fabrica.customer_segment (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    description TEXT,
    criteria JSONB, -- Dynamic segment criteria
    is_dynamic BOOLEAN DEFAULT false,
    customer_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Customer-Segment association (many-to-many)
CREATE TABLE fabrica.customer_segment_member (
    customer_id UUID NOT NULL REFERENCES fabrica.customer(id) ON DELETE CASCADE,
    segment_id UUID NOT NULL REFERENCES fabrica.customer_segment(id) ON DELETE CASCADE,
    added_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (customer_id, segment_id)
);

-- ============================================================================
-- CDC SCHEMA: Change Data Capture / Outbox Pattern
-- ============================================================================

CREATE TABLE cdc.outbox (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- 'customer', 'customer_address', etc.
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- 'customer.created', 'customer.updated', etc.
    event_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending' -- pending, processing, processed, failed
);

-- ============================================================================
-- INDEXES for Performance
-- ============================================================================

-- Customer indexes
CREATE INDEX idx_customer_tenant ON fabrica.customer(tenant_id);
CREATE INDEX idx_customer_email ON fabrica.customer(tenant_id, email);
CREATE INDEX idx_customer_status ON fabrica.customer(status);
CREATE INDEX idx_customer_name ON fabrica.customer(last_name, first_name);
CREATE INDEX idx_customer_created ON fabrica.customer(created_at DESC);

-- Customer address indexes
CREATE INDEX idx_address_customer ON fabrica.customer_address(customer_id);
CREATE INDEX idx_address_type ON fabrica.customer_address(address_type);
CREATE INDEX idx_address_default ON fabrica.customer_address(customer_id, is_default);

-- Customer note indexes
CREATE INDEX idx_note_customer ON fabrica.customer_note(customer_id);
CREATE INDEX idx_note_type ON fabrica.customer_note(note_type);
CREATE INDEX idx_note_created ON fabrica.customer_note(created_at DESC);

-- Customer segment indexes
CREATE INDEX idx_segment_tenant ON fabrica.customer_segment(tenant_id);
CREATE INDEX idx_segment_slug ON fabrica.customer_segment(tenant_id, slug);

-- Outbox indexes
CREATE INDEX idx_outbox_tenant ON cdc.outbox(tenant_id);
CREATE INDEX idx_outbox_status ON cdc.outbox(status);
CREATE INDEX idx_outbox_created ON cdc.outbox(created_at);

-- ============================================================================
-- SAMPLE DATA for Testing
-- ============================================================================

-- Insert sample customer segments
INSERT INTO fabrica.customer_segment (tenant_id, name, slug, description) VALUES
('tenant-test', 'VIP Customers', 'vip', 'High-value customers with premium benefits'),
('tenant-test', 'Newsletter Subscribers', 'newsletter', 'Customers who opted in to marketing emails'),
('tenant-test', 'New Customers', 'new-customers', 'Customers who joined in the last 30 days');

-- Insert sample customers
INSERT INTO fabrica.customer (
    tenant_id, email, first_name, last_name, display_name,
    phone_number, status, email_verified, accepts_marketing, total_orders, total_spent
) VALUES
(
    'tenant-test',
    'john.doe@example.com',
    'John',
    'Doe',
    'John Doe',
    '+1-555-0101',
    'active',
    true,
    true,
    5,
    549.95
),
(
    'tenant-test',
    'jane.smith@example.com',
    'Jane',
    'Smith',
    'Jane Smith',
    '+1-555-0102',
    'active',
    true,
    false,
    12,
    1249.50
),
(
    'tenant-test',
    'robert.wilson@example.com',
    'Robert',
    'Wilson',
    'Bob Wilson',
    '+1-555-0103',
    'active',
    true,
    true,
    3,
    189.97
),
(
    'tenant-test',
    'emily.chen@example.com',
    'Emily',
    'Chen',
    'Emily Chen',
    '+1-555-0104',
    'active',
    false,
    true,
    0,
    0.00
),
(
    'tenant-test',
    'michael.johnson@example.com',
    'Michael',
    'Johnson',
    'Mike Johnson',
    '+1-555-0105',
    'inactive',
    true,
    false,
    8,
    892.40
);

-- Insert sample addresses for customers
INSERT INTO fabrica.customer_address (
    customer_id, address_type, is_default, first_name, last_name,
    address_line1, address_line2, city, state_province, postal_code, country
)
SELECT
    c.id,
    'shipping',
    true,
    c.first_name,
    c.last_name,
    '123 Main Street',
    'Apt 4B',
    'New York',
    'NY',
    '10001',
    'United States'
FROM fabrica.customer c WHERE c.email = 'john.doe@example.com';

INSERT INTO fabrica.customer_address (
    customer_id, address_type, is_default, first_name, last_name,
    address_line1, city, state_province, postal_code, country
)
SELECT
    c.id,
    'shipping',
    true,
    c.first_name,
    c.last_name,
    '456 Oak Avenue',
    'Los Angeles',
    'CA',
    '90001',
    'United States'
FROM fabrica.customer c WHERE c.email = 'jane.smith@example.com';

INSERT INTO fabrica.customer_address (
    customer_id, address_type, is_default, first_name, last_name,
    address_line1, city, state_province, postal_code, country
)
SELECT
    c.id,
    'billing',
    false,
    c.first_name,
    c.last_name,
    '456 Oak Avenue',
    'Los Angeles',
    'CA',
    '90001',
    'United States'
FROM fabrica.customer c WHERE c.email = 'jane.smith@example.com';

-- Add VIP customers to VIP segment
INSERT INTO fabrica.customer_segment_member (customer_id, segment_id)
SELECT c.id, s.id
FROM fabrica.customer c, fabrica.customer_segment s
WHERE c.email = 'jane.smith@example.com' AND s.slug = 'vip';

-- Add marketing opt-in customers to newsletter segment
INSERT INTO fabrica.customer_segment_member (customer_id, segment_id)
SELECT c.id, s.id
FROM fabrica.customer c, fabrica.customer_segment s
WHERE c.accepts_marketing = true AND s.slug = 'newsletter';

-- ============================================================================
-- TRIGGERS for updated_at timestamps
-- ============================================================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_customer_updated_at BEFORE UPDATE ON fabrica.customer
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customer_address_updated_at BEFORE UPDATE ON fabrica.customer_address
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customer_segment_updated_at BEFORE UPDATE ON fabrica.customer_segment
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

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
