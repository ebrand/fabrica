-- ============================================================================
-- Migration: Block Structure Redesign
-- From: content_block, content_block_translation, content_content_block
-- To: block, section_type, block_section, variant, block_content, block_content_section_translation
-- ============================================================================

-- ============================================================================
-- STEP 1: Drop old content block tables
-- ============================================================================

-- Drop dependent tables first (respecting foreign keys)
DROP TABLE IF EXISTS fabrica.content_content_block CASCADE;
DROP TABLE IF EXISTS fabrica.content_block_translation CASCADE;
DROP TABLE IF EXISTS fabrica.content_block CASCADE;

-- Drop related triggers
DROP TRIGGER IF EXISTS update_content_block_updated_at ON fabrica.content_block;
DROP TRIGGER IF EXISTS update_content_block_translation_updated_at ON fabrica.content_block_translation;

-- Drop related indexes
DROP INDEX IF EXISTS fabrica.idx_content_block_tenant;
DROP INDEX IF EXISTS fabrica.idx_content_block_code;

-- ============================================================================
-- STEP 2: Create new block structure tables
-- ============================================================================

-- ===================
-- BLOCK (Template Definition)
-- ===================
-- Defines types of content blocks (Article, Card, Modal, etc.)
CREATE TABLE fabrica.block (
    block_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,                   -- Display name: "Article", "Card"
    slug VARCHAR(100) NOT NULL,                   -- Identifier: "article", "card"
    description TEXT,                             -- Admin description
    icon VARCHAR(50),                             -- Icon for UI
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

COMMENT ON TABLE fabrica.block IS 'Block template definitions - defines the structure of a content block';

-- ===================
-- SECTION_TYPE (Field Definitions)
-- ===================
-- Defines types of sections that can appear in blocks (Title, Subtitle, Author, Body, etc.)
CREATE TABLE fabrica.section_type (
    section_type_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    name VARCHAR(100) NOT NULL,                   -- Display name: "Title", "Subtitle"
    slug VARCHAR(100) NOT NULL,                   -- Identifier: "title", "subtitle"
    description TEXT,
    field_type VARCHAR(50) DEFAULT 'text',        -- text, richtext, image, video, date, etc.
    validation_rules JSONB,                       -- Optional validation config
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

COMMENT ON TABLE fabrica.section_type IS 'Section type definitions - defines types of fields that can appear in blocks';

-- ===================
-- BLOCK_SECTION (Block → Section Junction)
-- ===================
-- Defines which sections a block contains
CREATE TABLE fabrica.block_section (
    block_id UUID NOT NULL REFERENCES fabrica.block(block_id) ON DELETE CASCADE,
    section_type_id UUID NOT NULL REFERENCES fabrica.section_type(section_type_id) ON DELETE CASCADE,
    is_required BOOLEAN DEFAULT false,
    display_order INTEGER DEFAULT 0,
    default_value TEXT,                           -- Default content for this section
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (block_id, section_type_id)
);

COMMENT ON TABLE fabrica.block_section IS 'Junction table linking blocks to their allowed section types';

-- ===================
-- VARIANT (Visual Presentation Styles)
-- ===================
-- Defines visual variants for each block (Article→Long Form, Summary; Card→Dark, Hero)
CREATE TABLE fabrica.variant (
    variant_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    block_id UUID NOT NULL REFERENCES fabrica.block(block_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,                   -- Display name: "Long Form", "Hero"
    slug VARCHAR(100) NOT NULL,                   -- Identifier: "long-form", "hero"
    description TEXT,
    preview_image_url VARCHAR(500),               -- Preview image for admin UI
    css_class VARCHAR(100),                       -- Optional CSS class
    settings JSONB,                               -- Variant-specific settings
    is_default BOOLEAN DEFAULT false,             -- Default variant for this block
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(block_id, slug)
);

COMMENT ON TABLE fabrica.variant IS 'Visual presentation styles for each block type';

-- ===================
-- BLOCK_CONTENT (Content Instances)
-- ===================
-- Actual content pieces - instances of blocks
CREATE TABLE fabrica.block_content (
    content_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id VARCHAR(100) NOT NULL,
    block_id UUID NOT NULL REFERENCES fabrica.block(block_id),
    default_variant_id UUID REFERENCES fabrica.variant(variant_id),
    slug VARCHAR(255) NOT NULL,                   -- Unique identifier for lookup
    name VARCHAR(255) NOT NULL,                   -- Admin name for the content
    description TEXT,                             -- Admin description
    access_control VARCHAR(50) DEFAULT 'everyone', -- Access control: everyone, authenticated, roles
    visibility VARCHAR(50) DEFAULT 'public',      -- public, private, scheduled
    publish_at TIMESTAMP,
    unpublish_at TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

COMMENT ON TABLE fabrica.block_content IS 'Content instances - actual content pieces created from block templates';

-- ===================
-- BLOCK_CONTENT_SECTION_TRANSLATION (Translated Section Values)
-- ===================
-- Stores the actual translated content for each section of each content piece
CREATE TABLE fabrica.block_content_section_translation (
    section_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    content_id UUID NOT NULL REFERENCES fabrica.block_content(content_id) ON DELETE CASCADE,
    section_type_id UUID NOT NULL REFERENCES fabrica.section_type(section_type_id),
    language_id UUID NOT NULL REFERENCES fabrica.language(id),
    content TEXT,                                 -- The actual translated content
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(content_id, section_type_id, language_id)
);

COMMENT ON TABLE fabrica.block_content_section_translation IS 'Translated content values for each section of each content piece';

-- ============================================================================
-- STEP 3: Create indexes for performance
-- ============================================================================

-- Block indexes
CREATE INDEX idx_block_tenant ON fabrica.block(tenant_id);
CREATE INDEX idx_block_slug ON fabrica.block(tenant_id, slug);
CREATE INDEX idx_block_active ON fabrica.block(tenant_id, is_active);

-- Section type indexes
CREATE INDEX idx_section_type_tenant ON fabrica.section_type(tenant_id);
CREATE INDEX idx_section_type_slug ON fabrica.section_type(tenant_id, slug);

-- Block section indexes
CREATE INDEX idx_block_section_block ON fabrica.block_section(block_id);
CREATE INDEX idx_block_section_section ON fabrica.block_section(section_type_id);

-- Variant indexes
CREATE INDEX idx_variant_block ON fabrica.variant(block_id);
CREATE INDEX idx_variant_slug ON fabrica.variant(block_id, slug);
CREATE INDEX idx_variant_default ON fabrica.variant(block_id, is_default);

-- Block content indexes
CREATE INDEX idx_block_content_tenant ON fabrica.block_content(tenant_id);
CREATE INDEX idx_block_content_slug ON fabrica.block_content(tenant_id, slug);
CREATE INDEX idx_block_content_block ON fabrica.block_content(block_id);
CREATE INDEX idx_block_content_active ON fabrica.block_content(tenant_id, is_active);

-- Block content section translation indexes
CREATE INDEX idx_bcst_content ON fabrica.block_content_section_translation(content_id);
CREATE INDEX idx_bcst_section ON fabrica.block_content_section_translation(section_type_id);
CREATE INDEX idx_bcst_language ON fabrica.block_content_section_translation(language_id);
CREATE INDEX idx_bcst_lookup ON fabrica.block_content_section_translation(content_id, language_id);

-- ============================================================================
-- STEP 4: Create triggers for updated_at
-- ============================================================================

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

-- ============================================================================
-- STEP 5: Insert seed data
-- ============================================================================

-- Get the en-US language ID for tenant-test
DO $$
DECLARE
    v_en_us_id UUID;
    v_es_mx_id UUID;
    v_article_block_id UUID;
    v_card_block_id UUID;
    v_title_section_id UUID;
    v_subtitle_section_id UUID;
    v_author_section_id UUID;
    v_body_section_id UUID;
    v_cta_text_section_id UUID;
    v_cta_url_section_id UUID;
    v_image_url_section_id UUID;
    v_article_longform_id UUID;
    v_article_summary_id UUID;
    v_card_default_id UUID;
    v_card_hero_id UUID;
    v_card_dark_id UUID;
    v_card_accent_id UUID;
    v_card_minimal_id UUID;
BEGIN
    -- Get language IDs
    SELECT id INTO v_en_us_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_mx_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-MX';

    -- ===================
    -- Insert Section Types
    -- ===================
    INSERT INTO fabrica.section_type (section_type_id, tenant_id, name, slug, description, field_type, display_order)
    VALUES
        (uuid_generate_v4(), 'tenant-test', 'Title', 'title', 'Main heading text', 'text', 1),
        (uuid_generate_v4(), 'tenant-test', 'Subtitle', 'subtitle', 'Secondary heading text', 'text', 2),
        (uuid_generate_v4(), 'tenant-test', 'Author', 'author', 'Author name', 'text', 3),
        (uuid_generate_v4(), 'tenant-test', 'Body', 'body', 'Main content body (supports HTML)', 'richtext', 4),
        (uuid_generate_v4(), 'tenant-test', 'CTA Text', 'cta-text', 'Call-to-action button text', 'text', 5),
        (uuid_generate_v4(), 'tenant-test', 'CTA URL', 'cta-url', 'Call-to-action link URL', 'text', 6),
        (uuid_generate_v4(), 'tenant-test', 'Image URL', 'image-url', 'Image URL for media', 'text', 7),
        (uuid_generate_v4(), 'tenant-test', 'Date', 'date', 'Date field', 'date', 8),
        (uuid_generate_v4(), 'tenant-test', 'Tags', 'tags', 'Comma-separated tags', 'text', 9);

    -- Get section type IDs
    SELECT section_type_id INTO v_title_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'title';
    SELECT section_type_id INTO v_subtitle_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'subtitle';
    SELECT section_type_id INTO v_author_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'author';
    SELECT section_type_id INTO v_body_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'body';
    SELECT section_type_id INTO v_cta_text_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-text';
    SELECT section_type_id INTO v_cta_url_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-url';
    SELECT section_type_id INTO v_image_url_section_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'image-url';

    -- ===================
    -- Insert Blocks
    -- ===================
    INSERT INTO fabrica.block (block_id, tenant_id, name, slug, description, display_order)
    VALUES
        (uuid_generate_v4(), 'tenant-test', 'Article', 'article', 'Full article with title, subtitle, author, and body', 1),
        (uuid_generate_v4(), 'tenant-test', 'Card', 'card', 'Content card with title, body, and optional CTA', 2);

    -- Get block IDs
    SELECT block_id INTO v_article_block_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'article';
    SELECT block_id INTO v_card_block_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'card';

    -- ===================
    -- Link Blocks to Sections
    -- ===================

    -- Article block sections: title (required), subtitle, author, body (required)
    INSERT INTO fabrica.block_section (block_id, section_type_id, is_required, display_order) VALUES
        (v_article_block_id, v_title_section_id, true, 1),
        (v_article_block_id, v_subtitle_section_id, false, 2),
        (v_article_block_id, v_author_section_id, false, 3),
        (v_article_block_id, v_body_section_id, true, 4),
        (v_article_block_id, v_image_url_section_id, false, 5);

    -- Card block sections: title (required), subtitle, body, cta-text, cta-url
    INSERT INTO fabrica.block_section (block_id, section_type_id, is_required, display_order) VALUES
        (v_card_block_id, v_title_section_id, true, 1),
        (v_card_block_id, v_subtitle_section_id, false, 2),
        (v_card_block_id, v_body_section_id, false, 3),
        (v_card_block_id, v_cta_text_section_id, false, 4),
        (v_card_block_id, v_cta_url_section_id, false, 5),
        (v_card_block_id, v_image_url_section_id, false, 6);

    -- ===================
    -- Insert Variants
    -- ===================

    -- Article variants
    INSERT INTO fabrica.variant (variant_id, block_id, name, slug, description, is_default, display_order)
    VALUES
        (uuid_generate_v4(), v_article_block_id, 'Long Form', 'long-form', 'Full article layout with all sections', true, 1),
        (uuid_generate_v4(), v_article_block_id, 'Summary', 'summary', 'Compact article summary', false, 2);

    -- Card variants
    INSERT INTO fabrica.variant (variant_id, block_id, name, slug, description, is_default, display_order)
    VALUES
        (uuid_generate_v4(), v_card_block_id, 'Default', 'default', 'Standard white card', true, 1),
        (uuid_generate_v4(), v_card_block_id, 'Hero', 'hero', 'Gradient hero card with large text', false, 2),
        (uuid_generate_v4(), v_card_block_id, 'Dark', 'dark', 'Dark background card', false, 3),
        (uuid_generate_v4(), v_card_block_id, 'Accent', 'accent', 'Card with accent border', false, 4),
        (uuid_generate_v4(), v_card_block_id, 'Minimal', 'minimal', 'Minimal transparent card', false, 5);

    -- Get variant IDs
    SELECT variant_id INTO v_article_longform_id FROM fabrica.variant WHERE block_id = v_article_block_id AND slug = 'long-form';
    SELECT variant_id INTO v_article_summary_id FROM fabrica.variant WHERE block_id = v_article_block_id AND slug = 'summary';
    SELECT variant_id INTO v_card_default_id FROM fabrica.variant WHERE block_id = v_card_block_id AND slug = 'default';
    SELECT variant_id INTO v_card_hero_id FROM fabrica.variant WHERE block_id = v_card_block_id AND slug = 'hero';
    SELECT variant_id INTO v_card_dark_id FROM fabrica.variant WHERE block_id = v_card_block_id AND slug = 'dark';
    SELECT variant_id INTO v_card_accent_id FROM fabrica.variant WHERE block_id = v_card_block_id AND slug = 'accent';
    SELECT variant_id INTO v_card_minimal_id FROM fabrica.variant WHERE block_id = v_card_block_id AND slug = 'minimal';

    -- ===================
    -- Insert Sample Block Content
    -- ===================

    -- Hero Welcome (Card - Hero variant)
    INSERT INTO fabrica.block_content (content_id, tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES (uuid_generate_v4(), 'tenant-test', v_card_block_id, v_card_hero_id, 'hero-welcome', 'Hero Welcome Banner', 'Main welcome banner for dashboard');

    -- Promo Banner (Card - Accent variant)
    INSERT INTO fabrica.block_content (content_id, tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES (uuid_generate_v4(), 'tenant-test', v_card_block_id, v_card_accent_id, 'promo-banner', 'Promo Banner', 'Promotional banner');

    -- Feature Card (Card - Default variant)
    INSERT INTO fabrica.block_content (content_id, tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES (uuid_generate_v4(), 'tenant-test', v_card_block_id, v_card_default_id, 'feature-card', 'Feature Card', 'Feature highlight card');

    -- Minimal Note (Card - Minimal variant)
    INSERT INTO fabrica.block_content (content_id, tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES (uuid_generate_v4(), 'tenant-test', v_card_block_id, v_card_minimal_id, 'minimal-note', 'Minimal Note', 'Minimal style note');

    -- Dark Section (Card - Dark variant)
    INSERT INTO fabrica.block_content (content_id, tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES (uuid_generate_v4(), 'tenant-test', v_card_block_id, v_card_dark_id, 'dark-section', 'Dark Section', 'Dark themed content section');

    -- Outbox Article (Article - Long Form variant)
    INSERT INTO fabrica.block_content (content_id, tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES (uuid_generate_v4(), 'tenant-test', v_article_block_id, v_article_longform_id, 'outbox-article', 'Outbox Pattern Article', 'Article about the outbox pattern');

    -- ===================
    -- Insert Section Translations for each content
    -- ===================

    -- Hero Welcome - English
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_en_us_id, 'Welcome to Fabrica Commerce'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_subtitle_section_id, v_en_us_id, 'Build Your Dream Store'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_body_section_id, v_en_us_id, '<p>Fabrica Commerce Cloud provides everything you need to launch and grow your online business. Our platform offers <strong>powerful tools</strong>, seamless integrations, and world-class support.</p><p>Start your journey today and join thousands of successful merchants worldwide.</p>'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_cta_text_section_id, v_en_us_id, 'Get Started'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_cta_url_section_id, v_en_us_id, '/signup'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    -- Hero Welcome - Spanish
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_es_mx_id, 'Bienvenido a Fabrica Commerce'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_subtitle_section_id, v_es_mx_id, 'Construye Tu Tienda Ideal'
    FROM fabrica.block_content bc WHERE bc.slug = 'hero-welcome';

    -- Promo Banner - English
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_en_us_id, 'Black Friday Sale!'
    FROM fabrica.block_content bc WHERE bc.slug = 'promo-banner';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_body_section_id, v_en_us_id, '<p>Save up to <strong>50%</strong> on all premium plans. Limited time offer - don''t miss out!</p>'
    FROM fabrica.block_content bc WHERE bc.slug = 'promo-banner';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_cta_text_section_id, v_en_us_id, 'Shop Now'
    FROM fabrica.block_content bc WHERE bc.slug = 'promo-banner';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_cta_url_section_id, v_en_us_id, '/deals'
    FROM fabrica.block_content bc WHERE bc.slug = 'promo-banner';

    -- Feature Card - English
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_en_us_id, 'Easy Product Management'
    FROM fabrica.block_content bc WHERE bc.slug = 'feature-card';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_body_section_id, v_en_us_id, '<p>Manage your entire product catalog with our intuitive interface. Import, export, and bulk edit with ease.</p>'
    FROM fabrica.block_content bc WHERE bc.slug = 'feature-card';

    -- Minimal Note - English
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_en_us_id, 'Did you know?'
    FROM fabrica.block_content bc WHERE bc.slug = 'minimal-note';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_body_section_id, v_en_us_id, '<p>You can customize every aspect of your store using our theme editor. No coding required!</p>'
    FROM fabrica.block_content bc WHERE bc.slug = 'minimal-note';

    -- Dark Section - English
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_en_us_id, 'Join Our Community'
    FROM fabrica.block_content bc WHERE bc.slug = 'dark-section';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_body_section_id, v_en_us_id, '<p>Connect with thousands of merchants, share experiences, and learn from the best. Our community is here to help you succeed.</p>'
    FROM fabrica.block_content bc WHERE bc.slug = 'dark-section';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_cta_text_section_id, v_en_us_id, 'Join Now'
    FROM fabrica.block_content bc WHERE bc.slug = 'dark-section';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_cta_url_section_id, v_en_us_id, '/community'
    FROM fabrica.block_content bc WHERE bc.slug = 'dark-section';

    -- Outbox Article - English
    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_title_section_id, v_en_us_id, 'The Outbox Pattern'
    FROM fabrica.block_content bc WHERE bc.slug = 'outbox-article';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_subtitle_section_id, v_en_us_id, 'Why it still slaps!'
    FROM fabrica.block_content bc WHERE bc.slug = 'outbox-article';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_author_section_id, v_en_us_id, 'Eric Brand'
    FROM fabrica.block_content bc WHERE bc.slug = 'outbox-article';

    INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content)
    SELECT bc.content_id, v_body_section_id, v_en_us_id, '<p>The outbox pattern is a reliable way to ensure data consistency across distributed systems. By writing events to an outbox table in the same transaction as your domain data, you guarantee that events will eventually be published.</p><p>This pattern is especially useful in microservices architectures where you need to maintain consistency without distributed transactions.</p>'
    FROM fabrica.block_content bc WHERE bc.slug = 'outbox-article';

    RAISE NOTICE 'Block structure seed data inserted successfully';
END $$;

-- ============================================================================
-- STEP 6: Update outbox config for new tables
-- ============================================================================

-- Remove old content_block config if exists
DELETE FROM cdc.outbox_config WHERE table_name = 'content_block';

-- Add new block_content config
INSERT INTO cdc.outbox_config (schema_name, table_name, domain_name, topic_name, capture_insert, capture_update, capture_delete, is_active, description)
VALUES ('fabrica', 'block_content', 'content', 'content.block_content', true, true, true, true, 'Capture all block content entity changes')
ON CONFLICT (schema_name, table_name) DO NOTHING;

-- ============================================================================
-- Migration Complete
-- ============================================================================
