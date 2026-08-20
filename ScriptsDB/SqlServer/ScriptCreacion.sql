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

CREATE TABLE dbo.historicoasignacionesticket (
    idhistoricoasignacion UNIQUEIDENTIFIER PRIMARY KEY CONSTRAINT df_historicoasignacionesticket_id DEFAULT NEWID(),
    idticket UNIQUEIDENTIFIER NOT NULL,
    idusuarioasignado BIGINT NOT NULL,
    idusuarioaccion BIGINT NOT NULL,
    comentario VARCHAR(1000) NULL,
    fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_historicoasignacionesticket_fecha DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_historicoasignacionesticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
    CONSTRAINT fk_historicoasignacionesticket_usuarioasignado FOREIGN KEY (idusuarioasignado) REFERENCES dbo.usuarios(idusuario),
    CONSTRAINT fk_historicoasignacionesticket_usuarioaccion FOREIGN KEY (idusuarioaccion) REFERENCES dbo.usuarios(idusuario)
);

CREATE INDEX ix_historicoasignacionesticket_usuario_ticket
    ON dbo.historicoasignacionesticket(idusuarioasignado, idticket);

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

CREATE TABLE dbo.tiposentradaconocimiento (
    idtipoentrada INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_tiposentradaconocimiento_activo DEFAULT (1)
);

CREATE TABLE dbo.resultadosentradaconocimiento (
    idresultado INT PRIMARY KEY,
    idtipoentrada INT NOT NULL,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_resultadosentradaconocimiento_activo DEFAULT (1),
    CONSTRAINT fk_resultadosentrada_tiposentrada FOREIGN KEY (idtipoentrada)
        REFERENCES dbo.tiposentradaconocimiento(idtipoentrada),
    CONSTRAINT ux_resultadosentrada_tipo_nombre UNIQUE (idtipoentrada, nombre),
    CONSTRAINT ux_resultadosentrada_resultado_tipo UNIQUE (idresultado, idtipoentrada)
);

CREATE TABLE dbo.ambientesticket (
    idambiente INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200) NULL,
    activo BIT NOT NULL CONSTRAINT df_ambientesticket_activo DEFAULT (1)
);

CREATE TABLE dbo.entradasconocimientoticket (
    identrada UNIQUEIDENTIFIER PRIMARY KEY,
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
    CONSTRAINT fk_entradasconocimiento_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
    CONSTRAINT fk_entradasconocimiento_tipos FOREIGN KEY (idtipoentrada) REFERENCES dbo.tiposentradaconocimiento(idtipoentrada),
    CONSTRAINT fk_entradasconocimiento_resultados FOREIGN KEY (idresultado, idtipoentrada)
        REFERENCES dbo.resultadosentradaconocimiento(idresultado, idtipoentrada),
    CONSTRAINT fk_entradasconocimiento_ambientes FOREIGN KEY (idambiente) REFERENCES dbo.ambientesticket(idambiente),
    CONSTRAINT fk_entradasconocimiento_usuarios FOREIGN KEY (idusuarioautor) REFERENCES dbo.usuarios(idusuario),
    CONSTRAINT fk_entradasconocimiento_roles FOREIGN KEY (idrolautor) REFERENCES dbo.roles(idrol)
);

CREATE INDEX ix_entradasconocimiento_ticket_fecha
    ON dbo.entradasconocimientoticket(idticket, fechacreacion DESC);
CREATE INDEX ix_entradasconocimiento_tipo_resultado
    ON dbo.entradasconocimientoticket(idtipoentrada, idresultado);

CREATE TABLE dbo.referenciasentradaconocimiento (
    idreferencia UNIQUEIDENTIFIER PRIMARY KEY,
    identrada UNIQUEIDENTIFIER NOT NULL,
    tiporeferencia INT NOT NULL,
    url VARCHAR(2048) NOT NULL,
    descripcion VARCHAR(300) NULL,
    CONSTRAINT fk_referenciasconocimiento_entradas FOREIGN KEY (identrada)
        REFERENCES dbo.entradasconocimientoticket(identrada) ON DELETE CASCADE
);

CREATE INDEX ix_referenciasconocimiento_entrada
    ON dbo.referenciasentradaconocimiento(identrada);

CREATE TABLE dbo.revisionesentradaconocimiento (
    idrevision UNIQUEIDENTIFIER PRIMARY KEY,
    identrada UNIQUEIDENTIFIER NOT NULL,
    contenidoanterior VARCHAR(MAX) NOT NULL,
    idusuarioaccion BIGINT NOT NULL,
    idrolusuarioaccion INT NOT NULL,
    idestadoticket INT NOT NULL,
    fecharevision DATETIMEOFFSET NOT NULL CONSTRAINT df_revisionesconocimiento_fecha DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_revisionesconocimiento_entradas FOREIGN KEY (identrada)
        REFERENCES dbo.entradasconocimientoticket(identrada) ON DELETE CASCADE,
    CONSTRAINT fk_revisionesconocimiento_usuarios FOREIGN KEY (idusuarioaccion) REFERENCES dbo.usuarios(idusuario),
    CONSTRAINT fk_revisionesconocimiento_roles FOREIGN KEY (idrolusuarioaccion) REFERENCES dbo.roles(idrol),
    CONSTRAINT fk_revisionesconocimiento_estados FOREIGN KEY (idestadoticket) REFERENCES dbo.estadosticket(idestado)
);

CREATE INDEX ix_revisionesconocimiento_entrada_fecha
    ON dbo.revisionesentradaconocimiento(identrada, fecharevision DESC);

CREATE TABLE dbo.tags (
    idtag UNIQUEIDENTIFIER PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    nombrenormalizado VARCHAR(50) NOT NULL,
    activo BIT NOT NULL CONSTRAINT df_tags_activo DEFAULT (1)
);

CREATE UNIQUE INDEX ux_tags_nombrenormalizado ON dbo.tags(nombrenormalizado);

CREATE TABLE dbo.tagsticket (
    idtagticket UNIQUEIDENTIFIER PRIMARY KEY,
    idticket UNIQUEIDENTIFIER NOT NULL,
    idtag UNIQUEIDENTIFIER NOT NULL,
    fechaasignacion DATETIMEOFFSET NOT NULL CONSTRAINT df_tagsticket_fecha DEFAULT SYSDATETIMEOFFSET(),
    CONSTRAINT fk_tagsticket_tickets FOREIGN KEY (idticket) REFERENCES dbo.tickets(idticket) ON DELETE CASCADE,
    CONSTRAINT fk_tagsticket_tags FOREIGN KEY (idtag) REFERENCES dbo.tags(idtag) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_tagsticket_ticket_tag ON dbo.tagsticket(idticket, idtag);

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

INSERT INTO dbo.tiposentradaconocimiento (idtipoentrada, nombre, descripcion, activo) VALUES
(1, 'Diagnostico', 'Hipotesis y comprobaciones tecnicas realizadas', 1),
(2, 'Solucion', 'Solucion planteada o implementada', 1),
(3, 'ValidacionQa', 'Validacion funcional realizada por QA', 1);

INSERT INTO dbo.resultadosentradaconocimiento (idresultado, idtipoentrada, nombre, descripcion, activo) VALUES
(1, 1, 'Confirmado', 'La hipotesis fue confirmada', 1),
(2, 1, 'Descartado', 'La hipotesis fue descartada', 1),
(3, 1, 'Inconcluso', 'No fue posible confirmar ni descartar la hipotesis', 1),
(4, 2, 'Exitosa', 'La solucion produjo el resultado esperado', 1),
(5, 2, 'Fallida', 'La solucion no produjo el resultado esperado', 1),
(6, 2, 'Parcial', 'La solucion resolvio parcialmente el caso', 1),
(7, 2, 'NoImplementada', 'La solucion no fue implementada', 1),
(8, 3, 'Aprobada', 'QA aprobo la validacion', 1),
(9, 3, 'Rechazada', 'QA rechazo la validacion', 1),
(10, 3, 'ConObservaciones', 'QA registro observaciones pendientes', 1);

INSERT INTO dbo.ambientesticket (idambiente, nombre, descripcion, activo) VALUES
(1, 'Local', 'Ambiente local del desarrollador', 1),
(2, 'Desarrollo', 'Ambiente compartido de desarrollo', 1),
(3, 'ApiTesting', 'Ambiente de pruebas de API', 1),
(4, 'QA', 'Ambiente formal de calidad', 1),
(5, 'Produccion', 'Ambiente productivo', 1);

COMMIT TRANSACTION;
GO
