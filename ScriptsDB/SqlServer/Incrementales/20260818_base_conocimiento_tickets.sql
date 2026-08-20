SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.tiposentradaconocimiento', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tiposentradaconocimiento (
            idtipoentrada INT NOT NULL CONSTRAINT pk_tiposentradaconocimiento PRIMARY KEY,
            nombre VARCHAR(50) NOT NULL,
            descripcion VARCHAR(200) NULL,
            activo BIT NOT NULL CONSTRAINT df_tiposentradaconocimiento_activo DEFAULT (1)
        );
    END;

    IF OBJECT_ID(N'dbo.resultadosentradaconocimiento', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.resultadosentradaconocimiento (
            idresultado INT NOT NULL CONSTRAINT pk_resultadosentradaconocimiento PRIMARY KEY,
            idtipoentrada INT NOT NULL,
            nombre VARCHAR(50) NOT NULL,
            descripcion VARCHAR(200) NULL,
            activo BIT NOT NULL CONSTRAINT df_resultadosentradaconocimiento_activo DEFAULT (1),
            CONSTRAINT fk_resultadosentrada_tiposentrada FOREIGN KEY (idtipoentrada)
                REFERENCES dbo.tiposentradaconocimiento(idtipoentrada),
            CONSTRAINT ux_resultadosentrada_tipo_nombre UNIQUE (idtipoentrada, nombre),
            CONSTRAINT ux_resultadosentrada_resultado_tipo UNIQUE (idresultado, idtipoentrada)
        );
    END;

    IF OBJECT_ID(N'dbo.ambientesticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ambientesticket (
            idambiente INT NOT NULL CONSTRAINT pk_ambientesticket PRIMARY KEY,
            nombre VARCHAR(50) NOT NULL,
            descripcion VARCHAR(200) NULL,
            activo BIT NOT NULL CONSTRAINT df_ambientesticket_activo DEFAULT (1)
        );
    END;

    IF OBJECT_ID(N'dbo.entradasconocimientoticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.entradasconocimientoticket (
            identrada UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_entradasconocimientoticket PRIMARY KEY,
            idticket UNIQUEIDENTIFIER NOT NULL,
            idtipoentrada INT NOT NULL,
            idresultado INT NOT NULL,
            resumen VARCHAR(2000) NOT NULL,
            sintomas VARCHAR(2000) NULL,
            comprobaciones VARCHAR(4000) NULL,
            pasosreproduccion VARCHAR(4000) NULL,
            idambiente INT NULL,
            requieredespliegue BIT NULL,
            observaciones VARCHAR(2000) NULL,
            idusuarioautor BIGINT NOT NULL,
            idrolautor INT NOT NULL,
            fechacreacion DATETIMEOFFSET NOT NULL CONSTRAINT df_entradasconocimiento_fecha DEFAULT SYSDATETIMEOFFSET(),
            fechaultimaactualizacion DATETIMEOFFSET NULL,
            activo BIT NOT NULL CONSTRAINT df_entradasconocimiento_activo DEFAULT (1),
            CONSTRAINT fk_entradasconocimiento_tickets FOREIGN KEY (idticket)
                REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
            CONSTRAINT fk_entradasconocimiento_tipos FOREIGN KEY (idtipoentrada)
                REFERENCES dbo.tiposentradaconocimiento(idtipoentrada),
            CONSTRAINT fk_entradasconocimiento_resultados FOREIGN KEY (idresultado, idtipoentrada)
                REFERENCES dbo.resultadosentradaconocimiento(idresultado, idtipoentrada),
            CONSTRAINT fk_entradasconocimiento_ambientes FOREIGN KEY (idambiente)
                REFERENCES dbo.ambientesticket(idambiente),
            CONSTRAINT fk_entradasconocimiento_usuarios FOREIGN KEY (idusuarioautor)
                REFERENCES dbo.usuarios(idusuario),
            CONSTRAINT fk_entradasconocimiento_roles FOREIGN KEY (idrolautor)
                REFERENCES dbo.roles(idrol)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_entradasconocimiento_ticket_fecha' AND object_id = OBJECT_ID(N'dbo.entradasconocimientoticket'))
        CREATE INDEX ix_entradasconocimiento_ticket_fecha ON dbo.entradasconocimientoticket(idticket, fechacreacion DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_entradasconocimiento_tipo_resultado' AND object_id = OBJECT_ID(N'dbo.entradasconocimientoticket'))
        CREATE INDEX ix_entradasconocimiento_tipo_resultado ON dbo.entradasconocimientoticket(idtipoentrada, idresultado);

    IF OBJECT_ID(N'dbo.referenciasentradaconocimiento', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.referenciasentradaconocimiento (
            idreferencia UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_referenciasentradaconocimiento PRIMARY KEY,
            identrada UNIQUEIDENTIFIER NOT NULL,
            tiporeferencia INT NOT NULL,
            url VARCHAR(2048) NOT NULL,
            descripcion VARCHAR(300) NULL,
            CONSTRAINT fk_referenciasconocimiento_entradas FOREIGN KEY (identrada)
                REFERENCES dbo.entradasconocimientoticket(identrada) ON DELETE CASCADE
        );
        CREATE INDEX ix_referenciasconocimiento_entrada ON dbo.referenciasentradaconocimiento(identrada);
    END;

    IF OBJECT_ID(N'dbo.revisionesentradaconocimiento', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.revisionesentradaconocimiento (
            idrevision UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_revisionesentradaconocimiento PRIMARY KEY,
            identrada UNIQUEIDENTIFIER NOT NULL,
            contenidoanterior VARCHAR(MAX) NOT NULL,
            idusuarioaccion BIGINT NOT NULL,
            idrolusuarioaccion INT NOT NULL,
            idestadoticket INT NOT NULL,
            fecharevision DATETIMEOFFSET NOT NULL CONSTRAINT df_revisionesconocimiento_fecha DEFAULT SYSDATETIMEOFFSET(),
            CONSTRAINT fk_revisionesconocimiento_entradas FOREIGN KEY (identrada)
                REFERENCES dbo.entradasconocimientoticket(identrada) ON DELETE CASCADE,
            CONSTRAINT fk_revisionesconocimiento_usuarios FOREIGN KEY (idusuarioaccion)
                REFERENCES dbo.usuarios(idusuario),
            CONSTRAINT fk_revisionesconocimiento_roles FOREIGN KEY (idrolusuarioaccion)
                REFERENCES dbo.roles(idrol),
            CONSTRAINT fk_revisionesconocimiento_estados FOREIGN KEY (idestadoticket)
                REFERENCES dbo.estadosticket(idestado)
        );
        CREATE INDEX ix_revisionesconocimiento_entrada_fecha
            ON dbo.revisionesentradaconocimiento(identrada, fecharevision DESC);
    END;

    IF OBJECT_ID(N'dbo.tags', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tags (
            idtag UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_tags PRIMARY KEY,
            nombre VARCHAR(50) NOT NULL,
            nombrenormalizado VARCHAR(50) NOT NULL,
            activo BIT NOT NULL CONSTRAINT df_tags_activo DEFAULT (1)
        );
        CREATE UNIQUE INDEX ux_tags_nombrenormalizado ON dbo.tags(nombrenormalizado);
    END;

    IF OBJECT_ID(N'dbo.tagsticket', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tagsticket (
            idtagticket UNIQUEIDENTIFIER NOT NULL CONSTRAINT pk_tagsticket PRIMARY KEY,
            idticket UNIQUEIDENTIFIER NOT NULL,
            idtag UNIQUEIDENTIFIER NOT NULL,
            fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_tagsticket_fecha DEFAULT SYSDATETIMEOFFSET(),
            CONSTRAINT fk_tagsticket_tickets FOREIGN KEY (idticket)
                REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
            CONSTRAINT fk_tagsticket_tags FOREIGN KEY (idtag)
                REFERENCES dbo.tags(idtag) ON DELETE CASCADE
        );
        CREATE UNIQUE INDEX ux_tagsticket_ticket_tag ON dbo.tagsticket(idticket, idtag);
    END;

    MERGE dbo.tiposentradaconocimiento AS destino
    USING (VALUES
        (1, 'Diagnostico', 'Hipotesis y comprobaciones tecnicas realizadas'),
        (2, 'Solucion', 'Solucion planteada o implementada'),
        (3, 'ValidacionQa', 'Validacion funcional realizada por QA')
    ) AS origen(idtipoentrada, nombre, descripcion)
    ON destino.idtipoentrada = origen.idtipoentrada
    WHEN NOT MATCHED THEN INSERT (idtipoentrada, nombre, descripcion, activo)
        VALUES (origen.idtipoentrada, origen.nombre, origen.descripcion, 1);

    MERGE dbo.resultadosentradaconocimiento AS destino
    USING (VALUES
        (1, 1, 'Confirmado', 'La hipotesis fue confirmada'),
        (2, 1, 'Descartado', 'La hipotesis fue descartada'),
        (3, 1, 'Inconcluso', 'No fue posible confirmar ni descartar la hipotesis'),
        (4, 2, 'Exitosa', 'La solucion produjo el resultado esperado'),
        (5, 2, 'Fallida', 'La solucion no produjo el resultado esperado'),
        (6, 2, 'Parcial', 'La solucion resolvio parcialmente el caso'),
        (7, 2, 'NoImplementada', 'La solucion no fue implementada'),
        (8, 3, 'Aprobada', 'QA aprobo la validacion'),
        (9, 3, 'Rechazada', 'QA rechazo la validacion'),
        (10, 3, 'ConObservaciones', 'QA registro observaciones pendientes')
    ) AS origen(idresultado, idtipoentrada, nombre, descripcion)
    ON destino.idresultado = origen.idresultado
    WHEN NOT MATCHED THEN INSERT (idresultado, idtipoentrada, nombre, descripcion, activo)
        VALUES (origen.idresultado, origen.idtipoentrada, origen.nombre, origen.descripcion, 1);

    MERGE dbo.ambientesticket AS destino
    USING (VALUES
        (1, 'Local', 'Ambiente local del desarrollador'),
        (2, 'Desarrollo', 'Ambiente compartido de desarrollo'),
        (3, 'ApiTesting', 'Ambiente de pruebas de API'),
        (4, 'QA', 'Ambiente formal de calidad'),
        (5, 'Produccion', 'Ambiente productivo')
    ) AS origen(idambiente, nombre, descripcion)
    ON destino.idambiente = origen.idambiente
    WHEN NOT MATCHED THEN INSERT (idambiente, nombre, descripcion, activo)
        VALUES (origen.idambiente, origen.nombre, origen.descripcion, 1);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
