-- Migration: Add consumer_group column to cache_config
-- Tracks the Kafka consumer group name for each consumption configuration

-- Add the consumer_group column
ALTER TABLE cache.cache_config
ADD COLUMN IF NOT EXISTS consumer_group VARCHAR(255);

-- Update existing rows with default consumer group names
-- Pattern: {this_domain}-{source_domain}.{source_table}
UPDATE cache.cache_config
SET consumer_group = 'admin-' || source_domain || '.' || source_table
WHERE consumer_group IS NULL;

-- Make the column NOT NULL after populating existing rows
ALTER TABLE cache.cache_config
ALTER COLUMN consumer_group SET NOT NULL;

-- Add comment
COMMENT ON COLUMN cache.cache_config.consumer_group IS 'Kafka consumer group name for consuming events from this source';

-- Create index for consumer group lookups
CREATE INDEX IF NOT EXISTS idx_cache_config_consumer_group ON cache.cache_config (consumer_group);
