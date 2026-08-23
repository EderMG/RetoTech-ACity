-- ============================================================
-- EventService — PostgreSQL — Script de inicialización mínima
-- ============================================================
-- Nota: en desarrollo, EF Core aplica migraciones automáticamente al iniciar
-- (ver Program.cs -> db.Database.Migrate()). Este script se deja como
-- referencia / fallback para levantar el esquema manualmente sin EF.

CREATE TABLE IF NOT EXISTS events (
    id              UUID PRIMARY KEY,
    name            VARCHAR(200) NOT NULL,
    date            TIMESTAMP NOT NULL,
    venue           VARCHAR(200) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'Draft',
    created_at      TIMESTAMP NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS zones (
    id              UUID PRIMARY KEY,
    event_id        UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    name            VARCHAR(120) NOT NULL,
    price           NUMERIC(10,2) NOT NULL,
    capacity        INT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_events_name ON events (name);
CREATE INDEX IF NOT EXISTS idx_events_date ON events (date);
CREATE INDEX IF NOT EXISTS idx_zones_event_id ON zones (event_id);

-- Datos semilla opcionales (útiles para probar GET /events sin crear eventos manualmente)
INSERT INTO events (id, name, date, venue, status, created_at)
VALUES ('11111111-1111-1111-1111-111111111111', 'Concierto Demo - Rock en Lima', now() + interval '30 days', 'Estadio Nacional, Lima', 'Published', now())
ON CONFLICT (id) DO NOTHING;

INSERT INTO zones (id, event_id, name, price, capacity)
VALUES
    ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'VIP', 250.00, 500),
    ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'General', 90.00, 5000)
ON CONFLICT (id) DO NOTHING;
