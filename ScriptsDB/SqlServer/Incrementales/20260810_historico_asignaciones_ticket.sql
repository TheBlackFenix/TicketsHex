SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.historicoasignacionesticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.historicoasignacionesticket (
            idhistoricoasignacion UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT pk_historicoasignacionesticket PRIMARY KEY
                CONSTRAINT df_historicoasignacionesticket_id DEFAULT NEWID(),
            idticket UNIQUEIDENTIFIER NOT NULL,
            idusuarioasignado BIGINT NOT NULL,
            idusuarioaccion BIGINT NOT NULL,
            comentario VARCHAR(1000) NULL,
            fechaasignacion DATETIMEOFFSET NOT NULL
                CONSTRAINT df_historicoasignacionesticket_fecha DEFAULT SYSDATETIMEOFFSET(),
            CONSTRAINT fk_historicoasignacionesticket_tickets
                FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
            CONSTRAINT fk_historicoasignacionesticket_usuarioasignado
                FOREIGN KEY (idusuarioasignado) REFERENCES dbo.usuarios(idusuario),
            CONSTRAINT fk_historicoasignacionesticket_usuarioaccion
                FOREIGN KEY (idusuarioaccion) REFERENCES dbo.usuarios(idusuario)
        );
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'ix_historicoasignacionesticket_usuario_ticket'
          AND object_id = OBJECT_ID(N'dbo.historicoasignacionesticket')
    )
    BEGIN
        CREATE INDEX ix_historicoasignacionesticket_usuario_ticket
            ON dbo.historicoasignacionesticket(idusuarioasignado, idticket);
    END;

    ;WITH reasignaciones_parseadas AS (
        SELECT
            h.idhistorico,
            h.idticket,
            TRY_CONVERT(
                BIGINT,
                CASE
                    WHEN CHARINDEX('. Obs:', h.comentario) >
                         LEN('Reasignado. Nuevo usuario: ') + 1
                    THEN SUBSTRING(
                        h.comentario,
                        LEN('Reasignado. Nuevo usuario: ') + 1,
                        CHARINDEX('. Obs:', h.comentario) -
                            (LEN('Reasignado. Nuevo usuario: ') + 1)
                    )
                END
            ) AS idusuarioasignado,
            h.idusuarioaccion,
            h.comentario,
            h.fechacambio
        FROM dbo.historicoestadosticket h
        WHERE h.comentario LIKE 'Reasignado. Nuevo usuario: %. Obs:%'
          AND CHARINDEX('. Obs:', h.comentario) >
              LEN('Reasignado. Nuevo usuario: ') + 1
    )
    INSERT INTO dbo.historicoasignacionesticket (
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
    INNER JOIN dbo.usuarios u
        ON u.idusuario = r.idusuarioasignado
    WHERE r.idusuarioasignado IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.historicoasignacionesticket existente
          WHERE existente.idhistoricoasignacion = r.idhistorico
      );

    INSERT INTO dbo.historicoasignacionesticket (
        idhistoricoasignacion,
        idticket,
        idusuarioasignado,
        idusuarioaccion,
        comentario,
        fechaasignacion
    )
    SELECT
        NEWID(),
        t.idticket,
        t.idusuarioasignado,
        COALESCE(creacion.idusuarioaccion, t.idusuarioasignado),
        'Migracion: asignacion vigente al crear el historial estructurado.',
        COALESCE(t.fechaultimaactualizacion, t.fechaasignacion)
    FROM dbo.tickets t
    OUTER APPLY (
        SELECT TOP (1) h.idusuarioaccion
        FROM dbo.historicoestadosticket h
        WHERE h.idticket = t.idticket
        ORDER BY h.fechacambio, h.idhistorico
    ) creacion
    WHERE t.idusuarioasignado IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.historicoasignacionesticket existente
          WHERE existente.idticket = t.idticket
            AND existente.idusuarioasignado = t.idusuarioasignado
      );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
