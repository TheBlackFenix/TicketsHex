SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (SELECT 1 FROM dbo.estadosticket WHERE idestado = 16)
        INSERT INTO dbo.estadosticket (idestado, estado, descripcion, activo)
        VALUES (16, 'EnReplicaQA', 'QA replica el escenario en ambiente preproductivo', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.estadosticket WHERE idestado = 17)
        INSERT INTO dbo.estadosticket (idestado, estado, descripcion, activo)
        VALUES (17, 'Finalizado', 'Flujo cerrado de forma terminal por Planner o Lider Tecnico', 1);

    IF COL_LENGTH('dbo.historicoasignacionesticket', 'idusuarioanterior') IS NULL
        ALTER TABLE dbo.historicoasignacionesticket ADD idusuarioanterior BIGINT NULL;
    IF COL_LENGTH('dbo.historicoasignacionesticket', 'idestado') IS NULL
        ALTER TABLE dbo.historicoasignacionesticket ADD idestado INT NULL;
    IF COL_LENGTH('dbo.historicoasignacionesticket', 'idtipomovimiento') IS NULL
        ALTER TABLE dbo.historicoasignacionesticket ADD idtipomovimiento INT NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'fk_historicoasignacionesticket_usuarioanterior'
          AND parent_object_id = OBJECT_ID(N'dbo.historicoasignacionesticket'))
    BEGIN
        ALTER TABLE dbo.historicoasignacionesticket WITH CHECK
            ADD CONSTRAINT fk_historicoasignacionesticket_usuarioanterior
            FOREIGN KEY (idusuarioanterior) REFERENCES dbo.usuarios(idusuario);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = N'fk_historicoasignacionesticket_estado'
          AND parent_object_id = OBJECT_ID(N'dbo.historicoasignacionesticket'))
    BEGIN
        ALTER TABLE dbo.historicoasignacionesticket WITH CHECK
            ADD CONSTRAINT fk_historicoasignacionesticket_estado
            FOREIGN KEY (idestado) REFERENCES dbo.estadosticket(idestado);
    END;

    IF OBJECT_ID(N'dbo.responsablesticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.responsablesticket (
            idresponsableticket UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT pk_responsablesticket PRIMARY KEY
                CONSTRAINT df_responsablesticket_id DEFAULT NEWID(),
            idticket UNIQUEIDENTIFIER NOT NULL,
            idtiporesponsabilidad INT NOT NULL,
            idusuario BIGINT NOT NULL,
            idusuarioasignador BIGINT NOT NULL,
            CONSTRAINT fk_responsablesticket_tickets
                FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
            CONSTRAINT fk_responsablesticket_usuario
                FOREIGN KEY (idusuario) REFERENCES dbo.usuarios(idusuario),
            CONSTRAINT fk_responsablesticket_usuarioasignador
                FOREIGN KEY (idusuarioasignador) REFERENCES dbo.usuarios(idusuario)
        );
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'ux_responsablesticket_ticket_tipo'
          AND object_id = OBJECT_ID(N'dbo.responsablesticket'))
    BEGIN
        CREATE UNIQUE INDEX ux_responsablesticket_ticket_tipo
            ON dbo.responsablesticket(idticket, idtiporesponsabilidad);
    END;

    ;WITH candidatos AS (
        SELECT
            h.idticket,
            CASE u.idrol WHEN 1 THEN 1 WHEN 2 THEN 2 END AS idtiporesponsabilidad,
            h.idusuarioasignado AS idusuario,
            h.idusuarioaccion AS idusuarioasignador,
            ROW_NUMBER() OVER (
                PARTITION BY h.idticket, u.idrol
                ORDER BY h.fechaasignacion DESC, h.idhistoricoasignacion DESC) AS orden
        FROM dbo.historicoasignacionesticket h
        INNER JOIN dbo.usuarios u ON u.idusuario = h.idusuarioasignado
        WHERE u.idrol IN (1, 2)
    )
    INSERT INTO dbo.responsablesticket (
        idresponsableticket,
        idticket,
        idtiporesponsabilidad,
        idusuario,
        idusuarioasignador)
    SELECT NEWID(), c.idticket, c.idtiporesponsabilidad, c.idusuario, c.idusuarioasignador
    FROM candidatos c
    WHERE c.orden = 1
      AND NOT EXISTS (
          SELECT 1 FROM dbo.responsablesticket r
          WHERE r.idticket = c.idticket
            AND r.idtiporesponsabilidad = c.idtiporesponsabilidad);

    INSERT INTO dbo.responsablesticket (
        idresponsableticket,
        idticket,
        idtiporesponsabilidad,
        idusuario,
        idusuarioasignador)
    SELECT
        NEWID(),
        t.idticket,
        CASE u.idrol WHEN 1 THEN 1 WHEN 2 THEN 2 END,
        t.idusuarioasignado,
        t.idusuarioasignado
    FROM dbo.tickets t
    INNER JOIN dbo.usuarios u ON u.idusuario = t.idusuarioasignado
    WHERE u.idrol IN (1, 2)
      AND NOT EXISTS (
          SELECT 1 FROM dbo.responsablesticket r
          WHERE r.idticket = t.idticket
            AND r.idtiporesponsabilidad = CASE u.idrol WHEN 1 THEN 1 WHEN 2 THEN 2 END);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    t.idticket,
    t.codigocaso,
    t.titulo,
    CASE
        WHEN dev.idresponsableticket IS NULL THEN 'DESARROLLADOR_NO_ASIGNADO'
        WHEN t.idestado IN (6, 7, 9, 10, 11, 12, 16)
             AND qa.idresponsableticket IS NULL THEN 'QA_NO_ASIGNADO'
    END AS incidencia
FROM dbo.tickets t
LEFT JOIN dbo.responsablesticket dev
    ON dev.idticket = t.idticket AND dev.idtiporesponsabilidad = 1
LEFT JOIN dbo.responsablesticket qa
    ON qa.idticket = t.idticket AND qa.idtiporesponsabilidad = 2
WHERE t.activo = 1
  AND (
      dev.idresponsableticket IS NULL OR
      (t.idestado IN (6, 7, 9, 10, 11, 12, 16) AND qa.idresponsableticket IS NULL));
