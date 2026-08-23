CREATE TABLE telemetry_events (
    event_id TEXT PRIMARY KEY,
    received_at TEXT NOT NULL,
    occurred_at TEXT NOT NULL,
    installation_hash TEXT NOT NULL,
    session_hash TEXT NOT NULL,
    category TEXT NOT NULL,
    event_name TEXT NOT NULL,
    app_version TEXT NOT NULL,
    platform TEXT NOT NULL,
    attributes_json TEXT NOT NULL,
    schema_version INTEGER NOT NULL
);

CREATE INDEX telemetry_events_received_at_idx
    ON telemetry_events(received_at);

CREATE INDEX telemetry_events_name_received_idx
    ON telemetry_events(event_name, received_at);

CREATE TABLE telemetry_rate_limits (
    installation_hash TEXT NOT NULL,
    hour_bucket TEXT NOT NULL,
    event_count INTEGER NOT NULL,
    PRIMARY KEY (installation_hash, hour_bucket)
);

CREATE INDEX telemetry_rate_limits_hour_idx
    ON telemetry_rate_limits(hour_bucket);
