BEGIN;

CREATE TABLE IF NOT EXISTS historicoasignacionesticket (
    idhistoricoasignacion UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idusuarioasignado BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idusuarioaccion BIGINT NOT NULL REFERENCES usuarios(idusuario),
    comentario VARCHAR(1000),
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_historicoasignacionesticket_usuario_ticket
    ON historicoasignacionesticket(idusuarioasignado, idticket);

WITH reasignaciones_parseadas AS (
    SELECT
        h.idhistorico,
        h.idticket,
        ((regexp_match(
            h.comentario,
            '^Reasignado\. Nuevo usuario: ([0-9]+)\. Obs:'
        ))[1])::BIGINT AS idusuarioasignado,
        h.idusuarioaccion,
        h.comentario,
        h.fechacambio
    FROM historicoestadosticket h
    WHERE h.comentario ~ '^Reasignado\. Nuevo usuario: [0-9]+\. Obs:'
)
INSERT INTO historicoasignacionesticket (
    idhistoricoasignacion,
    idticket,
    idusuarioasignado,
    idusuarioaccion,
    comentario,
    fechaasignacion
)
SELECT
    r.idhistorico,
    r.idticket,
    r.idusuarioasignado,
    r.idusuarioaccion,
    r.comentario,
    r.fechacambio
FROM reasignaciones_parseadas r
INNER JOIN usuarios u
    ON u.idusuario = r.idusuarioasignado
ON CONFLICT (idhistoricoasignacion) DO NOTHING;

WITH creadores AS (
    SELECT DISTINCT ON (h.idticket)
        h.idticket,
        h.idusuarioaccion
    FROM historicoestadosticket h
    ORDER BY h.idticket, h.fechacambio, h.idhistorico
)
INSERT INTO historicoasignacionesticket (
    idticket,
    idusuarioasignado,
    idusuarioaccion,
    comentario,
    fechaasignacion
)
SELECT
    t.idticket,
    t.idusuarioasignado,
    COALESCE(c.idusuarioaccion, t.idusuarioasignado),
    'Migracion: asignacion vigente al crear el historial estructurado.',
    COALESCE(t.fechaultimaactualizacion, t.fechaasignacion)
FROM tickets t
LEFT JOIN creadores c
    ON c.idticket = t.idticket
WHERE t.idusuarioasignado IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM historicoasignacionesticket existente
      WHERE existente.idticket = t.idticket
        AND existente.idusuarioasignado = t.idusuarioasignado
  );

COMMIT;
