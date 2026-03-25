-- Event Sourcing Database Schema
-- PostgreSQL 16+ required for gen_random_uuid() function

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create event_store table
CREATE TABLE IF NOT EXISTS event_store (
    event_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stream_id VARCHAR(200) NOT NULL,
    stream_type VARCHAR(100) NOT NULL,
    version INTEGER NOT NULL,
    event_type VARCHAR(200) NOT NULL,
    event_data JSONB NOT NULL,
    metadata JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uk_event_store_stream_version UNIQUE (stream_id, version)
);

-- Create outbox_messages table
CREATE TABLE IF NOT EXISTS outbox_messages (
    message_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL,
    topic_name VARCHAR(200) NOT NULL,
    payload JSONB NOT NULL,
    headers JSONB NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    retry_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    published_at TIMESTAMPTZ NULL,
    CONSTRAINT fk_outbox_event FOREIGN KEY (event_id) REFERENCES event_store (event_id) ON DELETE CASCADE
);

-- Create snapshots table
CREATE TABLE IF NOT EXISTS snapshots (
    stream_id VARCHAR(200) PRIMARY KEY,
    version INTEGER NOT NULL,
    snapshot_data JSONB NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS ix_event_store_stream_id ON event_store (stream_id);
CREATE INDEX IF NOT EXISTS ix_event_store_stream_id_version ON event_store (stream_id, version);
CREATE INDEX IF NOT EXISTS ix_event_store_event_type ON event_store (event_type);
CREATE INDEX IF NOT EXISTS ix_event_store_created_at ON event_store (created_at);

CREATE INDEX IF NOT EXISTS ix_outbox_status_created_at ON outbox_messages (status, created_at);
CREATE INDEX IF NOT EXISTS ix_outbox_event_id ON outbox_messages (event_id);

-- Check constraints for data integrity
ALTER TABLE outbox_messages
ADD CONSTRAINT chk_outbox_status
CHECK (status IN ('Pending', 'Published', 'Failed', 'PoisonMessage'));

ALTER TABLE event_store
ADD CONSTRAINT chk_version_positive
CHECK (version > 0);

ALTER TABLE snapshots
ADD CONSTRAINT chk_snapshot_version_positive
CHECK (version > 0);

-- Comments for documentation
COMMENT ON TABLE event_store IS 'Stores all domain events in append-only fashion';
COMMENT ON COLUMN event_store.stream_id IS 'Aggregate identifier (e.g., order-123)';
COMMENT ON COLUMN event_store.stream_type IS 'Aggregate type (e.g., Order)';
COMMENT ON COLUMN event_store.version IS 'Event version within the stream for optimistic concurrency';
COMMENT ON COLUMN event_store.event_type IS 'Domain event type (e.g., OrderCreated)';
COMMENT ON COLUMN event_store.event_data IS 'Serialized event payload as JSON';
COMMENT ON COLUMN event_store.metadata IS 'Event metadata (correlation IDs, actor, etc.)';

COMMENT ON TABLE outbox_messages IS 'Outbox pattern implementation for reliable event publishing';
COMMENT ON COLUMN outbox_messages.status IS 'Processing status: Pending, Published, Failed, or PoisonMessage';
COMMENT ON COLUMN outbox_messages.retry_count IS 'Number of publishing attempts';

COMMENT ON TABLE snapshots IS 'Optional aggregate snapshots for performance optimization';
COMMENT ON COLUMN snapshots.version IS 'Snapshot version matching event store version';

-- Grant permissions (adjust as needed for your deployment)
-- GRANT SELECT, INSERT, UPDATE ON event_store TO app_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON outbox_messages TO app_user;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON snapshots TO app_user;
