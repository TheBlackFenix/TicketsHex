SET XACT_ABORT ON;
GO

/*DROP DATABASE TicketFlow_PREDEV;
GO
CREATE DATABASE  TicketFlow_PREDEV;
GO
USE TicketFlow_PREDEV;
GO*/
IF SCHEMA_ID(N'dbo') IS NULL
    EXEC(N'CREATE SCHEMA dbo');
GO

BEGIN TRANSACTION;

-- 1. TABLAS MAESTRAS
IF OBJECT_ID(N'dbo.roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.roles (
        idrol INT PRIMARY KEY ,
        nombrerol VARCHAR(50) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.estadosticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.estadosticket (
        idestado INT PRIMARY KEY ,
        estado VARCHAR(50) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.origenesticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.origenesticket (
        idorigen INT PRIMARY KEY,
        origen VARCHAR(100) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.areasticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.areas (
        idarea INT PRIMARY KEY,
        area VARCHAR(100) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.aplicativos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.aplicativos (
        idaplicativo UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        aplicativo VARCHAR(100) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT DEFAULT (1)
    );
END;

-- 2. USUARIOS Y SESIONES
IF OBJECT_ID(N'dbo.usuarios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.usuarios (
        idusuario BIGINT PRIMARY KEY,
        nombreusuario VARCHAR(50) NOT NULL,
        nombres VARCHAR(100) NOT NULL,
        apellidos VARCHAR(100) NULL,
        idrol INT FOREIGN KEY REFERENCES dbo.roles(idrol),
        idarea INT NULL FOREIGN KEY REFERENCES dbo.areas(idarea),
        imagenperfilbase64 VARCHAR(MAX) NULL,
        activo BIT DEFAULT (1),
        contrasenahash VARCHAR(500) NULL,
        intentosfallidos INT DEFAULT (0),
        bloqueado BIT DEFAULT (0),
        fechabloqueo DATETIMEOFFSET NULL,
        fechacambiocontrasena DATETIMEOFFSET NULL,
    );
END;

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
        idsesion UNIQUEIDENTIFIER PRIMARY KEY ,
        idusuario BIGINT NOT NULL FOREIGN KEY REFERENCES dbo.usuarios(idusuario),
        jti VARCHAR(64) NOT NULL,
        fechacreacion DATETIMEOFFSET NOT NULL,
        fechaexpiracion DATETIMEOFFSET NOT NULL,
        fecharevocacion DATETIMEOFFSET NULL,
    );
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
        idticket UNIQUEIDENTIFIER PRIMARY KEY ,
        codigocaso VARCHAR(20) NOT NULL,
        titulo VARCHAR(100) NOT NULL,
        descripcion VARCHAR(500) NOT NULL,
        fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_tickets_fechaasignacion DEFAULT (SYSDATETIMEOFFSET()),
        fechaultimaactualizacion DATETIMEOFFSET NULL,
        idusuarioasignado BIGINT FOREIGN KEY REFERENCES dbo.usuarios(idusuario),
        idorigen INT FOREIGN KEY REFERENCES dbo.origenesticket(idorigen),
        idestado INT FOREIGN KEY REFERENCES dbo.estadosticket(idestado),
        carpetamedios VARCHAR(200) NULL,
        causaraiz VARCHAR(1000) NULL,
        solucionpropuesta VARCHAR(1000) NULL,
        esdesarrollo BIT NOT NULL CONSTRAINT df_tickets_esdesarrollo DEFAULT (0),
        nombrehu VARCHAR(100) NULL,
        urlhu VARCHAR(2048) NULL,
        activo BIT NOT NULL CONSTRAINT df_tickets_activo DEFAULT (1),
        fechaeliminacion DATETIMEOFFSET NULL,
        idusuarioeliminacion BIGINT FOREIGN KEY REFERENCES dbo.usuarios(idusuario)
    );
END;


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
        idhistorico UNIQUEIDENTIFIER DEFAULT (NEWID()) PRIMARY KEY ,
        idticket UNIQUEIDENTIFIER FOREIGN KEY  REFERENCES dbo.tickets(idticket) ,
        idestadoorigen INT FOREIGN KEY REFERENCES dbo.estadosticket(idestado),
        idestadodestino INT FOREIGN KEY REFERENCES dbo.estadosticket(idestado),
        idusuarioaccion BIGINT FOREIGN KEY REFERENCES dbo.usuarios(idusuario),
        comentario VARCHAR(1000) NULL,
        fechacambio DATETIMEOFFSET DEFAULT (SYSDATETIMEOFFSET()),
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
        idrepositorio UNIQUEIDENTIFIER DEFAULT (NEWID()) PRIMARY KEY,
        repositorio VARCHAR(100) NOT NULL,
        link VARCHAR(255) NULL,
        descripcion VARCHAR(500) NULL
    );
END;

IF OBJECT_ID(N'dbo.ramas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ramas (
        idrama UNIQUEIDENTIFIER DEFAULT (NEWID()) PRIMARY KEY ,
        idrepositorio UNIQUEIDENTIFIER FOREIGN KEY REFERENCES dbo.repositorios(idrepositorio),
        nombrerama VARCHAR(150) NOT NULL,
        fechacreacion DATETIMEOFFSET NOT NULL CONSTRAINT df_ramas_fechacreacion DEFAULT (SYSDATETIMEOFFSET()),
    );
END;

IF OBJECT_ID(N'dbo.ramasticket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ramasticket (
        idramaticket UNIQUEIDENTIFIER DEFAULT (NEWID()) PRIMARY KEY ,
        idticket UNIQUEIDENTIFIER FOREIGN KEY REFERENCES dbo.tickets(idticket),
        idrama UNIQUEIDENTIFIER FOREIGN KEY REFERENCES dbo.ramas(idrama),
        fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_ramasticket_fechaasignacion DEFAULT (SYSDATETIMEOFFSET()),
    );
END;

IF OBJECT_ID(N'dbo.repositoriosaplicativo', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.repositoriosaplicativo (
        idrepositorioaplicativo UNIQUEIDENTIFIER DEFAULT (NEWID()) PRIMARY KEY ,
        idrepositorio UNIQUEIDENTIFIER FOREIGN KEY REFERENCES dbo.repositorios(idrepositorio),
        idaplicativo UNIQUEIDENTIFIER FOREIGN KEY REFERENCES dbo.aplicativos(idaplicativo)
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

MERGE dbo.areas AS target
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
