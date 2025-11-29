-- ============================================================================
-- Migration: 005_outbox_notify_trigger.sql
-- Description: Add PostgreSQL NOTIFY trigger for outbox pattern
-- Date: 2024-11-26
-- ============================================================================
-- This trigger fires a NOTIFY on the 'outbox_events' channel whenever a new
-- row is inserted into the cdc.outbox table. The OutboxPublisherService
-- LISTENs on this channel and processes pending events.
-- ============================================================================

-- Create the notify function
CREATE OR REPLACE FUNCTION cdc.notify_outbox_insert()
RETURNS TRIGGER AS $$
BEGIN
    -- Send notification with minimal payload (just a signal)
    -- The listener will query the table for actual data
    PERFORM pg_notify('outbox_events', json_build_object(
        'table', TG_TABLE_NAME,
        'action', TG_OP,
        'timestamp', CURRENT_TIMESTAMP
    )::text);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create the trigger on cdc.outbox
DROP TRIGGER IF EXISTS outbox_insert_notify ON cdc.outbox;

CREATE TRIGGER outbox_insert_notify
    AFTER INSERT ON cdc.outbox
    FOR EACH ROW
    EXECUTE FUNCTION cdc.notify_outbox_insert();

-- Add comment
COMMENT ON FUNCTION cdc.notify_outbox_insert() IS
    'Sends PostgreSQL NOTIFY on outbox_events channel when new outbox entries are created';

-- ============================================================================
-- Verification query (run manually to test):
-- LISTEN outbox_events;
-- INSERT INTO cdc.outbox (tenant_id, aggregate_type, aggregate_id, event_type, event_data)
-- VALUES ('test', 'test', gen_random_uuid(), 'test.created', '{}');
-- ============================================================================
