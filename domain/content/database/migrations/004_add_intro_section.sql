-- Migration: 004_add_intro_section.sql
-- Description: Add Intro section type for lead paragraphs/introductions
-- Date: 2024-11-26

BEGIN;

-- ============================================================================
-- ADD INTRO SECTION TYPE
-- ============================================================================

DO $$
DECLARE
    v_intro_section_id UUID;
    v_article_block_id UUID;
    v_card_block_id UUID;
BEGIN
    -- Insert the new Intro section type
    INSERT INTO fabrica.section_type (section_type_id, tenant_id, name, slug, description, field_type, display_order)
    VALUES (uuid_generate_v4(), 'tenant-test', 'Intro', 'intro', 'Introduction or lead paragraph text', 'text', 2)
    ON CONFLICT (tenant_id, slug) DO NOTHING
    RETURNING section_type_id INTO v_intro_section_id;

    -- If already existed, get the ID
    IF v_intro_section_id IS NULL THEN
        SELECT section_type_id INTO v_intro_section_id
        FROM fabrica.section_type
        WHERE tenant_id = 'tenant-test' AND slug = 'intro';
    END IF;

    -- Get block IDs
    SELECT block_id INTO v_article_block_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'article';
    SELECT block_id INTO v_card_block_id FROM fabrica.block WHERE tenant_id = 'tenant-test' AND slug = 'card';

    -- Link Intro to Article block (after subtitle, before author)
    IF v_article_block_id IS NOT NULL AND v_intro_section_id IS NOT NULL THEN
        INSERT INTO fabrica.block_section (block_id, section_type_id, is_required, display_order)
        VALUES (v_article_block_id, v_intro_section_id, false, 3)
        ON CONFLICT (block_id, section_type_id) DO NOTHING;
    END IF;

    -- Link Intro to Card block (after subtitle, before body)
    IF v_card_block_id IS NOT NULL AND v_intro_section_id IS NOT NULL THEN
        INSERT INTO fabrica.block_section (block_id, section_type_id, is_required, display_order)
        VALUES (v_card_block_id, v_intro_section_id, false, 3)
        ON CONFLICT (block_id, section_type_id) DO NOTHING;
    END IF;

    RAISE NOTICE 'Intro section type added successfully';
END $$;

COMMIT;
