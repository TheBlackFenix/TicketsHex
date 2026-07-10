SET XACT_ABORT ON;
GO

IF SCHEMA_ID(N'dbo') IS NULL
    EXEC(N'CREATE SCHEMA dbo');
GO

BEGIN TRANSACTION;

-- 1. TABLAS MAESTRAS
IF OBJECT_ID(N'dbo.roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.roles (
        idrol INT NOT NULL,
        nombrerol VARCHAR(50) NOT NULL,
        descripcion VARCHAR(200) NULL,
        CONSTRAINT pk_roles PRIMARY KEY (idrol)
    );
END;

IF OBJECT_ID(N'dbo.estadosticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.estadosticket (
        idestado INT NOT NULL,
        estado VARCHAR(50) NOT NULL,
        descripcion VARCHAR(500) NULL,
        activo BIT NOT NULL CONSTRAINT df_estadosticket_activo DEFAULT (1),
        CONSTRAINT pk_estadosticket PRIMARY KEY (idestado)
    );
END;

IF OBJECT_ID(N'dbo.origenesticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.origenesticket (
        idorigen INT NOT NULL,
        origen VARCHAR(100) NOT NULL,
        activo BIT NOT NULL CONSTRAINT df_origenesticket_activo DEFAULT (1),
        CONSTRAINT pk_origenesticket PRIMARY KEY (idorigen)
    );
END;

IF OBJECT_ID(N'dbo.areasticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.areasticket (
        idarea INT NOT NULL,
        area VARCHAR(100) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT NOT NULL CONSTRAINT df_areasticket_activo DEFAULT (1),
        CONSTRAINT pk_areasticket PRIMARY KEY (idarea)
    );
END;

-- 2. USUARIOS Y SESIONES
IF OBJECT_ID(N'dbo.usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.usuarios (
        idusuario BIGINT NOT NULL,
        nombreusuario VARCHAR(50) NOT NULL,
        nombres VARCHAR(100) NOT NULL,
        apellidos VARCHAR(100) NULL,
        idrol INT NULL,
        idarea INT NULL,
        imagenperfilbase64 VARCHAR(MAX) NULL,
        activo BIT NOT NULL CONSTRAINT df_usuarios_activo DEFAULT (1),
        contrasenahash VARCHAR(500) NULL,
        intentosfallidos INT NOT NULL CONSTRAINT df_usuarios_intentosfallidos DEFAULT (0),
        bloqueado BIT NOT NULL CONSTRAINT df_usuarios_bloqueado DEFAULT (0),
        fechabloqueo DATETIMEOFFSET NULL,
        fechacambiocontrasena DATETIMEOFFSET NULL,
        CONSTRAINT pk_usuarios PRIMARY KEY (idusuario),
        CONSTRAINT fk_usuarios_roles FOREIGN KEY (idrol) REFERENCES dbo.roles(idrol),
        CONSTRAINT fk_usuarios_areasticket FOREIGN KEY (idarea) REFERENCES dbo.areasticket(idarea)
    );
END;

IF COL_LENGTH(N'dbo.usuarios', N'idarea') IS NULL
    ALTER TABLE dbo.usuarios ADD idarea INT NULL;

IF COL_LENGTH(N'dbo.usuarios', N'imagenperfilbase64') IS NULL
    ALTER TABLE dbo.usuarios ADD imagenperfilbase64 VARCHAR(MAX) NULL;

IF COL_LENGTH(N'dbo.usuarios', N'contrasenahash') IS NULL
    ALTER TABLE dbo.usuarios ADD contrasenahash VARCHAR(500) NULL;

IF COL_LENGTH(N'dbo.usuarios', N'intentosfallidos') IS NULL
    ALTER TABLE dbo.usuarios ADD intentosfallidos INT NOT NULL CONSTRAINT df_usuarios_intentosfallidos DEFAULT (0);

IF COL_LENGTH(N'dbo.usuarios', N'bloqueado') IS NULL
    ALTER TABLE dbo.usuarios ADD bloqueado BIT NOT NULL CONSTRAINT df_usuarios_bloqueado DEFAULT (0);

IF COL_LENGTH(N'dbo.usuarios', N'fechabloqueo') IS NULL
    ALTER TABLE dbo.usuarios ADD fechabloqueo DATETIMEOFFSET NULL;

IF COL_LENGTH(N'dbo.usuarios', N'fechacambiocontrasena') IS NULL
    ALTER TABLE dbo.usuarios ADD fechacambiocontrasena DATETIMEOFFSET NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_usuarios_nombreusuario'
      AND object_id = OBJECT_ID(N'dbo.usuarios')
)
BEGIN
    CREATE UNIQUE INDEX ux_usuarios_nombreusuario
    ON dbo.usuarios(nombreusuario);
END;

IF OBJECT_ID(N'dbo.sesionesusuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.sesionesusuario (
        idsesion UNIQUEIDENTIFIER NOT NULL,
        idusuario BIGINT NOT NULL,
        jti VARCHAR(64) NOT NULL,
        fechacreacion DATETIMEOFFSET NOT NULL,
        fechaexpiracion DATETIMEOFFSET NOT NULL,
        fecharevocacion DATETIMEOFFSET NULL,
        CONSTRAINT pk_sesionesusuario PRIMARY KEY (idsesion),
        CONSTRAINT fk_sesionesusuario_usuarios FOREIGN KEY (idusuario) REFERENCES dbo.usuarios(idusuario) ON DELETE CASCADE
    );
END;

IF COL_LENGTH(N'dbo.sesionesusuario', N'jti') IS NULL
BEGIN
    DELETE FROM dbo.sesionesusuario;
    ALTER TABLE dbo.sesionesusuario ADD jti VARCHAR(64) NOT NULL;
END;

IF COL_LENGTH(N'dbo.sesionesusuario', N'tokenhash') IS NOT NULL
BEGIN
    DELETE FROM dbo.sesionesusuario;
    ALTER TABLE dbo.sesionesusuario DROP COLUMN tokenhash;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_sesionesusuario_jti'
      AND object_id = OBJECT_ID(N'dbo.sesionesusuario')
)
BEGIN
    CREATE UNIQUE INDEX ux_sesionesusuario_jti
    ON dbo.sesionesusuario(jti);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_sesionesusuario_activa'
      AND object_id = OBJECT_ID(N'dbo.sesionesusuario')
)
BEGIN
    CREATE UNIQUE INDEX ux_sesionesusuario_activa
    ON dbo.sesionesusuario(idusuario)
    WHERE fecharevocacion IS NULL;
END;

-- 3. TICKETS
IF OBJECT_ID(N'dbo.tickets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tickets (
        idticket UNIQUEIDENTIFIER NOT NULL,
        codigocaso VARCHAR(20) NOT NULL,
        titulo VARCHAR(100) NOT NULL,
        descripcion VARCHAR(500) NOT NULL,
        fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_tickets_fechaasignacion DEFAULT (SYSDATETIMEOFFSET()),
        fechaultimaactualizacion DATETIMEOFFSET NULL,
        idusuarioasignado BIGINT NULL,
        idorigen INT NULL,
        idestado INT NOT NULL,
        carpetamedios VARCHAR(200) NULL,
        causaraiz VARCHAR(1000) NULL,
        solucionpropuesta VARCHAR(1000) NULL,
        esdesarrollo BIT NOT NULL CONSTRAINT df_tickets_esdesarrollo DEFAULT (0),
        nombrehu VARCHAR(100) NULL,
        urlhu VARCHAR(2048) NULL,
        activo BIT NOT NULL CONSTRAINT df_tickets_activo DEFAULT (1),
        fechaeliminacion DATETIMEOFFSET NULL,
        idusuarioeliminacion BIGINT NULL,
        CONSTRAINT pk_tickets PRIMARY KEY (idticket),
        CONSTRAINT fk_tickets_usuarios_asignado FOREIGN KEY (idusuarioasignado) REFERENCES dbo.usuarios(idusuario),
        CONSTRAINT fk_tickets_origenesticket FOREIGN KEY (idorigen) REFERENCES dbo.origenesticket(idorigen),
        CONSTRAINT fk_tickets_estadosticket FOREIGN KEY (idestado) REFERENCES dbo.estadosticket(idestado),
        CONSTRAINT fk_tickets_usuarios_eliminacion FOREIGN KEY (idusuarioeliminacion) REFERENCES dbo.usuarios(idusuario)
    );
END;

IF COL_LENGTH(N'dbo.tickets', N'idorigen') IS NULL
    ALTER TABLE dbo.tickets ADD idorigen INT NULL;

IF COL_LENGTH(N'dbo.tickets', N'activo') IS NULL
    ALTER TABLE dbo.tickets ADD activo BIT NOT NULL CONSTRAINT df_tickets_activo DEFAULT (1);

IF COL_LENGTH(N'dbo.tickets', N'fechaeliminacion') IS NULL
    ALTER TABLE dbo.tickets ADD fechaeliminacion DATETIMEOFFSET NULL;

IF COL_LENGTH(N'dbo.tickets', N'idusuarioeliminacion') IS NULL
    ALTER TABLE dbo.tickets ADD idusuarioeliminacion BIGINT NULL;

IF COL_LENGTH(N'dbo.tickets', N'esdesarrollo') IS NULL
    ALTER TABLE dbo.tickets ADD esdesarrollo BIT NOT NULL CONSTRAINT df_tickets_esdesarrollo DEFAULT (0);

IF COL_LENGTH(N'dbo.tickets', N'nombrehu') IS NULL
    ALTER TABLE dbo.tickets ADD nombrehu VARCHAR(100) NULL;

IF COL_LENGTH(N'dbo.tickets', N'urlhu') IS NULL
    ALTER TABLE dbo.tickets ADD urlhu VARCHAR(2048) NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_tickets_codigocaso'
      AND object_id = OBJECT_ID(N'dbo.tickets')
)
BEGIN
    CREATE UNIQUE INDEX ux_tickets_codigocaso
    ON dbo.tickets(codigocaso);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_tickets_activo_fechaasignacion'
      AND object_id = OBJECT_ID(N'dbo.tickets')
)
BEGIN
    CREATE INDEX ix_tickets_activo_fechaasignacion
    ON dbo.tickets(activo, fechaasignacion DESC);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_tickets_usuarioasignado_activo'
      AND object_id = OBJECT_ID(N'dbo.tickets')
)
BEGIN
    CREATE INDEX ix_tickets_usuarioasignado_activo
    ON dbo.tickets(idusuarioasignado, activo);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_tickets_estado_activo'
      AND object_id = OBJECT_ID(N'dbo.tickets')
)
BEGIN
    CREATE INDEX ix_tickets_estado_activo
    ON dbo.tickets(idestado, activo);
END;

IF OBJECT_ID(N'dbo.historicoestadosticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.historicoestadosticket (
        idhistorico UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_historicoestadosticket_idhistorico DEFAULT (NEWID()),
        idticket UNIQUEIDENTIFIER NOT NULL,
        idestadoorigen INT NULL,
        idestadodestino INT NOT NULL,
        idusuarioaccion BIGINT NOT NULL,
        comentario VARCHAR(1000) NULL,
        fechacambio DATETIMEOFFSET NOT NULL CONSTRAINT df_historicoestadosticket_fechacambio DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT pk_historicoestadosticket PRIMARY KEY (idhistorico),
        CONSTRAINT fk_historicoestadosticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
        CONSTRAINT fk_historicoestadosticket_estadoorigen FOREIGN KEY (idestadoorigen) REFERENCES dbo.estadosticket(idestado),
        CONSTRAINT fk_historicoestadosticket_estadodestino FOREIGN KEY (idestadodestino) REFERENCES dbo.estadosticket(idestado),
        CONSTRAINT fk_historicoestadosticket_usuarios FOREIGN KEY (idusuarioaccion) REFERENCES dbo.usuarios(idusuario)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_historicoestadosticket_idticket'
      AND object_id = OBJECT_ID(N'dbo.historicoestadosticket')
)
BEGIN
    CREATE INDEX ix_historicoestadosticket_idticket
    ON dbo.historicoestadosticket(idticket);
END;

-- 4. CONFIGURACION GIT
IF OBJECT_ID(N'dbo.repositorios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.repositorios (
        idrepositorio UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_repositorios_idrepositorio DEFAULT (NEWID()),
        repositorio VARCHAR(100) NOT NULL,
        link VARCHAR(255) NULL,
        descripcion VARCHAR(500) NULL,
        CONSTRAINT pk_repositorios PRIMARY KEY (idrepositorio)
    );
END;

IF OBJECT_ID(N'dbo.ramas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ramas (
        idrama UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_ramas_idrama DEFAULT (NEWID()),
        idrepositorio UNIQUEIDENTIFIER NOT NULL,
        nombrerama VARCHAR(150) NOT NULL,
        fechacreacion DATETIMEOFFSET NOT NULL CONSTRAINT df_ramas_fechacreacion DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT pk_ramas PRIMARY KEY (idrama),
        CONSTRAINT fk_ramas_repositorios FOREIGN KEY (idrepositorio) REFERENCES dbo.repositorios(idrepositorio) ON DELETE CASCADE
    );
END;

IF OBJECT_ID(N'dbo.ramasticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ramasticket (
        idramaticket UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_ramasticket_idramaticket DEFAULT (NEWID()),
        idticket UNIQUEIDENTIFIER NOT NULL,
        idrama UNIQUEIDENTIFIER NOT NULL,
        fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_ramasticket_fechaasignacion DEFAULT (SYSDATETIMEOFFSET()),
        CONSTRAINT pk_ramasticket PRIMARY KEY (idramaticket),
        CONSTRAINT fk_ramasticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
        CONSTRAINT fk_ramasticket_ramas FOREIGN KEY (idrama) REFERENCES dbo.ramas(idrama) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_repositorios_repositorio'
      AND object_id = OBJECT_ID(N'dbo.repositorios')
)
BEGIN
    CREATE UNIQUE INDEX ux_repositorios_repositorio
    ON dbo.repositorios(repositorio);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_ramas_repositorio_nombre'
      AND object_id = OBJECT_ID(N'dbo.ramas')
)
BEGIN
    CREATE UNIQUE INDEX ux_ramas_repositorio_nombre
    ON dbo.ramas(idrepositorio, nombrerama);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ux_ramasticket_ticket_rama'
      AND object_id = OBJECT_ID(N'dbo.ramasticket')
)
BEGIN
    CREATE UNIQUE INDEX ux_ramasticket_ticket_rama
    ON dbo.ramasticket(idticket, idrama);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_ramasticket_ticket'
      AND object_id = OBJECT_ID(N'dbo.ramasticket')
)
BEGIN
    CREATE INDEX ix_ramasticket_ticket
    ON dbo.ramasticket(idticket);
END;

-- 5. DATA INICIAL
MERGE dbo.roles AS target
USING (VALUES
    (1, 'Desarrollador', 'Ingeniero encargado del mantenimiento tecnico'),
    (2, 'QA', 'Analista de calidad y pruebas'),
    (3, 'LiderTecnico', 'Aprobador tecnico y administrador del flujo'),
    (4, 'Planner', 'Planeador y certificador de entregas')
) AS source (idrol, nombrerol, descripcion)
ON target.idrol = source.idrol
WHEN NOT MATCHED THEN
    INSERT (idrol, nombrerol, descripcion)
    VALUES (source.idrol, source.nombrerol, source.descripcion);

MERGE dbo.estadosticket AS target
USING (VALUES
    (1, 'EnAnalisis', 'El caso esta siendo revisado inicialmente'),
    (2, 'EnProceso', 'El desarrollador esta trabajando en la solucion'),
    (3, 'Bloqueado', 'El avance esta detenido por dependencia externa'),
    (4, 'Entregado', 'Desarrollo finalizado listo para primer despliegue'),
    (5, 'DespliegueApitesting', 'Desplegado en ambiente de pruebas de API'),
    (6, 'EnRevisionApitesting', 'QA o Dev revisando comportamiento de API'),
    (7, 'AprobadoApitesting', 'API validada con exito'),
    (8, 'DespligueQA', 'Listo o desplegado en ambiente formal de QA'),
    (9, 'EnRevisionQA', 'El equipo de QA esta ejecutando planes de prueba'),
    (10, 'AprobadoQA', 'Pruebas de QA aprobadas satisfactoriamente'),
    (11, 'PendienteCertificacion', 'En cola para aval del Planner'),
    (12, 'Certificado', 'Caso formalmente certificado para produccion'),
    (13, 'DespliegueProduccion', 'El cambio esta siendo liberado en vivo'),
    (14, 'BUG', 'Defecto encontrado en revisiones intermedias'),
    (15, 'Rollback', 'Reversion aplicada por fallos en despliegue')
) AS source (idestado, estado, descripcion)
ON target.idestado = source.idestado
WHEN NOT MATCHED THEN
    INSERT (idestado, estado, descripcion)
    VALUES (source.idestado, source.estado, source.descripcion);

MERGE dbo.origenesticket AS target
USING (VALUES
    (1, 'SAIA'),
    (2, 'GLPI')
) AS source (idorigen, origen)
ON target.idorigen = source.idorigen
WHEN NOT MATCHED THEN
    INSERT (idorigen, origen)
    VALUES (source.idorigen, source.origen);

MERGE dbo.areasticket AS target
USING (VALUES
    (1, 'Mantenimiento', ''),
    (2, 'Soporte', ''),
    (3, 'Vulnerabilidades', '')
) AS source (idarea, area, descripcion)
ON target.idarea = source.idarea
WHEN MATCHED THEN
    UPDATE SET
        area = source.area,
        descripcion = source.descripcion
WHEN NOT MATCHED THEN
    INSERT (idarea, area, descripcion)
    VALUES (source.idarea, source.area, source.descripcion);

COMMIT TRANSACTION;
GO
