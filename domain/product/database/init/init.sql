-- Product Domain Database Initialization
-- Database: fabrica-product-db
-- Schemas: fabrica (domain data), cdc (change data capture/outbox)

-- Create schemas
CREATE SCHEMA IF NOT EXISTS fabrica;
CREATE SCHEMA IF NOT EXISTS cdc;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- FABRICA SCHEMA: Product Domain Aggregate
-- ============================================================================

-- Categories table
CREATE TABLE fabrica.category (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES fabrica.category(id),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    description TEXT,
    image_url VARCHAR(500),
    display_order INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    seo_meta_title VARCHAR(255),
    seo_meta_description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Products table
CREATE TABLE fabrica.product (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    sku VARCHAR(100) NOT NULL,
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    description TEXT,
    short_description VARCHAR(500),
    base_price DECIMAL(10, 2) NOT NULL,
    compare_at_price DECIMAL(10, 2),
    cost_price DECIMAL(10, 2),
    primary_image_url VARCHAR(500),
    status VARCHAR(50) DEFAULT 'draft', -- draft, active, archived
    product_type VARCHAR(100),
    vendor VARCHAR(255),
    weight DECIMAL(10, 2),
    weight_unit VARCHAR(20) DEFAULT 'lb',
    requires_shipping BOOLEAN DEFAULT true,
    is_taxable BOOLEAN DEFAULT true,
    track_inventory BOOLEAN DEFAULT true,
    seo_meta_title VARCHAR(255),
    seo_meta_description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, sku),
    UNIQUE(tenant_id, slug)
);

-- Product images table
CREATE TABLE fabrica.product_image (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES fabrica.product(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    alt_text VARCHAR(255),
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Product variants table (for size, color, etc.)
CREATE TABLE fabrica.product_variant (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES fabrica.product(id) ON DELETE CASCADE,
    sku VARCHAR(100) NOT NULL,
    name VARCHAR(255),
    price DECIMAL(10, 2),
    compare_at_price DECIMAL(10, 2),
    cost_price DECIMAL(10, 2),
    inventory_quantity INTEGER DEFAULT 0,
    image_url VARCHAR(500),
    weight DECIMAL(10, 2),
    position INTEGER DEFAULT 0,
    option1_name VARCHAR(100), -- e.g., "Size"
    option1_value VARCHAR(100), -- e.g., "Large"
    option2_name VARCHAR(100), -- e.g., "Color"
    option2_value VARCHAR(100), -- e.g., "Blue"
    option3_name VARCHAR(100),
    option3_value VARCHAR(100),
    barcode VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(sku)
);

-- Product-Category association (many-to-many)
CREATE TABLE fabrica.product_category (
    product_id UUID NOT NULL REFERENCES fabrica.product(id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES fabrica.category(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (product_id, category_id)
);

-- Product tags table
CREATE TABLE fabrica.product_tag (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    product_id UUID NOT NULL REFERENCES fabrica.product(id) ON DELETE CASCADE,
    tag VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(product_id, tag)
);

-- Inventory tracking table
CREATE TABLE fabrica.inventory (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    product_id UUID REFERENCES fabrica.product(id) ON DELETE CASCADE,
    variant_id UUID REFERENCES fabrica.product_variant(id) ON DELETE CASCADE,
    location VARCHAR(255) DEFAULT 'default',
    quantity_available INTEGER DEFAULT 0,
    quantity_reserved INTEGER DEFAULT 0,
    quantity_incoming INTEGER DEFAULT 0,
    reorder_point INTEGER,
    reorder_quantity INTEGER,
    last_restock_date TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CHECK (product_id IS NOT NULL OR variant_id IS NOT NULL)
);

-- ============================================================================
-- CDC SCHEMA: Change Data Capture / Outbox Pattern
-- ============================================================================

CREATE TABLE cdc.outbox (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL, -- 'product', 'category', etc.
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL, -- 'product.created', 'product.updated', etc.
    event_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending' -- pending, processing, processed, failed
);

-- ============================================================================
-- INDEXES for Performance
-- ============================================================================

-- Category indexes
CREATE INDEX idx_category_tenant ON fabrica.category(tenant_id);
CREATE INDEX idx_category_parent ON fabrica.category(parent_id);
CREATE INDEX idx_category_slug ON fabrica.category(tenant_id, slug);

-- Product indexes
CREATE INDEX idx_product_tenant ON fabrica.product(tenant_id);
CREATE INDEX idx_product_sku ON fabrica.product(tenant_id, sku);
CREATE INDEX idx_product_slug ON fabrica.product(tenant_id, slug);
CREATE INDEX idx_product_status ON fabrica.product(status);
CREATE INDEX idx_product_type ON fabrica.product(product_type);

-- Product variant indexes
CREATE INDEX idx_variant_product ON fabrica.product_variant(product_id);
CREATE INDEX idx_variant_sku ON fabrica.product_variant(sku);

-- Inventory indexes
CREATE INDEX idx_inventory_tenant ON fabrica.inventory(tenant_id);
CREATE INDEX idx_inventory_product ON fabrica.inventory(product_id);
CREATE INDEX idx_inventory_variant ON fabrica.inventory(variant_id);

-- Outbox indexes
CREATE INDEX idx_outbox_tenant ON cdc.outbox(tenant_id);
CREATE INDEX idx_outbox_status ON cdc.outbox(status);
CREATE INDEX idx_outbox_created ON cdc.outbox(created_at);

-- ============================================================================
-- SAMPLE DATA for Testing
-- ============================================================================

-- Insert sample categories
INSERT INTO fabrica.category (tenant_id, name, slug, description, display_order) VALUES
('tenant-test', 'Apparel', 'apparel', 'Clothing and fashion items', 1),
('tenant-test', 'Electronics', 'electronics', 'Electronic devices and gadgets', 2),
('tenant-test', 'Home & Garden', 'home-garden', 'Home decor and garden supplies', 3);

-- Insert sample products
INSERT INTO fabrica.product (
    tenant_id, sku, name, slug, description, short_description,
    base_price, compare_at_price, status, product_type
) VALUES
(
    'tenant-test',
    'TSHIRT-BLUE-001',
    'Classic Blue T-Shirt',
    'classic-blue-tshirt',
    'A comfortable, classic blue t-shirt made from 100% organic cotton. Perfect for casual wear.',
    'Comfortable organic cotton t-shirt',
    29.99,
    39.99,
    'active',
    'Apparel'
),
(
    'tenant-test',
    'LAPTOP-PRO-001',
    'Professional Laptop 15"',
    'professional-laptop-15',
    'High-performance laptop with 15" display, perfect for professionals and content creators.',
    'High-performance 15" laptop',
    1299.99,
    1499.99,
    'active',
    'Electronics'
),
(
    'tenant-test',
    'PLANT-POT-001',
    'Ceramic Plant Pot',
    'ceramic-plant-pot',
    'Beautiful handcrafted ceramic plant pot, perfect for indoor plants.',
    'Handcrafted ceramic pot',
    24.99,
    NULL,
    'active',
    'Home & Garden'
);

-- Link products to categories
INSERT INTO fabrica.product_category (product_id, category_id)
SELECT p.id, c.id
FROM fabrica.product p, fabrica.category c
WHERE p.sku = 'TSHIRT-BLUE-001' AND c.slug = 'apparel';

INSERT INTO fabrica.product_category (product_id, category_id)
SELECT p.id, c.id
FROM fabrica.product p, fabrica.category c
WHERE p.sku = 'LAPTOP-PRO-001' AND c.slug = 'electronics';

INSERT INTO fabrica.product_category (product_id, category_id)
SELECT p.id, c.id
FROM fabrica.product p, fabrica.category c
WHERE p.sku = 'PLANT-POT-001' AND c.slug = 'home-garden';

-- Insert sample inventory
INSERT INTO fabrica.inventory (tenant_id, product_id, quantity_available, reorder_point, reorder_quantity)
SELECT 'tenant-test', id, 100, 20, 50
FROM fabrica.product;

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

CREATE TRIGGER update_product_updated_at BEFORE UPDATE ON fabrica.product
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_category_updated_at BEFORE UPDATE ON fabrica.category
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_variant_updated_at BEFORE UPDATE ON fabrica.product_variant
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_inventory_updated_at BEFORE UPDATE ON fabrica.inventory
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
