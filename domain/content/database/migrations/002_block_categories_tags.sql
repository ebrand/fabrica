-- Migration: 002_block_categories_tags.sql
-- Description: Remove old content_* tables and create block_category/block_tag tables
-- Date: 2024-11-25

BEGIN;

-- ============================================================================
-- DROP OLD CONTENT TABLES
-- ============================================================================

-- Drop junction tables first (they have foreign keys)
DROP TABLE IF EXISTS fabrica.content_category_mapping CASCADE;
DROP TABLE IF EXISTS fabrica.content_tag_mapping CASCADE;

-- Drop translation tables
DROP TABLE IF EXISTS fabrica.content_translation CASCADE;
DROP TABLE IF EXISTS fabrica.content_category_translation CASCADE;
DROP TABLE IF EXISTS fabrica.content_tag_translation CASCADE;

-- Drop main tables
DROP TABLE IF EXISTS fabrica.content CASCADE;
DROP TABLE IF EXISTS fabrica.content_category CASCADE;
DROP TABLE IF EXISTS fabrica.content_tag CASCADE;
DROP TABLE IF EXISTS fabrica.content_type CASCADE;

-- ============================================================================
-- CREATE BLOCK CATEGORY TABLES
-- ============================================================================

-- Block Category - categories that can be applied to block_content
CREATE TABLE IF NOT EXISTS fabrica.block_category (
    category_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES fabrica.block_category(category_id) ON DELETE RESTRICT,
    slug VARCHAR(100) NOT NULL,
    icon VARCHAR(50),
    color VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    display_order INT DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Block Category Translations
CREATE TABLE IF NOT EXISTS fabrica.block_category_translation (
    translation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id UUID NOT NULL REFERENCES fabrica.block_category(category_id) ON DELETE CASCADE,
    language_id UUID NOT NULL REFERENCES fabrica.language(id) ON DELETE RESTRICT,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(category_id, language_id)
);

-- Junction: Block Content to Category (many-to-many)
CREATE TABLE IF NOT EXISTS fabrica.block_content_category (
    content_id UUID NOT NULL REFERENCES fabrica.block_content(content_id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES fabrica.block_category(category_id) ON DELETE CASCADE,
    is_primary BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, category_id)
);

-- ============================================================================
-- CREATE BLOCK TAG TABLES
-- ============================================================================

-- Block Tag - tags that can be applied to block_content
CREATE TABLE IF NOT EXISTS fabrica.block_tag (
    tag_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id VARCHAR(100) NOT NULL,
    slug VARCHAR(100) NOT NULL,
    color VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tenant_id, slug)
);

-- Block Tag Translations
CREATE TABLE IF NOT EXISTS fabrica.block_tag_translation (
    translation_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tag_id UUID NOT NULL REFERENCES fabrica.block_tag(tag_id) ON DELETE CASCADE,
    language_id UUID NOT NULL REFERENCES fabrica.language(id) ON DELETE RESTRICT,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(tag_id, language_id)
);

-- Junction: Block Content to Tag (many-to-many)
CREATE TABLE IF NOT EXISTS fabrica.block_content_tag (
    content_id UUID NOT NULL REFERENCES fabrica.block_content(content_id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES fabrica.block_tag(tag_id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (content_id, tag_id)
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Block Category indexes
CREATE INDEX IF NOT EXISTS idx_block_category_tenant ON fabrica.block_category(tenant_id);
CREATE INDEX IF NOT EXISTS idx_block_category_parent ON fabrica.block_category(parent_id);
CREATE INDEX IF NOT EXISTS idx_block_category_translation_category ON fabrica.block_category_translation(category_id);
CREATE INDEX IF NOT EXISTS idx_block_content_category_content ON fabrica.block_content_category(content_id);
CREATE INDEX IF NOT EXISTS idx_block_content_category_category ON fabrica.block_content_category(category_id);

-- Block Tag indexes
CREATE INDEX IF NOT EXISTS idx_block_tag_tenant ON fabrica.block_tag(tenant_id);
CREATE INDEX IF NOT EXISTS idx_block_tag_translation_tag ON fabrica.block_tag_translation(tag_id);
CREATE INDEX IF NOT EXISTS idx_block_content_tag_content ON fabrica.block_content_tag(content_id);
CREATE INDEX IF NOT EXISTS idx_block_content_tag_tag ON fabrica.block_content_tag(tag_id);

-- ============================================================================
-- TRIGGERS FOR updated_at
-- ============================================================================

CREATE TRIGGER update_block_category_updated_at
    BEFORE UPDATE ON fabrica.block_category
    FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

CREATE TRIGGER update_block_category_translation_updated_at
    BEFORE UPDATE ON fabrica.block_category_translation
    FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

CREATE TRIGGER update_block_tag_updated_at
    BEFORE UPDATE ON fabrica.block_tag
    FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

CREATE TRIGGER update_block_tag_translation_updated_at
    BEFORE UPDATE ON fabrica.block_tag_translation
    FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

-- ============================================================================
-- SEED DATA
-- ============================================================================

-- Get the English language ID for translations
DO $$
DECLARE
    v_en_lang_id UUID;
    v_es_lang_id UUID;
    v_cat_news_id UUID;
    v_cat_tutorials_id UUID;
    v_cat_announcements_id UUID;
    v_tag_featured_id UUID;
    v_tag_new_id UUID;
    v_tag_popular_id UUID;
    v_hero_content_id UUID;
BEGIN
    -- Get language IDs
    SELECT id INTO v_en_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-ES';

    -- Create Categories
    INSERT INTO fabrica.block_category (category_id, tenant_id, slug, icon, color, display_order)
    VALUES
        (gen_random_uuid(), 'tenant-test', 'news', 'newspaper', '#3B82F6', 1),
        (gen_random_uuid(), 'tenant-test', 'tutorials', 'academic-cap', '#10B981', 2),
        (gen_random_uuid(), 'tenant-test', 'announcements', 'megaphone', '#F59E0B', 3)
    ON CONFLICT (tenant_id, slug) DO NOTHING;

    -- Get category IDs
    SELECT category_id INTO v_cat_news_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'news';
    SELECT category_id INTO v_cat_tutorials_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'tutorials';
    SELECT category_id INTO v_cat_announcements_id FROM fabrica.block_category WHERE tenant_id = 'tenant-test' AND slug = 'announcements';

    -- Category Translations (English)
    INSERT INTO fabrica.block_category_translation (category_id, language_id, name, description)
    VALUES
        (v_cat_news_id, v_en_lang_id, 'News', 'Latest news and updates'),
        (v_cat_tutorials_id, v_en_lang_id, 'Tutorials', 'How-to guides and tutorials'),
        (v_cat_announcements_id, v_en_lang_id, 'Announcements', 'Important announcements')
    ON CONFLICT (category_id, language_id) DO NOTHING;

    -- Category Translations (Spanish)
    IF v_es_lang_id IS NOT NULL THEN
        INSERT INTO fabrica.block_category_translation (category_id, language_id, name, description)
        VALUES
            (v_cat_news_id, v_es_lang_id, 'Noticias', 'Ultimas noticias y actualizaciones'),
            (v_cat_tutorials_id, v_es_lang_id, 'Tutoriales', 'Guias y tutoriales'),
            (v_cat_announcements_id, v_es_lang_id, 'Anuncios', 'Anuncios importantes')
        ON CONFLICT (category_id, language_id) DO NOTHING;
    END IF;

    -- Create Tags
    INSERT INTO fabrica.block_tag (tag_id, tenant_id, slug, color)
    VALUES
        (gen_random_uuid(), 'tenant-test', 'featured', '#EF4444'),
        (gen_random_uuid(), 'tenant-test', 'new', '#22C55E'),
        (gen_random_uuid(), 'tenant-test', 'popular', '#8B5CF6')
    ON CONFLICT (tenant_id, slug) DO NOTHING;

    -- Get tag IDs
    SELECT tag_id INTO v_tag_featured_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'featured';
    SELECT tag_id INTO v_tag_new_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'new';
    SELECT tag_id INTO v_tag_popular_id FROM fabrica.block_tag WHERE tenant_id = 'tenant-test' AND slug = 'popular';

    -- Tag Translations (English)
    INSERT INTO fabrica.block_tag_translation (tag_id, language_id, name)
    VALUES
        (v_tag_featured_id, v_en_lang_id, 'Featured'),
        (v_tag_new_id, v_en_lang_id, 'New'),
        (v_tag_popular_id, v_en_lang_id, 'Popular')
    ON CONFLICT (tag_id, language_id) DO NOTHING;

    -- Tag Translations (Spanish)
    IF v_es_lang_id IS NOT NULL THEN
        INSERT INTO fabrica.block_tag_translation (tag_id, language_id, name)
        VALUES
            (v_tag_featured_id, v_es_lang_id, 'Destacado'),
            (v_tag_new_id, v_es_lang_id, 'Nuevo'),
            (v_tag_popular_id, v_es_lang_id, 'Popular')
        ON CONFLICT (tag_id, language_id) DO NOTHING;
    END IF;

    -- Link hero-welcome content to categories and tags
    SELECT content_id INTO v_hero_content_id FROM fabrica.block_content WHERE tenant_id = 'tenant-test' AND slug = 'hero-welcome';

    IF v_hero_content_id IS NOT NULL THEN
        INSERT INTO fabrica.block_content_category (content_id, category_id, is_primary)
        VALUES (v_hero_content_id, v_cat_announcements_id, true)
        ON CONFLICT (content_id, category_id) DO NOTHING;

        INSERT INTO fabrica.block_content_tag (content_id, tag_id)
        VALUES
            (v_hero_content_id, v_tag_featured_id),
            (v_hero_content_id, v_tag_new_id)
        ON CONFLICT (content_id, tag_id) DO NOTHING;
    END IF;
END $$;

-- ============================================================================
-- UPDATE OUTBOX CONFIG
-- ============================================================================

-- Update outbox config for new tables
INSERT INTO cdc.outbox_config (schema_name, table_name, topic_name, domain_name, is_active)
VALUES
    ('fabrica', 'block_category', 'content.block_category', 'content', true),
    ('fabrica', 'block_tag', 'content.block_tag', 'content', true)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- TABLE COMMENTS
-- ============================================================================

COMMENT ON TABLE fabrica.block_category IS 'Categories for organizing block content';
COMMENT ON TABLE fabrica.block_category_translation IS 'Translated names and descriptions for categories';
COMMENT ON TABLE fabrica.block_content_category IS 'Junction table linking block content to categories';
COMMENT ON TABLE fabrica.block_tag IS 'Tags for labeling block content';
COMMENT ON TABLE fabrica.block_tag_translation IS 'Translated names for tags';
COMMENT ON TABLE fabrica.block_content_tag IS 'Junction table linking block content to tags';

COMMIT;
