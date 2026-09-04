BEGIN;

INSERT INTO estadosticket (idestado, estado, descripcion, activo)
VALUES
    (16, 'EnReplicaQA', 'QA replica el escenario en ambiente preproductivo', TRUE),
    (17, 'Finalizado', 'Flujo cerrado de forma terminal por Planner o Lider Tecnico', TRUE)
ON CONFLICT (idestado) DO NOTHING;

ALTER TABLE historicoasignacionesticket
    ADD COLUMN IF NOT EXISTS idusuarioanterior BIGINT,
    ADD COLUMN IF NOT EXISTS idestado INT,
    ADD COLUMN IF NOT EXISTS idtipomovimiento INT;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_historicoasignacionesticket_usuarioanterior') THEN
        ALTER TABLE historicoasignacionesticket
            ADD CONSTRAINT fk_historicoasignacionesticket_usuarioanterior
            FOREIGN KEY (idusuarioanterior) REFERENCES usuarios(idusuario);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_historicoasignacionesticket_estado') THEN
        ALTER TABLE historicoasignacionesticket
            ADD CONSTRAINT fk_historicoasignacionesticket_estado
            FOREIGN KEY (idestado) REFERENCES estadosticket(idestado);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS responsablesticket (
    idresponsableticket UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idtiporesponsabilidad INT NOT NULL,
    idusuario BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idusuarioasignador BIGINT NOT NULL REFERENCES usuarios(idusuario)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_responsablesticket_ticket_tipo
    ON responsablesticket(idticket, idtiporesponsabilidad);

WITH candidatos AS (
    SELECT DISTINCT ON (h.idticket, u.idrol)
        h.idticket,
        CASE u.idrol WHEN 1 THEN 1 WHEN 2 THEN 2 END AS idtiporesponsabilidad,
        h.idusuarioasignado AS idusuario,
        h.idusuarioaccion AS idusuarioasignador
    FROM historicoasignacionesticket h
    INNER JOIN usuarios u ON u.idusuario = h.idusuarioasignado
    WHERE u.idrol IN (1, 2)
    ORDER BY h.idticket, u.idrol, h.fechaasignacion DESC, h.idhistoricoasignacion DESC
)
INSERT INTO responsablesticket (
    idticket,
    idtiporesponsabilidad,
    idusuario,
    idusuarioasignador)
SELECT c.idticket, c.idtiporesponsabilidad, c.idusuario, c.idusuarioasignador
FROM candidatos c
ON CONFLICT (idticket, idtiporesponsabilidad) DO NOTHING;

INSERT INTO responsablesticket (
    idticket,
    idtiporesponsabilidad,
    idusuario,
    idusuarioasignador)
SELECT
    t.idticket,
    CASE u.idrol WHEN 1 THEN 1 WHEN 2 THEN 2 END,
    t.idusuarioasignado,
    t.idusuarioasignado
FROM tickets t
INNER JOIN usuarios u ON u.idusuario = t.idusuarioasignado
WHERE u.idrol IN (1, 2)
ON CONFLICT (idticket, idtiporesponsabilidad) DO NOTHING;

COMMIT;

SELECT
    t.idticket,
    t.codigocaso,
    t.titulo,
    CASE
        WHEN dev.idresponsableticket IS NULL THEN 'DESARROLLADOR_NO_ASIGNADO'
        WHEN t.idestado IN (6, 7, 9, 10, 11, 12, 16)
             AND qa.idresponsableticket IS NULL THEN 'QA_NO_ASIGNADO'
    END AS incidencia
FROM tickets t
LEFT JOIN responsablesticket dev
    ON dev.idticket = t.idticket AND dev.idtiporesponsabilidad = 1
LEFT JOIN responsablesticket qa
    ON qa.idticket = t.idticket AND qa.idtiporesponsabilidad = 2
WHERE t.activo = TRUE
  AND (
      dev.idresponsableticket IS NULL OR
      (t.idestado IN (6, 7, 9, 10, 11, 12, 16) AND qa.idresponsableticket IS NULL));
