CREATE TABLE IF NOT EXISTS host (
    id INTEGER PRIMARY KEY NOT NULL,
    url TEXT NOT NULL,
    guild_id INTEGER NOT NULL,
    UNIQUE (url, guild_id)
);

CREATE TABLE IF NOT EXISTS notification (
    id INTEGER PRIMARY KEY NOT NULL,
    host_id INTEGER NULL,
    stream_key TEXT NULL,
    channel TEXT NOT NULL,
    created_by INTEGER NOT NULL,
    FOREIGN KEY (host_id) REFERENCES host(id)
);

-- We use -1 here to denote available to all guilds. Traditionally I would have set it as null
-- however sqlite UNIQUE() constraint does not support uniqueness via null values
INSERT OR IGNORE INTO host(url, guild_id) VALUES("https://b.siobud.com", "-1");