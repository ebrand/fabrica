-- Migration: Add domain_name and topic_name columns to outbox_config
-- Allows the ESB Producer to know which Kafka topic to publish events to

-- Add the domain_name column
ALTER TABLE cdc.outbox_config
ADD COLUMN IF NOT EXISTS domain_name VARCHAR(100);

-- Set domain for this database (admin)
UPDATE cdc.outbox_config
SET domain_name = 'admin'
WHERE domain_name IS NULL;

-- Make domain_name NOT NULL
ALTER TABLE cdc.outbox_config
ALTER COLUMN domain_name SET NOT NULL;

-- Add the topic_name column
ALTER TABLE cdc.outbox_config
ADD COLUMN IF NOT EXISTS topic_name VARCHAR(255);

-- Update existing rows with default topic names based on domain.table pattern
UPDATE cdc.outbox_config
SET topic_name = domain_name || '.' || table_name
WHERE topic_name IS NULL;

-- Make the column NOT NULL after populating existing rows
ALTER TABLE cdc.outbox_config
ALTER COLUMN topic_name SET NOT NULL;

-- Add comments
COMMENT ON COLUMN cdc.outbox_config.domain_name IS 'Domain name (admin, product, customer, etc.)';
COMMENT ON COLUMN cdc.outbox_config.topic_name IS 'Kafka topic name for publishing CDC events';

-- Create index for topic lookups
CREATE INDEX IF NOT EXISTS idx_outbox_config_topic ON cdc.outbox_config (topic_name);
CREATE INDEX IF NOT EXISTS idx_outbox_config_domain ON cdc.outbox_config (domain_name);
