SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

CREATE TABLE dbo.roles (
    idrol INT PRIMARY KEY,
    nombrerol VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_roles_activo DEFAULT (1)
);

CREATE TABLE dbo.estadosticket (
    idestado INT PRIMARY KEY,
    estado VARCHAR(50) NOT NULL,
    descripcion VARCHAR(500) NULL,
    activo BIT NOT NULL CONSTRAINT df_estadosticket_activo DEFAULT (1)
);

CREATE TABLE dbo.origenesticket (
    idorigen INT PRIMARY KEY,
    origen VARCHAR(100) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_origenesticket_activo DEFAULT (1)
);

CREATE TABLE dbo.areas (
    idarea INT PRIMARY KEY,
    area VARCHAR(100) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_areas_activo DEFAULT (1)
);

CREATE TABLE dbo.aplicativos (
    idaplicativo UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_aplicativos_idaplicativo DEFAULT NEWID(),
    aplicativo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_aplicativos_activo DEFAULT (1)
);

CREATE TABLE dbo.usuarios (
    idusuario BIGINT PRIMARY KEY,
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
    debecambiarcontrasena BIT NOT NULL CONSTRAINT df_usuarios_debecambiarcontrasena DEFAULT (0),
    fechabloqueo DATETIMEOFFSET NULL,
    fechacambiocontrasena DATETIMEOFFSET NULL,
    CONSTRAINT fk_usuarios_roles FOREIGN KEY (idrol) REFERENCES dbo.roles(idrol),
    CONSTRAINT fk_usuarios_areas FOREIGN KEY (idarea) REFERENCES dbo.areas(idarea)
);

CREATE UNIQUE INDEX ux_usuarios_nombreusuario ON dbo.usuarios(nombreusuario);

CREATE TABLE dbo.sesionesusuario (
    idsesion UNIQUEIDENTIFIER PRIMARY KEY,
    idusuario BIGINT NOT NULL,
    jti VARCHAR(64) NOT NULL,
    fechacreacion DATETIMEOFFSET NOT NULL,
    fechaexpiracion DATETIMEOFFSET NOT NULL,
    fecharevocacion DATETIMEOFFSET NULL,
    CONSTRAINT fk_sesionesusuario_usuarios FOREIGN KEY (idusuario) REFERENCES dbo.usuarios(idusuario) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_sesionesusuario_jti ON dbo.sesionesusuario(jti);
CREATE UNIQUE INDEX ux_sesionesusuario_activa
    ON dbo.sesionesusuario(idusuario)
    WHERE fecharevocacion IS NULL;

CREATE TABLE dbo.tickets (
    idticket UNIQUEIDENTIFIER PRIMARY KEY,
    codigocaso VARCHAR(20) NOT NULL,
    titulo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(500) NOT NULL,
    fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_tickets_fechaasignacion DEFAULT SYSDATETIMEOFFSET(),
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
    CONSTRAINT fk_tickets_usuarios_asignado FOREIGN KEY (idusuarioasignado) REFERENCES dbo.usuarios(idusuario),
    CONSTRAINT fk_tickets_origenesticket FOREIGN KEY (idorigen) REFERENCES dbo.origenesticket(idorigen),
    CONSTRAINT fk_tickets_estadosticket FOREIGN KEY (idestado) REFERENCES dbo.estadosticket(idestado),
    CONSTRAINT fk_tickets_usuarios_eliminacion FOREIGN KEY (idusuarioeliminacion) REFERENCES dbo.usuarios(idusuario)
);

CREATE UNIQUE INDEX ux_tickets_codigocaso ON dbo.tickets(codigocaso);
CREATE INDEX ix_tickets_activo_fechaasignacion ON dbo.tickets(activo, fechaasignacion DESC);
CREATE INDEX ix_tickets_usuarioasignado_activo ON dbo.tickets(idusuarioasignado, activo);
CREATE INDEX ix_tickets_estado_activo ON dbo.tickets(idestado, activo);

CREATE TABLE dbo.historicoestadosticket (
    idhistorico UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_historicoestadosticket_idhistorico DEFAULT NEWID(),
    idticket UNIQUEIDENTIFIER NOT NULL,
    idestadoorigen INT NULL,
    idestadodestino INT NOT NULL,
    idusuarioaccion BIGINT NOT NULL,
    comentario VARCHAR(1000) NULL,
    fechacambio DATETIMEOFFSET NOT NULL CONSTRAINT df_historicoestadosticket_fechacambio DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_historicoestadosticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
    CONSTRAINT fk_historicoestadosticket_estadoorigen FOREIGN KEY (idestadoorigen) REFERENCES dbo.estadosticket(idestado),
    CONSTRAINT fk_historicoestadosticket_estadodestino FOREIGN KEY (idestadodestino) REFERENCES dbo.estadosticket(idestado),
    CONSTRAINT fk_historicoestadosticket_usuarios FOREIGN KEY (idusuarioaccion) REFERENCES dbo.usuarios(idusuario)
);

CREATE INDEX ix_historicoestadosticket_idticket ON dbo.historicoestadosticket(idticket);

CREATE TABLE dbo.repositorios (
    idrepositorio UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_repositorios_idrepositorio DEFAULT NEWID(),
    repositorio VARCHAR(100) NOT NULL,
    link VARCHAR(255) NULL,
    descripcion VARCHAR(500) NULL
);

CREATE UNIQUE INDEX ux_repositorios_repositorio ON dbo.repositorios(repositorio);

CREATE TABLE dbo.ramas (
    idrama UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_ramas_idrama DEFAULT NEWID(),
    idrepositorio UNIQUEIDENTIFIER NOT NULL,
    nombrerama VARCHAR(150) NOT NULL,
    fechacreacion DATETIMEOFFSET NOT NULL CONSTRAINT df_ramas_fechacreacion DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_ramas_repositorios FOREIGN KEY (idrepositorio) REFERENCES dbo.repositorios(idrepositorio) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_ramas_repositorio_nombre ON dbo.ramas(idrepositorio, nombrerama);

CREATE TABLE dbo.ramasticket (
    idramaticket UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_ramasticket_idramaticket DEFAULT NEWID(),
    idticket UNIQUEIDENTIFIER NOT NULL,
    idrama UNIQUEIDENTIFIER NOT NULL,
    fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_ramasticket_fechaasignacion DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_ramasticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
    CONSTRAINT fk_ramasticket_ramas FOREIGN KEY (idrama) REFERENCES dbo.ramas(idrama) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_ramasticket_ticket_rama ON dbo.ramasticket(idticket, idrama);
CREATE INDEX ix_ramasticket_ticket ON dbo.ramasticket(idticket);

CREATE TABLE dbo.aplicativosticket (
    idaplicativoticket UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_aplicativosticket_idaplicativoticket DEFAULT NEWID(),
    idticket UNIQUEIDENTIFIER NOT NULL,
    idaplicativo UNIQUEIDENTIFIER NOT NULL,
    fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_aplicativosticket_fechaasignacion DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_aplicativosticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
    CONSTRAINT fk_aplicativosticket_aplicativos FOREIGN KEY (idaplicativo) REFERENCES dbo.aplicativos(idaplicativo) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_aplicativos_aplicativo ON dbo.aplicativos(aplicativo);
CREATE UNIQUE INDEX ux_aplicativosticket_ticket_aplicativo ON dbo.aplicativosticket(idticket, idaplicativo);
CREATE INDEX ix_aplicativosticket_ticket ON dbo.aplicativosticket(idticket);

CREATE TABLE dbo.repositoriosaplicativo (
    idrepositorioaplicativo UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_repositoriosaplicativo_idrepositorioaplicativo DEFAULT NEWID(),
    idrepositorio UNIQUEIDENTIFIER NOT NULL,
    idaplicativo UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT fk_repositoriosaplicativo_repositorios FOREIGN KEY (idrepositorio) REFERENCES dbo.repositorios(idrepositorio) ON DELETE CASCADE,
    CONSTRAINT fk_repositoriosaplicativo_aplicativos FOREIGN KEY (idaplicativo) REFERENCES dbo.aplicativos(idaplicativo) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_repositoriosaplicativo_repositorio_aplicativo
    ON dbo.repositoriosaplicativo(idrepositorio, idaplicativo);

INSERT INTO dbo.roles (idrol, nombrerol, descripcion, activo) VALUES
(1, 'Desarrollador', 'Ingeniero encargado del mantenimiento tecnico', 1),
(2, 'QA', 'Analista de calidad y pruebas', 1),
(3, 'LiderTecnico', 'Aprobador tecnico y administrador del flujo', 1),
(4, 'Planner', 'Planeador y certificador de entregas', 1);

INSERT INTO dbo.estadosticket (idestado, estado, descripcion, activo) VALUES
(1, 'EnAnalisis', 'El caso esta siendo revisado inicialmente', 1),
(2, 'EnProceso', 'El desarrollador esta trabajando en la solucion', 1),
(3, 'Bloqueado', 'El avance esta detenido por dependencia externa', 1),
(4, 'Entregado', 'Desarrollo finalizado listo para primer despliegue', 1),
(5, 'DespliegueApitesting', 'Desplegado en ambiente de pruebas de API', 1),
(6, 'EnRevisionApitesting', 'QA o Dev revisando comportamiento de API', 1),
(7, 'AprobadoApitesting', 'API validada con exito', 1),
(8, 'DespligueQA', 'Listo o desplegado en ambiente formal de QA', 1),
(9, 'EnRevisionQA', 'El equipo de QA esta ejecutando planes de prueba', 1),
(10, 'AprobadoQA', 'Pruebas de QA aprobadas satisfactoriamente', 1),
(11, 'PendienteCertificacion', 'En cola para aval del Planner', 1),
(12, 'Certificado', 'Caso formalmente certificado para produccion', 1),
(13, 'DespliegueProduccion', 'El cambio esta siendo liberado en vivo', 1),
(14, 'BUG', 'Defecto encontrado en revisiones intermedias', 1),
(15, 'Rollback', 'Reversion aplicada por fallos en despliegue', 1);

INSERT INTO dbo.origenesticket (idorigen, origen, descripcion, activo) VALUES
(1, 'SAIA', NULL, 1),
(2, 'GLPI', NULL, 1);

INSERT INTO dbo.areas (idarea, area, descripcion, activo) VALUES
(1, 'Mantenimiento', '', 1),
(2, 'Soporte', '', 1),
(3, 'Vulnerabilidades', '', 1);

COMMIT TRANSACTION;
GO
