-- Migration: 003_dashboard_content.sql
-- Description: Add minimal/accent card variants and dashboard content blocks
-- Date: 2024-11-25

BEGIN;

-- ============================================================================
-- ADD NEW CARD VARIANTS
-- ============================================================================

DO $$
DECLARE
    v_card_id UUID;
BEGIN
    SELECT block_id INTO v_card_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'card';

    -- Add minimal variant
    INSERT INTO fabrica.variant (block_id, name, slug, description, css_class, is_default, display_order)
    VALUES (v_card_id, 'Minimal', 'minimal', 'Clean minimal card style', 'card-minimal', false, 4)
    ON CONFLICT DO NOTHING;

    -- Add accent variant
    INSERT INTO fabrica.variant (block_id, name, slug, description, css_class, is_default, display_order)
    VALUES (v_card_id, 'Accent', 'accent', 'Accented sidebar card', 'card-accent', false, 5)
    ON CONFLICT DO NOTHING;
END $$;

-- ============================================================================
-- CREATE DASHBOARD CONTENT BLOCKS
-- ============================================================================

-- 1. "welcome" block with hero variant
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

    -- Create welcome content
    INSERT INTO fabrica.block_content (tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES ('tenant-test', v_card_id, v_hero_variant_id, 'welcome', 'Welcome Hero', 'Dashboard welcome hero banner')
    ON CONFLICT (tenant_id, slug) DO NOTHING
    RETURNING content_id INTO v_content_id;

    -- Only insert translations if we created a new record
    IF v_content_id IS NOT NULL THEN
        -- English translations
        INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
        (v_content_id, v_title_id, v_en_lang_id, 'Welcome to Your Dashboard'),
        (v_content_id, v_subtitle_id, v_en_lang_id, 'Manage Your Business'),
        (v_content_id, v_body_id, v_en_lang_id, '<p>Access all your tools and analytics in one place. Monitor sales, manage inventory, and track customer engagement from this central hub.</p>'),
        (v_content_id, v_cta_text_id, v_en_lang_id, 'View Analytics'),
        (v_content_id, v_cta_url_id, v_en_lang_id, '/analytics');

        -- Spanish translations
        IF v_es_lang_id IS NOT NULL THEN
            INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
            (v_content_id, v_title_id, v_es_lang_id, 'Bienvenido a tu Panel'),
            (v_content_id, v_subtitle_id, v_es_lang_id, 'Gestiona tu Negocio'),
            (v_content_id, v_body_id, v_es_lang_id, '<p>Accede a todas tus herramientas y analiticas en un solo lugar. Monitorea ventas, gestiona inventario y rastrea la interaccion con clientes desde este centro.</p>'),
            (v_content_id, v_cta_text_id, v_es_lang_id, 'Ver Analiticas'),
            (v_content_id, v_cta_url_id, v_es_lang_id, '/analiticas');
        END IF;
    END IF;
END $$;

-- 2. "note-01" block with minimal variant
DO $$
DECLARE
    v_card_id UUID;
    v_minimal_variant_id UUID;
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
    SELECT variant_id INTO v_minimal_variant_id FROM fabrica.variant WHERE block_id = v_card_id AND slug = 'minimal';
    SELECT id INTO v_en_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-ES';

    SELECT section_type_id INTO v_title_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'title';
    SELECT section_type_id INTO v_subtitle_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'subtitle';
    SELECT section_type_id INTO v_body_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'body';
    SELECT section_type_id INTO v_cta_text_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-text';
    SELECT section_type_id INTO v_cta_url_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-url';

    -- Create note-01 content
    INSERT INTO fabrica.block_content (tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES ('tenant-test', v_card_id, v_minimal_variant_id, 'note-01', 'Quick Note', 'Minimal style note card')
    ON CONFLICT (tenant_id, slug) DO NOTHING
    RETURNING content_id INTO v_content_id;

    IF v_content_id IS NOT NULL THEN
        -- English translations
        INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
        (v_content_id, v_title_id, v_en_lang_id, 'Quick Tip'),
        (v_content_id, v_subtitle_id, v_en_lang_id, 'Productivity Boost'),
        (v_content_id, v_body_id, v_en_lang_id, '<p>Use keyboard shortcuts to navigate faster. Press <code>Ctrl+K</code> to open the command palette and search for any action or page.</p>'),
        (v_content_id, v_cta_text_id, v_en_lang_id, 'Learn More'),
        (v_content_id, v_cta_url_id, v_en_lang_id, '/help/shortcuts');

        -- Spanish translations
        IF v_es_lang_id IS NOT NULL THEN
            INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
            (v_content_id, v_title_id, v_es_lang_id, 'Consejo Rapido'),
            (v_content_id, v_subtitle_id, v_es_lang_id, 'Mejora tu Productividad'),
            (v_content_id, v_body_id, v_es_lang_id, '<p>Usa atajos de teclado para navegar mas rapido. Presiona <code>Ctrl+K</code> para abrir la paleta de comandos.</p>'),
            (v_content_id, v_cta_text_id, v_es_lang_id, 'Saber Mas'),
            (v_content_id, v_cta_url_id, v_es_lang_id, '/ayuda/atajos');
        END IF;
    END IF;
END $$;

-- 3. "promo-banner" block with accent variant
DO $$
DECLARE
    v_card_id UUID;
    v_accent_variant_id UUID;
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
    SELECT variant_id INTO v_accent_variant_id FROM fabrica.variant WHERE block_id = v_card_id AND slug = 'accent';
    SELECT id INTO v_en_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'en-US';
    SELECT id INTO v_es_lang_id FROM fabrica.language WHERE tenant_id = 'tenant-test' AND locale_code = 'es-ES';

    SELECT section_type_id INTO v_title_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'title';
    SELECT section_type_id INTO v_subtitle_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'subtitle';
    SELECT section_type_id INTO v_body_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'body';
    SELECT section_type_id INTO v_cta_text_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-text';
    SELECT section_type_id INTO v_cta_url_id FROM fabrica.section_type WHERE tenant_id = 'tenant-test' AND slug = 'cta-url';

    -- Create promo-banner content
    INSERT INTO fabrica.block_content (tenant_id, block_id, default_variant_id, slug, name, description)
    VALUES ('tenant-test', v_card_id, v_accent_variant_id, 'promo-banner', 'Promo Banner', 'Promotional accent banner')
    ON CONFLICT (tenant_id, slug) DO NOTHING
    RETURNING content_id INTO v_content_id;

    IF v_content_id IS NOT NULL THEN
        -- English translations
        INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
        (v_content_id, v_title_id, v_en_lang_id, 'New Features Available'),
        (v_content_id, v_subtitle_id, v_en_lang_id, 'November 2024 Update'),
        (v_content_id, v_body_id, v_en_lang_id, '<p>Check out the latest features including advanced reporting, bulk operations, and improved performance. Your workflow just got better!</p>'),
        (v_content_id, v_cta_text_id, v_en_lang_id, 'See What''s New'),
        (v_content_id, v_cta_url_id, v_en_lang_id, '/changelog');

        -- Spanish translations
        IF v_es_lang_id IS NOT NULL THEN
            INSERT INTO fabrica.block_content_section_translation (content_id, section_type_id, language_id, content) VALUES
            (v_content_id, v_title_id, v_es_lang_id, 'Nuevas Funciones Disponibles'),
            (v_content_id, v_subtitle_id, v_es_lang_id, 'Actualizacion Noviembre 2024'),
            (v_content_id, v_body_id, v_es_lang_id, '<p>Descubre las ultimas funciones incluyendo reportes avanzados, operaciones masivas y rendimiento mejorado.</p>'),
            (v_content_id, v_cta_text_id, v_es_lang_id, 'Ver Novedades'),
            (v_content_id, v_cta_url_id, v_es_lang_id, '/novedades');
        END IF;
    END IF;
END $$;

COMMIT;
