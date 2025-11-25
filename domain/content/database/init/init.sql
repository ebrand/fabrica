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
-- CONTENT TYPES & CONTENT
-- ===================

-- Content types table (page, blog_post, article, faq, banner, etc.)
CREATE TABLE fabrica.content_type (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL,                -- e.g., "page", "blog_post", "faq"
    name VARCHAR(100) NOT NULL,               -- e.g., "Page", "Blog Post"
    description TEXT,
    icon VARCHAR(50),                         -- Icon identifier for UI
    has_slug BOOLEAN DEFAULT true,
    has_featured_image BOOLEAN DEFAULT true,
    has_excerpt BOOLEAN DEFAULT true,
    has_body BOOLEAN DEFAULT true,
    has_seo BOOLEAN DEFAULT true,
    has_categories BOOLEAN DEFAULT true,
    has_tags BOOLEAN DEFAULT true,
    has_author BOOLEAN DEFAULT false,
    has_publish_date BOOLEAN DEFAULT false,
    is_hierarchical BOOLEAN DEFAULT false,    -- Can have parent/child (like pages)
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    custom_fields JSONB,                      -- Schema for custom fields specific to this type
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, code)
);

-- Main content table
CREATE TABLE fabrica.content (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    content_type_id UUID NOT NULL REFERENCES fabrica.content_type(id),
    parent_id UUID REFERENCES fabrica.content(id),  -- For hierarchical content
    slug VARCHAR(500) NOT NULL,
    author_id UUID,                           -- Reference to user in admin domain
    featured_image_id UUID,                   -- Reference to media
    status VARCHAR(50) DEFAULT 'draft',       -- draft, pending_review, published, archived
    visibility VARCHAR(50) DEFAULT 'public',  -- public, private, password_protected
    password_hash VARCHAR(255),               -- For password-protected content
    publish_at TIMESTAMP,
    unpublish_at TIMESTAMP,
    published_at TIMESTAMP,
    view_count INTEGER DEFAULT 0,
    is_featured BOOLEAN DEFAULT false,
    is_pinned BOOLEAN DEFAULT false,
    display_order INTEGER DEFAULT 0,
    custom_data JSONB,                        -- Custom fields data
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Content translations table
-- Stores translations for all translatable fields on content
CREATE TABLE fabrica.content_translation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_id UUID NOT NULL REFERENCES fabrica.content(id) ON DELETE CASCADE,
    locale_code VARCHAR(10) NOT NULL,
    title VARCHAR(500) NOT NULL,
    excerpt TEXT,                             -- Short description/teaser
    body TEXT,                                -- Main content (HTML or markdown)
    seo_title VARCHAR(255),                   -- Meta title for SEO
    seo_description TEXT,                     -- Meta description for SEO
    seo_keywords TEXT,                        -- Meta keywords
    og_title VARCHAR(255),                    -- Open Graph title
    og_description TEXT,                      -- Open Graph description
    og_image_url VARCHAR(500),                -- Open Graph image
    translation_status VARCHAR(50) DEFAULT 'draft', -- draft, in_review, published
    translator_id UUID,                       -- Reference to user who translated
    reviewed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(content_id, locale_code)
);

-- ===================
-- CONTENT BLOCKS (Reusable Sections)
-- ===================

-- Content blocks table (for reusable content snippets, widgets, etc.)
CREATE TABLE fabrica.content_block (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL,               -- Unique identifier for the block
    block_type VARCHAR(50) NOT NULL,          -- hero, feature_list, cta, testimonial, etc.
    settings JSONB,                           -- Block-specific settings (layout, colors, etc.)
    is_global BOOLEAN DEFAULT false,          -- Available across all content
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, code)
);

-- Content block translations
CREATE TABLE fabrica.content_block_translation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_block_id UUID NOT NULL REFERENCES fabrica.content_block(id) ON DELETE CASCADE,
    locale_code VARCHAR(10) NOT NULL,
    title VARCHAR(255),
    subtitle VARCHAR(500),
    body TEXT,
    cta_text VARCHAR(100),                    -- Call-to-action button text
    cta_url VARCHAR(500),
    additional_data JSONB,                    -- Any additional translatable data
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(content_block_id, locale_code)
);

-- Junction table: Content to Content Blocks
CREATE TABLE fabrica.content_content_block (
    content_id UUID NOT NULL REFERENCES fabrica.content(id) ON DELETE CASCADE,
    content_block_id UUID NOT NULL REFERENCES fabrica.content_block(id) ON DELETE CASCADE,
    position VARCHAR(50) DEFAULT 'body',      -- header, body, sidebar, footer
    display_order INTEGER DEFAULT 0,
    settings_override JSONB,                  -- Override block settings for this content
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, content_block_id, position)
);

-- ===================
-- CATEGORIES & TAGS
-- ===================

-- Content categories (hierarchical)
CREATE TABLE fabrica.content_category (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES fabrica.content_category(id),
    slug VARCHAR(255) NOT NULL,
    image_url VARCHAR(500),
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Content category translations
CREATE TABLE fabrica.content_category_translation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_category_id UUID NOT NULL REFERENCES fabrica.content_category(id) ON DELETE CASCADE,
    locale_code VARCHAR(10) NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    seo_title VARCHAR(255),
    seo_description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(content_category_id, locale_code)
);

-- Junction table: Content to Categories
CREATE TABLE fabrica.content_category_mapping (
    content_id UUID NOT NULL REFERENCES fabrica.content(id) ON DELETE CASCADE,
    content_category_id UUID NOT NULL REFERENCES fabrica.content_category(id) ON DELETE CASCADE,
    is_primary BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, content_category_id)
);

-- Content tags
CREATE TABLE fabrica.content_tag (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Content tag translations
CREATE TABLE fabrica.content_tag_translation (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_tag_id UUID NOT NULL REFERENCES fabrica.content_tag(id) ON DELETE CASCADE,
    locale_code VARCHAR(10) NOT NULL,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(content_tag_id, locale_code)
);

-- Junction table: Content to Tags
CREATE TABLE fabrica.content_tag_mapping (
    content_id UUID NOT NULL REFERENCES fabrica.content(id) ON DELETE CASCADE,
    content_tag_id UUID NOT NULL REFERENCES fabrica.content_tag(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, content_tag_id)
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
    link_type VARCHAR(50) NOT NULL,           -- content, url, category, placeholder
    content_id UUID REFERENCES fabrica.content(id),
    category_id UUID REFERENCES fabrica.content_category(id),
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
    aggregate_type VARCHAR(100) NOT NULL,     -- 'content', 'media', etc.
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,         -- 'content.created', 'content.updated', etc.
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

-- Content type indexes
CREATE INDEX idx_content_type_tenant ON fabrica.content_type(tenant_id);
CREATE INDEX idx_content_type_code ON fabrica.content_type(tenant_id, code);

-- Content indexes
CREATE INDEX idx_content_tenant ON fabrica.content(tenant_id);
CREATE INDEX idx_content_slug ON fabrica.content(tenant_id, slug);
CREATE INDEX idx_content_type ON fabrica.content(content_type_id);
CREATE INDEX idx_content_status ON fabrica.content(status);
CREATE INDEX idx_content_parent ON fabrica.content(parent_id);
CREATE INDEX idx_content_publish ON fabrica.content(tenant_id, status, publish_at);

-- Content translation indexes
CREATE INDEX idx_content_translation_content ON fabrica.content_translation(content_id);
CREATE INDEX idx_content_translation_locale ON fabrica.content_translation(locale_code);

-- Content block indexes
CREATE INDEX idx_content_block_tenant ON fabrica.content_block(tenant_id);
CREATE INDEX idx_content_block_code ON fabrica.content_block(tenant_id, code);

-- Category indexes
CREATE INDEX idx_content_category_tenant ON fabrica.content_category(tenant_id);
CREATE INDEX idx_content_category_parent ON fabrica.content_category(parent_id);

-- Tag indexes
CREATE INDEX idx_content_tag_tenant ON fabrica.content_tag(tenant_id);

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

CREATE TRIGGER update_content_type_updated_at BEFORE UPDATE ON fabrica.content_type
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_updated_at BEFORE UPDATE ON fabrica.content
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_translation_updated_at BEFORE UPDATE ON fabrica.content_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_block_updated_at BEFORE UPDATE ON fabrica.content_block
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_block_translation_updated_at BEFORE UPDATE ON fabrica.content_block_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_category_updated_at BEFORE UPDATE ON fabrica.content_category
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_category_translation_updated_at BEFORE UPDATE ON fabrica.content_category_translation
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_content_tag_translation_updated_at BEFORE UPDATE ON fabrica.content_tag_translation
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
('tenant-test', 'es-MX', 'es', 'Spanish (Mexico)', 'Espanol (Mexico)', false, 'ltr'),
('tenant-test', 'fr-FR', 'fr', 'French (France)', 'Francais', false, 'ltr'),
('tenant-test', 'de-DE', 'de', 'German (Germany)', 'Deutsch', false, 'ltr'),
('tenant-test', 'ar-SA', 'ar', 'Arabic (Saudi Arabia)', 'Arabic', false, 'rtl');

-- Insert sample content types
INSERT INTO fabrica.content_type (tenant_id, code, name, description, has_author, has_publish_date, is_hierarchical) VALUES
('tenant-test', 'page', 'Page', 'Standard web page', false, false, true),
('tenant-test', 'blog_post', 'Blog Post', 'Blog article with author and publish date', true, true, false),
('tenant-test', 'faq', 'FAQ', 'Frequently asked question', false, false, false),
('tenant-test', 'banner', 'Banner', 'Promotional banner for homepage or campaigns', false, true, false);

-- Insert sample categories
INSERT INTO fabrica.content_category (tenant_id, slug, display_order) VALUES
('tenant-test', 'news', 1),
('tenant-test', 'tutorials', 2),
('tenant-test', 'company', 3);

-- Insert category translations
INSERT INTO fabrica.content_category_translation (content_category_id, locale_code, name, description)
SELECT id, 'en-US', 'News', 'Latest news and announcements' FROM fabrica.content_category WHERE slug = 'news';
INSERT INTO fabrica.content_category_translation (content_category_id, locale_code, name, description)
SELECT id, 'es-MX', 'Noticias', 'Ultimas noticias y anuncios' FROM fabrica.content_category WHERE slug = 'news';

INSERT INTO fabrica.content_category_translation (content_category_id, locale_code, name, description)
SELECT id, 'en-US', 'Tutorials', 'How-to guides and tutorials' FROM fabrica.content_category WHERE slug = 'tutorials';
INSERT INTO fabrica.content_category_translation (content_category_id, locale_code, name, description)
SELECT id, 'es-MX', 'Tutoriales', 'Guias y tutoriales' FROM fabrica.content_category WHERE slug = 'tutorials';

INSERT INTO fabrica.content_category_translation (content_category_id, locale_code, name, description)
SELECT id, 'en-US', 'Company', 'About our company' FROM fabrica.content_category WHERE slug = 'company';
INSERT INTO fabrica.content_category_translation (content_category_id, locale_code, name, description)
SELECT id, 'es-MX', 'Empresa', 'Acerca de nuestra empresa' FROM fabrica.content_category WHERE slug = 'company';

-- Insert sample content
INSERT INTO fabrica.content (tenant_id, content_type_id, slug, status, visibility, is_featured)
SELECT 'tenant-test', id, 'about-us', 'published', 'public', false
FROM fabrica.content_type WHERE code = 'page' AND tenant_id = 'tenant-test';

INSERT INTO fabrica.content (tenant_id, content_type_id, slug, status, visibility, is_featured)
SELECT 'tenant-test', id, 'welcome-to-fabrica', 'published', 'public', true
FROM fabrica.content_type WHERE code = 'blog_post' AND tenant_id = 'tenant-test';

-- Insert content translations
INSERT INTO fabrica.content_translation (content_id, locale_code, title, excerpt, body, seo_title, seo_description, translation_status)
SELECT id, 'en-US', 'About Us', 'Learn more about our company and mission.',
'<h1>About Fabrica Commerce Cloud</h1><p>We are a leading e-commerce platform...</p>',
'About Us | Fabrica Commerce', 'Learn about Fabrica Commerce Cloud, our mission, and our team.', 'published'
FROM fabrica.content WHERE slug = 'about-us';

INSERT INTO fabrica.content_translation (content_id, locale_code, title, excerpt, body, seo_title, seo_description, translation_status)
SELECT id, 'es-MX', 'Acerca de Nosotros', 'Conoce mas sobre nuestra empresa y mision.',
'<h1>Acerca de Fabrica Commerce Cloud</h1><p>Somos una plataforma de comercio electronico lider...</p>',
'Acerca de Nosotros | Fabrica Commerce', 'Conoce Fabrica Commerce Cloud, nuestra mision y nuestro equipo.', 'published'
FROM fabrica.content WHERE slug = 'about-us';

INSERT INTO fabrica.content_translation (content_id, locale_code, title, excerpt, body, seo_title, seo_description, translation_status)
SELECT id, 'en-US', 'Welcome to Fabrica Commerce Cloud', 'Introducing our new e-commerce platform.',
'<h1>Welcome!</h1><p>We are excited to announce the launch of Fabrica Commerce Cloud...</p>',
'Welcome to Fabrica | Fabrica Commerce', 'Learn about the launch of Fabrica Commerce Cloud.', 'published'
FROM fabrica.content WHERE slug = 'welcome-to-fabrica';

INSERT INTO fabrica.content_translation (content_id, locale_code, title, excerpt, body, seo_title, seo_description, translation_status)
SELECT id, 'es-MX', 'Bienvenido a Fabrica Commerce Cloud', 'Presentamos nuestra nueva plataforma de comercio electronico.',
'<h1>Bienvenido!</h1><p>Estamos emocionados de anunciar el lanzamiento de Fabrica Commerce Cloud...</p>',
'Bienvenido a Fabrica | Fabrica Commerce', 'Conoce el lanzamiento de Fabrica Commerce Cloud.', 'published'
FROM fabrica.content WHERE slug = 'welcome-to-fabrica';

-- Insert sample menu
INSERT INTO fabrica.menu (tenant_id, code, name, location) VALUES
('tenant-test', 'main', 'Main Navigation', 'header'),
('tenant-test', 'footer', 'Footer Navigation', 'footer');

-- Insert default outbox config for content table
INSERT INTO cdc.outbox_config (schema_name, table_name, domain_name, topic_name, capture_insert, capture_update, capture_delete, is_active, description)
VALUES ('fabrica', 'content', 'content', 'content.content', true, true, true, true, 'Capture all content entity changes');

INSERT INTO cdc.outbox_config (schema_name, table_name, domain_name, topic_name, capture_insert, capture_update, capture_delete, is_active, description)
VALUES ('fabrica', 'media', 'content', 'content.media', true, true, true, true, 'Capture all media entity changes');

-- Insert example cache config: Content domain listens for product events (for product-related content)
INSERT INTO cache.cache_config (source_domain, source_schema, source_table, consumer_group, listen_create, listen_update, listen_delete, is_active, description)
VALUES ('product', 'fabrica', 'product', 'content-product.product', true, true, true, true, 'Cache product data for content linking');

-- ============================================================================
-- Comments
-- ============================================================================

COMMENT ON TABLE fabrica.language IS 'Supported languages/locales for multi-language content';
COMMENT ON TABLE fabrica.content_type IS 'Types of content (page, blog, FAQ, etc.)';
COMMENT ON TABLE fabrica.content IS 'Main content entities';
COMMENT ON TABLE fabrica.content_translation IS 'Translations for content fields by locale';
COMMENT ON TABLE fabrica.content_block IS 'Reusable content blocks/widgets';
COMMENT ON TABLE fabrica.content_block_translation IS 'Translations for content block fields';
COMMENT ON TABLE fabrica.content_category IS 'Content categories (hierarchical)';
COMMENT ON TABLE fabrica.content_category_translation IS 'Translations for category names/descriptions';
COMMENT ON TABLE fabrica.content_tag IS 'Content tags';
COMMENT ON TABLE fabrica.content_tag_translation IS 'Translations for tag names';
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
