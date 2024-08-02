CREATE TABLE IF NOT EXISTS host (
    id INTEGER PRIMARY KEY NOT NULL,
    url TEXT NOT NULL,
    poll_interval_seconds INTEGER DEFAULT 30 NOT NULL,
    guild_id INTEGER NOT NULL,
    created_by INTEGER NOT NULL,
    UNIQUE (url, guild_id)
);

CREATE TABLE IF NOT EXISTS subscription (
    id INTEGER PRIMARY KEY NOT NULL,
    host_id INTEGER NULL,
    stream_key TEXT NULL,
    channel_id INTEGER NOT NULL,
    created_by INTEGER NOT NULL,
    FOREIGN KEY (host_id) REFERENCES host(id) ON DELETE CASCADE,
    UNIQUE (host_id, stream_key)
);

CREATE TABLE IF NOT EXISTS stream (
    id INTEGER PRIMARY KEY NOT NULL,
    subscription_id INTEGER NOT NULL,
    status INTEGER NOT NULL,
    viewer_count INTEGER DEFAULT 0 NOT NULL,
    message_id INTEGER NOT NULL,
    playing TEXT NULL,
    snapshot BLOB null,
    start_time TEXT NOT NULL,
    end_time TEXT NULL,
    FOREIGN KEY (subscription_id) REFERENCES subscription(id) ON DELETE CASCADE,
    UNIQUE (subscription_id)
);