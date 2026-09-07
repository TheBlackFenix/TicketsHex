BEGIN;

CREATE TABLE IF NOT EXISTS tiposticket (
    idtipo INT PRIMARY KEY,
    tipo VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE);

CREATE TABLE IF NOT EXISTS prioridadesticket (
    idprioridad INT PRIMARY KEY,
    prioridad VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE);

CREATE TABLE IF NOT EXISTS impactosticket (
    idimpacto INT PRIMARY KEY,
    impacto VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE);

INSERT INTO tiposticket (idtipo, tipo, descripcion, activo) VALUES
(1, 'Incidente', 'Falla o comportamiento inesperado', TRUE),
(2, 'Requerimiento', 'Solicitud funcional o tecnica', TRUE),
(3, 'Mejora', 'Optimización de una funcionalidad existente', TRUE)
ON CONFLICT (idtipo) DO NOTHING;

INSERT INTO prioridadesticket (idprioridad, prioridad, descripcion, activo) VALUES
(1, 'Baja', 'Atencion sin urgencia operativa', TRUE),
(2, 'Media', 'Atencion dentro del flujo ordinario', TRUE),
(3, 'Alta', 'Atencion prioritaria', TRUE),
(4, 'Crítica', 'Atencion inmediata', TRUE)
ON CONFLICT (idprioridad) DO NOTHING;

INSERT INTO impactosticket (idimpacto, impacto, descripcion, activo) VALUES
(1, 'Bajo', 'Afectacion limitada', TRUE),
(2, 'Medio', 'Afectacion moderada', TRUE),
(3, 'Alto', 'Afectacion significativa', TRUE),
(4, 'Crítico', 'Afectacion general o de operacion critica', TRUE)
ON CONFLICT (idimpacto) DO NOTHING;

ALTER TABLE tickets
    ADD COLUMN IF NOT EXISTS idtipo INT,
    ADD COLUMN IF NOT EXISTS idprioridad INT,
    ADD COLUMN IF NOT EXISTS idimpacto INT;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_tickets_tiposticket') THEN
        ALTER TABLE tickets ADD CONSTRAINT fk_tickets_tiposticket
            FOREIGN KEY (idtipo) REFERENCES tiposticket(idtipo);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_tickets_prioridadesticket') THEN
        ALTER TABLE tickets ADD CONSTRAINT fk_tickets_prioridadesticket
            FOREIGN KEY (idprioridad) REFERENCES prioridadesticket(idprioridad);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_tickets_impactosticket') THEN
        ALTER TABLE tickets ADD CONSTRAINT fk_tickets_impactosticket
            FOREIGN KEY (idimpacto) REFERENCES impactosticket(idimpacto);
    END IF;
END $$;

COMMIT;
