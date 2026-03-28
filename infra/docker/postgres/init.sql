-- Development-only PostgreSQL init script
-- Runs after 001_CreateEventStoreSchema.sql (mounted from Database/Scripts)
-- Applies dev grants to the 'app' user created by POSTGRES_USER

-- Grant permissions on tables
GRANT SELECT, INSERT, UPDATE ON event_store TO app;
GRANT SELECT, INSERT, UPDATE, DELETE ON outbox_messages TO app;
GRANT SELECT, INSERT, UPDATE, DELETE ON snapshots TO app;

-- Grant usage on any sequences
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app;
