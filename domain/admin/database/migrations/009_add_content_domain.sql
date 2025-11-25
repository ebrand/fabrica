-- Migration: Add Content domain to the domain registry
-- This registers the Content domain in the ESB system

-- Insert the content domain record
INSERT INTO fabrica.domain (
    domain_name,
    display_name,
    description,
    service_url,
    kafka_topic_prefix,
    schema_name,
    database_name,
    publishes_events,
    consumes_events,
    is_active
)
VALUES (
    'content',
    'Content Domain',
    'Handles CMS content, pages, blogs, media assets, menus, and multi-language translations',
    'http://acl-content:3460',
    'content',
    'fabrica',
    'fabrica-content-db',
    true,
    true,
    true
)
ON CONFLICT (domain_name) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    description = EXCLUDED.description,
    service_url = EXCLUDED.service_url,
    kafka_topic_prefix = EXCLUDED.kafka_topic_prefix,
    database_name = EXCLUDED.database_name,
    updated_at = CURRENT_TIMESTAMP;

-- Add cache config for admin domain to consume content events
INSERT INTO cache.cache_config (
    source_domain,
    source_schema,
    source_table,
    consumer_group,
    listen_create,
    listen_update,
    listen_delete,
    is_active,
    description
)
VALUES
    ('content', 'fabrica', 'content', 'admin-content.content', true, true, true, true, 'Cache content data from content domain'),
    ('content', 'fabrica', 'media', 'admin-content.media', true, true, true, true, 'Cache media data from content domain')
ON CONFLICT (source_domain, source_schema, source_table) DO NOTHING;
