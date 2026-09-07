SET XACT_ABORT ON;

BEGIN TRANSACTION;

CREATE TABLE dbo.notificacionesusuario (
    idnotificacion UNIQUEIDENTIFIER PRIMARY KEY,
    idusuariodestinatario BIGINT NOT NULL,
    idticket UNIQUEIDENTIFIER NOT NULL,
    idtiponotificacion INT NOT NULL,
    mensaje VARCHAR(250) NOT NULL,
    fechacreacion DATETIMEOFFSET NOT NULL,
    fechalectura DATETIMEOFFSET NULL,
    fechaexpiracion DATETIMEOFFSET NOT NULL,
    CONSTRAINT fk_notificacionesusuario_usuario FOREIGN KEY (idusuariodestinatario)
        REFERENCES dbo.usuarios(idusuario),
    CONSTRAINT fk_notificacionesusuario_ticket FOREIGN KEY (idticket)
        REFERENCES dbo.tickets(idticket) ON DELETE CASCADE
);

CREATE INDEX ix_notificacionesusuario_usuario_fecha
    ON dbo.notificacionesusuario(idusuariodestinatario, fechacreacion DESC);
CREATE INDEX ix_notificacionesusuario_usuario_lectura_expiracion
    ON dbo.notificacionesusuario(idusuariodestinatario, fechalectura, fechaexpiracion);
CREATE INDEX ix_notificacionesusuario_ticket
    ON dbo.notificacionesusuario(idticket);

COMMIT TRANSACTION;
