BEGIN;

CREATE TABLE notificacionesusuario (
    idnotificacion UUID PRIMARY KEY,
    idusuariodestinatario BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idtiponotificacion INT NOT NULL,
    mensaje VARCHAR(250) NOT NULL,
    fechacreacion TIMESTAMPTZ NOT NULL,
    fechalectura TIMESTAMPTZ,
    fechaexpiracion TIMESTAMPTZ NOT NULL
);

CREATE INDEX ix_notificacionesusuario_usuario_fecha
    ON notificacionesusuario(idusuariodestinatario, fechacreacion DESC);
CREATE INDEX ix_notificacionesusuario_usuario_lectura_expiracion
    ON notificacionesusuario(idusuariodestinatario, fechalectura, fechaexpiracion);
CREATE INDEX ix_notificacionesusuario_ticket
    ON notificacionesusuario(idticket);

COMMIT;
