SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.tiposticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tiposticket (
            idtipo INT NOT NULL CONSTRAINT pk_tiposticket PRIMARY KEY,
            tipo VARCHAR(50) NOT NULL,
            descripcion VARCHAR(200) NULL,
            activo BIT NOT NULL CONSTRAINT df_tiposticket_activo DEFAULT (1));
    END;

    IF OBJECT_ID(N'dbo.prioridadesticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.prioridadesticket (
            idprioridad INT NOT NULL CONSTRAINT pk_prioridadesticket PRIMARY KEY,
            prioridad VARCHAR(50) NOT NULL,
            descripcion VARCHAR(200) NULL,
            activo BIT NOT NULL CONSTRAINT df_prioridadesticket_activo DEFAULT (1));
    END;

    IF OBJECT_ID(N'dbo.impactosticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.impactosticket (
            idimpacto INT NOT NULL CONSTRAINT pk_impactosticket PRIMARY KEY,
            impacto VARCHAR(50) NOT NULL,
            descripcion VARCHAR(200) NULL,
            activo BIT NOT NULL CONSTRAINT df_impactosticket_activo DEFAULT (1));
    END;

    MERGE dbo.tiposticket AS destino
    USING (VALUES
        (1, 'Incidente', 'Falla o comportamiento inesperado'),
        (2, 'Requerimiento', 'Solicitud funcional o tecnica'),
        (3, 'Mejora', 'Optimización de una funcionalidad existente')
    ) AS origen(idtipo, tipo, descripcion)
    ON destino.idtipo = origen.idtipo
    WHEN NOT MATCHED THEN
        INSERT (idtipo, tipo, descripcion, activo)
        VALUES (origen.idtipo, origen.tipo, origen.descripcion, 1);

    MERGE dbo.prioridadesticket AS destino
    USING (VALUES
        (1, 'Baja', 'Atencion sin urgencia operativa'),
        (2, 'Media', 'Atencion dentro del flujo ordinario'),
        (3, 'Alta', 'Atencion prioritaria'),
        (4, 'Crítica', 'Atencion inmediata')
    ) AS origen(idprioridad, prioridad, descripcion)
    ON destino.idprioridad = origen.idprioridad
    WHEN NOT MATCHED THEN
        INSERT (idprioridad, prioridad, descripcion, activo)
        VALUES (origen.idprioridad, origen.prioridad, origen.descripcion, 1);

    MERGE dbo.impactosticket AS destino
    USING (VALUES
        (1, 'Bajo', 'Afectacion limitada'),
        (2, 'Medio', 'Afectacion moderada'),
        (3, 'Alto', 'Afectacion significativa'),
        (4, 'Crítico', 'Afectacion general o de operacion critica')
    ) AS origen(idimpacto, impacto, descripcion)
    ON destino.idimpacto = origen.idimpacto
    WHEN NOT MATCHED THEN
        INSERT (idimpacto, impacto, descripcion, activo)
        VALUES (origen.idimpacto, origen.impacto, origen.descripcion, 1);

    IF COL_LENGTH('dbo.tickets', 'idtipo') IS NULL
        ALTER TABLE dbo.tickets ADD idtipo INT NULL;
    IF COL_LENGTH('dbo.tickets', 'idprioridad') IS NULL
        ALTER TABLE dbo.tickets ADD idprioridad INT NULL;
    IF COL_LENGTH('dbo.tickets', 'idimpacto') IS NULL
        ALTER TABLE dbo.tickets ADD idimpacto INT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_tickets_tiposticket')
        ALTER TABLE dbo.tickets WITH CHECK ADD CONSTRAINT fk_tickets_tiposticket
            FOREIGN KEY (idtipo) REFERENCES dbo.tiposticket(idtipo);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_tickets_prioridadesticket')
        ALTER TABLE dbo.tickets WITH CHECK ADD CONSTRAINT fk_tickets_prioridadesticket
            FOREIGN KEY (idprioridad) REFERENCES dbo.prioridadesticket(idprioridad);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_tickets_impactosticket')
        ALTER TABLE dbo.tickets WITH CHECK ADD CONSTRAINT fk_tickets_impactosticket
            FOREIGN KEY (idimpacto) REFERENCES dbo.impactosticket(idimpacto);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
