-- Content Domain Database Initialization
-- Database: fabrica-content-db
-- Schemas: fabrica (domain data), cdc (change data capture/outbox), cache (external domain data)

-- Create schemas
CREATE SCHEMA IF NOT EXISTS fabrica;
CREATE SCHEMA IF NOT EXISTS cdc;
CREATE SCHEMA IF NOT EXISTS cache;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ============================================================================
-- FABRICA SCHEMA: Content Domain Aggregate
-- ============================================================================

-- ===================
-- LOCALIZATION SUPPORT
-- ===================

-- Languages/Locales table
-- Stores all supported languages for the multi-tenant platform
CREATE TABLE fabrica.language (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    locale_code VARCHAR(10) NOT NULL,         -- e.g., "en-US", "es-MX", "fr-FR"
    language_code VARCHAR(5) NOT NULL,        -- e.g., "en", "es", "fr"
    name VARCHAR(100) NOT NULL,               -- e.g., "English (US)"
    native_name VARCHAR(100),                 -- e.g., "English", "Espanol"
    is_default BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    direction VARCHAR(3) DEFAULT 'ltr',       -- "ltr" or "rtl"
    date_format VARCHAR(50),                  -- e.g., "MM/DD/YYYY"
    currency_code VARCHAR(3),                 -- e.g., "USD", "EUR"
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, locale_code)
);

-- ===================
-- BLOCK STRUCTURE (Schema-driven Content Blocks)
-- ===================

-- Block template definitions (Article, Card, Modal, etc.)
CREATE TABLE fabrica.block (
    block_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,               -- Display name: "Article", "Card"
    slug VARCHAR(100) NOT NULL,               -- Identifier: "article", "card"
    description TEXT,                         -- Admin description
    icon VARCHAR(50),                         -- Icon for UI
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Section type definitions (Title, Subtitle, Author, Body, etc.)
CREATE TABLE fabrica.section_type (
    section_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,               -- Display name: "Title", "Subtitle"
    slug VARCHAR(100) NOT NULL,               -- Identifier: "title", "subtitle"
    description TEXT,
    field_type VARCHAR(50) DEFAULT 'text',    -- text, richtext, image, video, date, etc.
    validation_rules JSONB,                   -- Optional validation config
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Junction table: Block to Section Types (defines which sections a block contains)
CREATE TABLE fabrica.block_section (
    block_id UUID NOT NULL REFERENCES fabrica.block(block_id) ON DELETE CASCADE,
    section_type_id UUID NOT NULL REFERENCES fabrica.section_type(section_type_id) ON DELETE CASCADE,
    is_required BOOLEAN DEFAULT false,
    display_order INTEGER DEFAULT 0,
    default_value TEXT,                       -- Default content for this section
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (block_id, section_type_id)
);

-- Visual variants for each block (Article→Long Form, Summary; Card→Dark, Hero)
CREATE TABLE fabrica.variant (
    variant_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    block_id UUID NOT NULL REFERENCES fabrica.block(block_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,               -- Display name: "Long Form", "Hero"
    slug VARCHAR(100) NOT NULL,               -- Identifier: "long-form", "hero"
    description TEXT,
    preview_image_url VARCHAR(500),           -- Preview image for admin UI
    css_class VARCHAR(100),                   -- Optional CSS class
    settings JSONB,                           -- Variant-specific settings
    is_default BOOLEAN DEFAULT false,         -- Default variant for this block
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(block_id, slug)
);

-- Content instances - actual content pieces created from block templates
CREATE TABLE fabrica.block_content (
    content_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    block_id UUID NOT NULL REFERENCES fabrica.block(block_id),
    default_variant_id UUID REFERENCES fabrica.variant(variant_id),
    slug VARCHAR(255) NOT NULL,               -- Unique identifier for lookup
    name VARCHAR(255) NOT NULL,               -- Admin name for the content
    description TEXT,                         -- Admin description
    access_control VARCHAR(50) DEFAULT 'everyone', -- Access control: everyone, authenticated, roles
    visibility VARCHAR(50) DEFAULT 'public',  -- public, private, scheduled
    publish_at TIMESTAMP,
    unpublish_at TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Translated content values for each section of each content piece
CREATE TABLE fabrica.block_content_section_translation (
    section_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_id UUID NOT NULL REFERENCES fabrica.block_content(content_id) ON DELETE CASCADE,
    section_type_id UUID NOT NULL REFERENCES fabrica.section_type(section_type_id),
    language_id UUID NOT NULL REFERENCES fabrica.language(id),
    content TEXT,                             -- The actual translated content
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(content_id, section_type_id, language_id)
);

-- ===================
-- BLOCK CATEGORIES & TAGS
-- ===================

-- Block Category - categories for organizing block content
CREATE TABLE fabrica.block_category (
    category_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES fabrica.block_category(category_id) ON DELETE RESTRICT,
    slug VARCHAR(100) NOT NULL,
    icon VARCHAR(50),
    color VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Block Category Translations
CREATE TABLE fabrica.block_category_translation (
    translation_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    category_id UUID NOT NULL REFERENCES fabrica.block_category(category_id) ON DELETE CASCADE,
    language_id UUID NOT NULL REFERENCES fabrica.language(id) ON DELETE RESTRICT,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(category_id, language_id)
);

-- Junction: Block Content to Category (many-to-many)
CREATE TABLE fabrica.block_content_category (
    content_id UUID NOT NULL REFERENCES fabrica.block_content(content_id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES fabrica.block_category(category_id) ON DELETE CASCADE,
    is_primary BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, category_id)
);

-- Block Tag - tags for labeling block content
CREATE TABLE fabrica.block_tag (
    tag_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    color VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Block Tag Translations
CREATE TABLE fabrica.block_tag_translation (
    translation_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tag_id UUID NOT NULL REFERENCES fabrica.block_tag(tag_id) ON DELETE CASCADE,
    language_id UUID NOT NULL REFERENCES fabrica.language(id) ON DELETE RESTRICT,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tag_id, language_id)
);

-- Junction: Block Content to Tag (many-to-many)
CREATE TABLE fabrica.block_content_tag (
    content_id UUID NOT NULL REFERENCES fabrica.block_content(content_id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES fabrica.block_tag(tag_id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, tag_id)
);

-- ===================
-- MEDIA / ASSETS
-- ===================

-- Media folders for organization
CREATE TABLE fabrica.media_folder (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES fabrica.media_folder(id),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, parent_id, slug)
);

-- Media assets table
CREATE TABLE fabrica.media (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    folder_id UUID REFERENCES fabrica.media_folder(id),
    file_name VARCHAR(255) NOT NULL,
    original_file_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(1000) NOT NULL,
    file_url VARCHAR(1000) NOT NULL,
    mime_type VARCHAR(100) NOT NULL,
    file_size BIGINT NOT NULL,                -- Size in bytes
    file_extension VARCHAR(20),
    media_type VARCHAR(50) NOT NULL,          -- image, video, audio, document, other
    width INTEGER,                            -- For images/videos
    height INTEGER,                           -- For images/videos
    duration INTEGER,                         -- For audio/video (seconds)
    thumbnail_url VARCHAR(1000),
    blurhash VARCHAR(100),                    -- For image placeholders
    metadata JSONB,                           -- EXIF, video codecs, etc.
    is_public BOOLEAN DEFAULT true,
    uploaded_by UUID,                         -- Reference to user
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Media translations (alt text, captions, etc.)
CREATE TABLE fabrica.media_translation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    media_id UUID NOT NULL REFERENCES fabrica.media(id) ON DELETE CASCADE,
    locale_code VARCHAR(10) NOT NULL,
    alt_text VARCHAR(500),
    title VARCHAR(255),
    caption TEXT,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(media_id, locale_code)
);

-- ===================
-- MENUS / NAVIGATION
-- ===================

-- Menu definitions
CREATE TABLE fabrica.menu (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL,               -- e.g., "main", "footer", "sidebar"
    name VARCHAR(255) NOT NULL,
    location VARCHAR(100),                    -- Where the menu appears
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, code)
);

-- Menu items
CREATE TABLE fabrica.menu_item (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    menu_id UUID NOT NULL REFERENCES fabrica.menu(id) ON DELETE CASCADE,
    parent_id UUID REFERENCES fabrica.menu_item(id),
    link_type VARCHAR(50) NOT NULL,           -- block_content, url, category, placeholder
    block_content_id UUID REFERENCES fabrica.block_content(content_id),
    block_category_id UUID REFERENCES fabrica.block_category(category_id),
    url VARCHAR(1000),
    target VARCHAR(20) DEFAULT '_self',       -- _self, _blank
    icon VARCHAR(50),
    css_class VARCHAR(100),
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Menu item translations
CREATE TABLE fabrica.menu_item_translation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    menu_item_id UUID NOT NULL REFERENCES fabrica.menu_item(id) ON DELETE CASCADE,
    locale_code VARCHAR(10) NOT NULL,
    label VARCHAR(255) NOT NULL,
    title VARCHAR(255),                       -- Tooltip/title attribute
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(menu_item_id, locale_code)
);

-- ===================
-- REDIRECTS
-- ===================

-- URL redirects for SEO and content migration
CREATE TABLE fabrica.redirect (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    source_path VARCHAR(1000) NOT NULL,
    target_path VARCHAR(1000) NOT NULL,
    redirect_type INTEGER DEFAULT 301,        -- 301 (permanent), 302 (temporary)
    is_regex BOOLEAN DEFAULT false,
    is_active BOOLEAN DEFAULT true,
    hit_count INTEGER DEFAULT 0,
    last_hit_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, source_path)
);

-- ============================================================================
-- CDC SCHEMA: Change Data Capture / Outbox Pattern
-- ============================================================================

CREATE TABLE cdc.outbox (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,     -- 'block_content', 'media', etc.
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,         -- 'block_content.created', 'block_content.updated', etc.
    event_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending'      -- pending, processing, processed, failed
);

-- Outbox configuration table
CREATE TABLE cdc.outbox_config (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    schema_name VARCHAR(100) NOT NULL,
    table_name VARCHAR(100) NOT NULL,
    domain_name VARCHAR(100) NOT NULL DEFAULT 'content',
    topic_name VARCHAR(255) NOT NULL,
    capture_insert BOOLEAN NOT NULL DEFAULT true,
    capture_update BOOLEAN NOT NULL DEFAULT true,
    capture_delete BOOLEAN NOT NULL DEFAULT true,
    is_active BOOLEAN NOT NULL DEFAULT true,
    description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_outbox_config_schema_table UNIQUE (schema_name, table_name)
);

-- ============================================================================
-- CACHE SCHEMA: External Domain Data Cache
-- ============================================================================

-- Cache configuration table (controls which events to listen for)
CREATE TABLE cache.cache_config (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_domain VARCHAR(100) NOT NULL,
    source_schema VARCHAR(100) NOT NULL,
    source_table VARCHAR(100) NOT NULL,
    consumer_group VARCHAR(255) NOT NULL,
    listen_create BOOLEAN NOT NULL DEFAULT true,
    listen_update BOOLEAN NOT NULL DEFAULT true,
    listen_delete BOOLEAN NOT NULL DEFAULT true,
    is_active BOOLEAN NOT NULL DEFAULT true,
    cache_ttl_seconds INTEGER,
    description VARCHAR(500),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_cache_config_source UNIQUE (source_domain, source_schema, source_table)
);

-- Cache table (stores the actual cached data)
CREATE TABLE cache.cache (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    source_domain VARCHAR(100) NOT NULL,
    source_table VARCHAR(100) NOT NULL,
    aggregate_id UUID NOT NULL,
    tenant_id VARCHAR(100) NOT NULL,
    last_event_type VARCHAR(100) NOT NULL,
    cache_data JSONB NOT NULL,
    version BIGINT NOT NULL DEFAULT 1,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    cached_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE,
    source_event_id UUID,
    source_event_time TIMESTAMP WITH TIME ZONE,
    CONSTRAINT uq_cache_source_aggregate UNIQUE (source_domain, source_table, aggregate_id)
);

-- ============================================================================
-- INDEXES for Performance
-- ============================================================================

-- Language indexes
CREATE INDEX idx_language_tenant ON fabrica.language(tenant_id);
CREATE INDEX idx_language_default ON fabrica.language(tenant_id, is_default);

-- Block structure indexes
CREATE INDEX idx_block_tenant ON fabrica.block(tenant_id);
CREATE INDEX idx_block_slug ON fabrica.block(tenant_id, slug);
CREATE INDEX idx_block_active ON fabrica.block(tenant_id, is_active);

CREATE INDEX idx_section_type_tenant ON fabrica.section_type(tenant_id);
CREATE INDEX idx_section_type_slug ON fabrica.section_type(tenant_id, slug);

CREATE INDEX idx_block_section_block ON fabrica.block_section(block_id);
CREATE INDEX idx_block_section_section ON fabrica.block_section(section_type_id);

CREATE INDEX idx_variant_block ON fabrica.variant(block_id);
CREATE INDEX idx_variant_slug ON fabrica.variant(block_id, slug);
CREATE INDEX idx_variant_default ON fabrica.variant(block_id, is_default);

CREATE INDEX idx_block_content_tenant ON fabrica.block_content(tenant_id);
CREATE INDEX idx_block_content_slug ON fabrica.block_content(tenant_id, slug);
CREATE INDEX idx_block_content_block ON fabrica.block_content(block_id);
CREATE INDEX idx_block_content_active ON fabrica.block_content(tenant_id, is_active);

CREATE INDEX idx_bcst_content ON fabrica.block_content_section_translation(content_id);
CREATE INDEX idx_bcst_section ON fabrica.block_content_section_translation(section_type_id);
CREATE INDEX idx_bcst_language ON fabrica.block_content_section_translation(language_id);
CREATE INDEX idx_bcst_lookup ON fabrica.block_content_section_translation(content_id, language_id);

-- Block Category indexes
CREATE INDEX idx_block_category_tenant ON fabrica.block_category(tenant_id);
CREATE INDEX idx_block_category_parent ON fabrica.block_category(parent_id);
CREATE INDEX idx_block_category_translation_category ON fabrica.block_category_translation(category_id);
CREATE INDEX idx_block_content_category_content ON fabrica.block_content_category(content_id);
CREATE INDEX idx_block_content_category_category ON fabrica.block_content_category(category_id);

-- Block Tag indexes
CREATE INDEX idx_block_tag_tenant ON fabrica.block_tag(tenant_id);
CREATE INDEX idx_block_tag_translation_tag ON fabrica.block_tag_translation(tag_id);
CREATE INDEX idx_block_content_tag_content ON fabrica.block_content_tag(content_id);
CREATE INDEX idx_block_content_tag_tag ON fabrica.block_content_tag(tag_id);

-- Media indexes
CREATE INDEX idx_media_tenant ON fabrica.media(tenant_id);
CREATE INDEX idx_media_folder ON fabrica.media(folder_id);
CREATE INDEX idx_media_type ON fabrica.media(media_type);

-- Menu indexes
CREATE INDEX idx_menu_tenant ON fabrica.menu(tenant_id);
CREATE INDEX idx_menu_item_menu ON fabrica.menu_item(menu_id);
CREATE INDEX idx_menu_item_parent ON fabrica.menu_item(parent_id);

-- Redirect indexes
CREATE INDEX idx_redirect_tenant ON fabrica.redirect(tenant_id);
CREATE INDEX idx_redirect_source ON fabrica.redirect(tenant_id, source_path);

-- Outbox indexes
CREATE INDEX idx_outbox_tenant ON cdc.outbox(tenant_id);
CREATE INDEX idx_outbox_status ON cdc.outbox(status);
CREATE INDEX idx_outbox_created ON cdc.outbox(created_at);

-- Cache indexes
CREATE INDEX idx_cache_tenant ON cache.cache(tenant_id);
CREATE INDEX idx_cache_deleted ON cache.cache(is_deleted);
CREATE INDEX idx_cache_expires ON cache.cache(expires_at);
CREATE INDEX idx_cache_lookup ON cache.cache(source_domain, source_table, tenant_id, is_deleted);
CREATE INDEX idx_cache_config_active ON cache.cache_config(is_active);

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

CREATE TRIGGER update_language_updated_at BEFORE UPDATE ON fabrica.language
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_block_updated_at BEFORE UPDATE ON fabrica.block
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_section_type_updated_at BEFORE UPDATE ON fabrica.section_type
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_variant_updated_at BEFORE UPDATE ON fabrica.variant
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_block_content_updated_at BEFORE UPDATE ON fabrica.block_content
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_bcst_updated_at BEFORE UPDATE ON fabrica.block_content_section_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_block_category_updated_at BEFORE UPDATE ON fabrica.block_category
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_block_category_translation_updated_at BEFORE UPDATE ON fabrica.block_category_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_block_tag_updated_at BEFORE UPDATE ON fabrica.block_tag
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_block_tag_translation_updated_at BEFORE UPDATE ON fabrica.block_tag_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_media_updated_at BEFORE UPDATE ON fabrica.media
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_media_translation_updated_at BEFORE UPDATE ON fabrica.media_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_media_folder_updated_at BEFORE UPDATE ON fabrica.media_folder
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_menu_updated_at BEFORE UPDATE ON fabrica.menu
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_menu_item_updated_at BEFORE UPDATE ON fabrica.menu_item
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_menu_item_translation_updated_at BEFORE UPDATE ON fabrica.menu_item_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_redirect_updated_at BEFORE UPDATE ON fabrica.redirect
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ============================================================================
-- SAMPLE DATA for Testing
-- ============================================================================

-- Insert sample languages
INSERT INTO fabrica.language (tenant_id, locale_code, language_code, name, native_name, is_default, direction) VALUES
('tenant-test', 'en-US', 'en', 'English (US)', 'English', true, 'ltr'),
('tenant-test', 'es-ES', 'es', 'Spanish (Spain)', 'Espanol', false, 'ltr'),
('tenant-test', 'fr-FR', 'fr', 'French (France)', 'Francais', false, 'ltr'),
('tenant-test', 'de-DE', 'de', 'German (Germany)', 'Deutsch', false, 'ltr'),
('tenant-test', 'ar-SA', 'ar', 'Arabic (Saudi Arabia)', 'Arabic', false, 'rtl');

-- Insert section types (reusable field definitions)
INSERT INTO fabrica.section_type (tenant_id, name, slug, description, field_type, display_order) VALUES
('tenant-test', 'Title', 'title', 'Main title/heading', 'text', 1),
('tenant-test', 'Subtitle', 'subtitle', 'Secondary heading or tagline', 'text', 2),
('tenant-test', 'Body', 'body', 'Main content area (rich text)', 'richtext', 3),
('tenant-test', 'Author', 'author', 'Content author name', 'text', 4),
('tenant-test', 'CTA Text', 'cta-text', 'Call-to-action button text', 'text', 5),
('tenant-test', 'CTA URL', 'cta-url', 'Call-to-action button URL', 'text', 6),
('tenant-test', 'Image URL', 'image-url', 'Featured image URL', 'text', 7);

-- Insert blocks (content templates)
INSERT INTO fabrica.block (tenant_id, name, slug, description, icon, display_order) VALUES
('tenant-test', 'Article', 'article', 'Long-form article with author', 'document-text', 1),
('tenant-test', 'Card', 'card', 'Content card with optional CTA', 'rectangle-stack', 2);

-- Link blocks to their section types
DO $$
DECLARE
    v_article_id UUID;
    v_card_id UUID;
    v_title_id UUID;
    v_subtitle_id UUID;
    v_body_id UUID;
    v_author_id UUID;
    v_cta_text_id UUID;
    v_cta_url_id UUID;
    v_image_url_id UUID;
BEGIN
    SELECT block_id INTO v_article_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'article';
    SELECT block_id INTO v_card_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'card';

    SELECT section_type_id INTO v_title_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'title';
    SELECT section_type_id INTO v_subtitle_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'subtitle';
    SELECT section_type_id INTO v_body_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'body';
    SELECT section_type_id INTO v_author_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'author';
    SELECT section_type_id INTO v_cta_text_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-text';
    SELECT section_type_id INTO v_cta_url_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-url';
    SELECT section_type_id INTO v_image_url_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'image-url';

    -- Article sections
    INSERT INTO fabrica.block_section (block_id, section_type_id, is_required, display_order) VALUES
    (v_article_id, v_title_id, true, 1),
    (v_article_id, v_subtitle_id, false, 2),
    (v_article_id, v_body_id, true, 3),
    (v_article_id, v_author_id, false, 4);

    -- Card sections
    INSERT INTO fabrica.block_section (block_id, section_type_id, is_required, display_order) VALUES
    (v_card_id, v_title_id, true, 1),
    (v_card_id, v_subtitle_id, false, 2),
    (v_card_id, v_body_id, false, 3),
    (v_card_id, v_cta_text_id, false, 4),
    (v_card_id, v_cta_url_id, false, 5),
    (v_card_id, v_image_url_id, false, 6);
END $$;

-- Insert variants for blocks
DO $$
DECLARE
    v_article_id UUID;
    v_card_id UUID;
BEGIN
    SELECT block_id INTO v_article_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'article';
    SELECT block_id INTO v_card_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'card';

    -- Article variants
    INSERT INTO fabrica.variant (block_id, name, slug, description, css_class, is_default, display_order) VALUES
    (v_article_id, 'Long Form', 'long-form', 'Full article with all sections visible', 'article-long-form', true, 1),
    (v_article_id, 'Summary', 'summary', 'Condensed view with title and excerpt', 'article-summary', false, 2);

    -- Card variants
    INSERT INTO fabrica.variant (block_id, name, slug, description, css_class, is_default, display_order) VALUES
    (v_card_id, 'Default', 'default', 'Standard card layout', 'card-default', true, 1),
    (v_card_id, 'Hero', 'hero', 'Full-width hero card', 'card-hero', false, 2),
    (v_card_id, 'Dark', 'dark', 'Dark background card', 'card-dark', false, 3);
END $$;

-- Insert sample block content with translations
DO $$
DECLARE
    v_card_id UUID;
    v_hero_variant_id UUID;
    v_en_lang_id UUID;
    v_es_lang_id UUID;
    v_content_id UUID;
    v_title_id UUID;
    v_subtitle_id UUID;
    v_body_id UUID;
    v_cta_text_id UUID;
    v_cta_url_id UUID;
BEGIN
    SELECT block_id INTO v_card_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'card';
    SELECT variant_id INTO v_hero_variant_id FROM fabrica.variant WHERE block_id = v_card_id AND slug = 'hero';
    SELECT id INTO v_en_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-ES';

    SELECT section_type_id INTO v_title_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'title';
    SELECT section_type_id INTO v_subtitle_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'subtitle';
    SELECT section_type_id INTO v_body_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'body';
    SELECT section_type_id INTO v_cta_text_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-text';
    SELECT section_type_id INTO v_cta_url_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-url';

    -- Create hero-welcome content
    INSERT INTO fabrica.block_content (tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES ('tenant-test', v_card_id, v_hero_variant_id, 'hero-welcome', 'Hero Welcome Banner', 'Homepage hero banner')
    RETURNING content_id INTO v_content_id;

    -- English translations
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
    (v_content_id, v_title_id, v_en_lang_id, 'Welcome to Fabrica Commerce'),
    (v_content_id, v_subtitle_id, v_en_lang_id, 'Build Your Dream Store'),
    (v_content_id, v_body_id, v_en_lang_id, '<p>Fabrica Commerce Cloud provides everything you need to launch and grow your online business. Our platform offers <strong>powerful tools</strong>, seamless integrations, and world-class support.</p><p>Start your journey today and join thousands of successful merchants worldwide.</p>'),
    (v_content_id, v_cta_text_id, v_en_lang_id, 'Get Started'),
    (v_content_id, v_cta_url_id, v_en_lang_id, '/signup');

    -- Spanish translations
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
    (v_content_id, v_title_id, v_es_lang_id, 'Bienvenido a Fabrica Commerce'),
    (v_content_id, v_subtitle_id, v_es_lang_id, 'Construye la Tienda de tus Suenos'),
    (v_content_id, v_body_id, v_es_lang_id, '<p>Fabrica Commerce Cloud proporciona todo lo que necesitas para lanzar y hacer crecer tu negocio en linea.</p>'),
    (v_content_id, v_cta_text_id, v_es_lang_id, 'Comenzar'),
    (v_content_id, v_cta_url_id, v_es_lang_id, '/registro');
END $$;

-- Insert sample categories
DO $$
DECLARE
    v_en_lang_id UUID;
    v_es_lang_id UUID;
    v_cat_news_id UUID;
    v_cat_tutorials_id UUID;
    v_cat_announcements_id UUID;
BEGIN
    SELECT id INTO v_en_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-ES';

    -- Create Categories
    INSERT INTO fabrica.block_category (tenant_id, slug, icon, color, display_order) VALUES
    ('tenant-test', 'news', 'newspaper', '#3B82F6', 1),
    ('tenant-test', 'tutorials', 'academic-cap', '#10B981', 2),
    ('tenant-test', 'announcements', 'megaphone', '#F59E0B', 3);

    SELECT category_id INTO v_cat_news_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'news';
    SELECT category_id INTO v_cat_tutorials_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'tutorials';
    SELECT category_id INTO v_cat_announcements_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'announcements';

    -- Category Translations (English)
    INSERT INTO fabrica.block_category_translation (category_id, language_id, name, description) VALUES
    (v_cat_news_id, v_en_lang_id, 'News', 'Latest news and updates'),
    (v_cat_tutorials_id, v_en_lang_id, 'Tutorials', 'How-to guides and tutorials'),
    (v_cat_announcements_id, v_en_lang_id, 'Announcements', 'Important announcements');

    -- Category Translations (Spanish)
    INSERT INTO fabrica.block_category_translation (category_id, language_id, name, description) VALUES
    (v_cat_news_id, v_es_lang_id, 'Noticias', 'Ultimas noticias y actualizaciones'),
    (v_cat_tutorials_id, v_es_lang_id, 'Tutoriales', 'Guias y tutoriales'),
    (v_cat_announcements_id, v_es_lang_id, 'Anuncios', 'Anuncios importantes');
END $$;

-- Insert sample tags
DO $$
DECLARE
    v_en_lang_id UUID;
    v_es_lang_id UUID;
    v_tag_featured_id UUID;
    v_tag_new_id UUID;
    v_tag_popular_id UUID;
BEGIN
    SELECT id INTO v_en_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-ES';

    -- Create Tags
    INSERT INTO fabrica.block_tag (tenant_id, slug, color) VALUES
    ('tenant-test', 'featured', '#EF4444'),
    ('tenant-test', 'new', '#22C55E'),
    ('tenant-test', 'popular', '#8B5CF6');

    SELECT tag_id INTO v_tag_featured_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'featured';
    SELECT tag_id INTO v_tag_new_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'new';
    SELECT tag_id INTO v_tag_popular_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'popular';

    -- Tag Translations (English)
    INSERT INTO fabrica.block_tag_translation (tag_id, language_id, name) VALUES
    (v_tag_featured_id, v_en_lang_id, 'Featured'),
    (v_tag_new_id, v_en_lang_id, 'New'),
    (v_tag_popular_id, v_en_lang_id, 'Popular');

    -- Tag Translations (Spanish)
    INSERT INTO fabrica.block_tag_translation (tag_id, language_id, name) VALUES
    (v_tag_featured_id, v_es_lang_id, 'Destacado'),
    (v_tag_new_id, v_es_lang_id, 'Nuevo'),
    (v_tag_popular_id, v_es_lang_id, 'Popular');
END $$;

-- Link hero-welcome content to categories and tags
DO $$
DECLARE
    v_content_id UUID;
    v_cat_announcements_id UUID;
    v_tag_featured_id UUID;
    v_tag_new_id UUID;
BEGIN
    SELECT content_id INTO v_content_id FROM fabrica.block_content WHERE tenant_id = 'tenant-test' AND slug = 'hero-welcome';
    SELECT category_id INTO v_cat_announcements_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'announcements';
    SELECT tag_id INTO v_tag_featured_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'featured';
    SELECT tag_id INTO v_tag_new_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'new';

    INSERT INTO fabrica.block_content_category (content_id, category_id, is_primary) VALUES
    (v_content_id, v_cat_announcements_id, true);

    INSERT INTO fabrica.block_content_tag (content_id, tag_id) VALUES
    (v_content_id, v_tag_featured_id),
    (v_content_id, v_tag_new_id);
END $$;

-- Insert sample menu
INSERT INTO fabrica.menu (tenant_id, code, name, location) VALUES
('tenant-test', 'main', 'Main Navigation', 'header'),
('tenant-test', 'footer', 'Footer Navigation', 'footer');

-- Insert outbox config for block_content and media tables
INSERT INTO cdc.outbox_config (schema_name, table_name, domain_name, topic_name, capture_insert, capture_update, capture_delete, is_active, description) VALUES
('fabrica', 'block_content', 'content', 'content.block_content', true, true, true, true, 'Capture all block content entity changes'),
('fabrica', 'media', 'content', 'content.media', true, true, true, true, 'Capture all media entity changes'),
('fabrica', 'block_category', 'content', 'content.block_category', true, true, true, true, 'Capture all block category changes'),
('fabrica', 'block_tag', 'content', 'content.block_tag', true, true, true, true, 'Capture all block tag changes');

-- Insert example cache config: Content domain listens for product events (for product-related content)
INSERT INTO cache.cache_config (source_domain, source_schema, source_table, consumer_group, listen_create, listen_update, listen_delete, is_active, description)
VALUES ('product', 'fabrica', 'product', 'content-product.product', true, true, true, true, 'Cache product data for content linking');

-- ============================================================================
-- Comments
-- ============================================================================

COMMENT ON TABLE fabrica.language IS 'Supported languages/locales for multi-language content';
COMMENT ON TABLE fabrica.block IS 'Block template definitions - defines the structure of a content block';
COMMENT ON TABLE fabrica.section_type IS 'Section type definitions - defines types of fields that can appear in blocks';
COMMENT ON TABLE fabrica.block_section IS 'Junction table linking blocks to their allowed section types';
COMMENT ON TABLE fabrica.variant IS 'Visual presentation styles for each block type';
COMMENT ON TABLE fabrica.block_content IS 'Content instances - actual content pieces created from block templates';
COMMENT ON TABLE fabrica.block_content_section_translation IS 'Translated content values for each section of each content piece';
COMMENT ON TABLE fabrica.block_category IS 'Categories for organizing block content';
COMMENT ON TABLE fabrica.block_category_translation IS 'Translated names and descriptions for categories';
COMMENT ON TABLE fabrica.block_content_category IS 'Junction table linking block content to categories';
COMMENT ON TABLE fabrica.block_tag IS 'Tags for labeling block content';
COMMENT ON TABLE fabrica.block_tag_translation IS 'Translated names for tags';
COMMENT ON TABLE fabrica.block_content_tag IS 'Junction table linking block content to tags';
COMMENT ON TABLE fabrica.media IS 'Media assets (images, videos, documents)';
COMMENT ON TABLE fabrica.media_translation IS 'Translations for media alt text, captions';
COMMENT ON TABLE fabrica.menu IS 'Navigation menu definitions';
COMMENT ON TABLE fabrica.menu_item IS 'Menu items with hierarchical structure';
COMMENT ON TABLE fabrica.menu_item_translation IS 'Translations for menu item labels';
COMMENT ON TABLE fabrica.redirect IS 'URL redirects for SEO';
COMMENT ON TABLE cdc.outbox IS 'Outbox for CDC/event publishing';
COMMENT ON TABLE cdc.outbox_config IS 'Configuration for CDC outbox pattern';
COMMENT ON TABLE cache.cache IS 'Local cache of data from other domains';
COMMENT ON TABLE cache.cache_config IS 'Configuration for consuming events from other domains';
