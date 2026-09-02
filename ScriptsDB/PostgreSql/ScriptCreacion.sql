CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE roles (
    idrol INT PRIMARY KEY,
    nombrerol VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE estadosticket (
    idestado INT PRIMARY KEY,
    estado VARCHAR(50) NOT NULL,
    descripcion VARCHAR(500),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE origenesticket (
    idorigen INT PRIMARY KEY,
    origen VARCHAR(100) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE areas (
    idarea INT PRIMARY KEY,
    area VARCHAR(100) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE aplicativos (
    idaplicativo UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    aplicativo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE usuarios (
    idusuario BIGINT PRIMARY KEY,
    nombreusuario VARCHAR(50) NOT NULL,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100),
    idrol INT REFERENCES roles(idrol),
    idarea INT REFERENCES areas(idarea),
    imagenperfilbase64 TEXT,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    contrasenahash VARCHAR(500),
    intentosfallidos INT NOT NULL DEFAULT 0,
    bloqueado BOOLEAN NOT NULL DEFAULT FALSE,
    debecambiarcontrasena BOOLEAN NOT NULL DEFAULT FALSE,
    fechabloqueo TIMESTAMPTZ,
    fechacambiocontrasena TIMESTAMPTZ
);

CREATE UNIQUE INDEX ux_usuarios_nombreusuario ON usuarios(nombreusuario);

CREATE TABLE sesionesusuario (
    idsesion UUID PRIMARY KEY,
    idusuario BIGINT NOT NULL REFERENCES usuarios(idusuario) ON DELETE CASCADE,
    jti VARCHAR(64) NOT NULL,
    fechacreacion TIMESTAMPTZ NOT NULL,
    fechaexpiracion TIMESTAMPTZ NOT NULL,
    fecharevocacion TIMESTAMPTZ
);

CREATE UNIQUE INDEX ux_sesionesusuario_jti ON sesionesusuario(jti);
CREATE UNIQUE INDEX ux_sesionesusuario_activa
    ON sesionesusuario(idusuario)
    WHERE fecharevocacion IS NULL;

CREATE TABLE tickets (
    idticket UUID PRIMARY KEY,
    codigocaso VARCHAR(20) NOT NULL,
    titulo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(500) NOT NULL,
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fechaultimaactualizacion TIMESTAMPTZ,
    idusuarioasignado BIGINT REFERENCES usuarios(idusuario),
    idorigen INT REFERENCES origenesticket(idorigen),
    idestado INT NOT NULL REFERENCES estadosticket(idestado),
    carpetamedios VARCHAR(200),
    causaraiz VARCHAR(1000),
    solucionpropuesta VARCHAR(1000),
    esdesarrollo BOOLEAN NOT NULL DEFAULT FALSE,
    nombrehu VARCHAR(100),
    urlhu VARCHAR(2048),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fechaeliminacion TIMESTAMPTZ,
    idusuarioeliminacion BIGINT REFERENCES usuarios(idusuario)
);

CREATE UNIQUE INDEX ux_tickets_codigocaso ON tickets(codigocaso);
CREATE INDEX ix_tickets_activo_fechaasignacion ON tickets(activo, fechaasignacion DESC);
CREATE INDEX ix_tickets_usuarioasignado_activo ON tickets(idusuarioasignado, activo);
CREATE INDEX ix_tickets_estado_activo ON tickets(idestado, activo);

CREATE TABLE historicoestadosticket (
    idhistorico UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idestadoorigen INT REFERENCES estadosticket(idestado),
    idestadodestino INT NOT NULL REFERENCES estadosticket(idestado),
    idusuarioaccion BIGINT NOT NULL REFERENCES usuarios(idusuario),
    comentario VARCHAR(1000),
    fechacambio TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_historicoestadosticket_idticket ON historicoestadosticket(idticket);

CREATE TABLE historicoasignacionesticket (
    idhistoricoasignacion UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idusuarioasignado BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idusuarioaccion BIGINT NOT NULL REFERENCES usuarios(idusuario),
    comentario VARCHAR(1000),
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_historicoasignacionesticket_usuario_ticket
    ON historicoasignacionesticket(idusuarioasignado, idticket);

CREATE TABLE repositorios (
    idrepositorio UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    repositorio VARCHAR(100) NOT NULL,
    link VARCHAR(255),
    descripcion VARCHAR(500)
);

CREATE UNIQUE INDEX ux_repositorios_repositorio ON repositorios(repositorio);

CREATE TABLE ramas (
    idrama UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idrepositorio UUID NOT NULL REFERENCES repositorios(idrepositorio) ON DELETE CASCADE,
    nombrerama VARCHAR(150) NOT NULL,
    fechacreacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ux_ramas_repositorio_nombre ON ramas(idrepositorio, nombrerama);

CREATE TABLE ramasticket (
    idramaticket UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idrama UUID NOT NULL REFERENCES ramas(idrama) ON DELETE CASCADE,
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ux_ramasticket_ticket_rama ON ramasticket(idticket, idrama);
CREATE INDEX ix_ramasticket_ticket ON ramasticket(idticket);

CREATE TABLE aplicativosticket (
    idaplicativoticket UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idaplicativo UUID NOT NULL REFERENCES aplicativos(idaplicativo) ON DELETE CASCADE,
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ux_aplicativos_aplicativo ON aplicativos(aplicativo);
CREATE UNIQUE INDEX ux_aplicativosticket_ticket_aplicativo ON aplicativosticket(idticket, idaplicativo);
CREATE INDEX ix_aplicativosticket_ticket ON aplicativosticket(idticket);

CREATE TABLE repositoriosaplicativo (
    idrepositorioaplicativo UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idrepositorio UUID NOT NULL REFERENCES repositorios(idrepositorio) ON DELETE CASCADE,
    idaplicativo UUID NOT NULL REFERENCES aplicativos(idaplicativo) ON DELETE CASCADE
);

CREATE UNIQUE INDEX ux_repositoriosaplicativo_repositorio_aplicativo
    ON repositoriosaplicativo(idrepositorio, idaplicativo);

CREATE TABLE tiposentradaconocimiento (
    idtipoentrada INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE resultadosentradaconocimiento (
    idresultado INT PRIMARY KEY,
    idtipoentrada INT NOT NULL REFERENCES tiposentradaconocimiento(idtipoentrada),
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT ux_resultadosentrada_tipo_nombre UNIQUE (idtipoentrada, nombre),
    CONSTRAINT ux_resultadosentrada_resultado_tipo UNIQUE (idresultado, idtipoentrada)
);

CREATE TABLE ambientesticket (
    idambiente INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE entradasconocimientoticket (
    identrada UUID PRIMARY KEY,
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idtipoentrada INT NOT NULL REFERENCES tiposentradaconocimiento(idtipoentrada),
    idresultado INT NOT NULL,
    resumen VARCHAR(2000) NOT NULL,
    sintomas VARCHAR(2000),
    comprobaciones VARCHAR(4000),
    pasosreproduccion VARCHAR(4000),
    idambiente INT REFERENCES ambientesticket(idambiente),
    requieredespliegue BOOLEAN,
    observaciones VARCHAR(2000),
    idusuarioautor BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idrolautor INT NOT NULL REFERENCES roles(idrol),
    fechacreacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fechaultimaactualizacion TIMESTAMPTZ,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_entradasconocimiento_resultados FOREIGN KEY (idresultado, idtipoentrada)
        REFERENCES resultadosentradaconocimiento(idresultado, idtipoentrada)
);

CREATE INDEX ix_entradasconocimiento_ticket_fecha
    ON entradasconocimientoticket(idticket, fechacreacion DESC);
CREATE INDEX ix_entradasconocimiento_tipo_resultado
    ON entradasconocimientoticket(idtipoentrada, idresultado);

CREATE TABLE referenciasentradaconocimiento (
    idreferencia UUID PRIMARY KEY,
    identrada UUID NOT NULL REFERENCES entradasconocimientoticket(identrada) ON DELETE CASCADE,
    tiporeferencia INT NOT NULL,
    url VARCHAR(2048) NOT NULL,
    descripcion VARCHAR(300)
);

CREATE INDEX ix_referenciasconocimiento_entrada
    ON referenciasentradaconocimiento(identrada);

CREATE TABLE revisionesentradaconocimiento (
    idrevision UUID PRIMARY KEY,
    identrada UUID NOT NULL REFERENCES entradasconocimientoticket(identrada) ON DELETE CASCADE,
    contenidoanterior TEXT NOT NULL,
    idusuarioaccion BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idrolusuarioaccion INT NOT NULL REFERENCES roles(idrol),
    idestadoticket INT NOT NULL REFERENCES estadosticket(idestado),
    fecharevision TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_revisionesconocimiento_entrada_fecha
    ON revisionesentradaconocimiento(identrada, fecharevision DESC);

CREATE TABLE tags (
    idtag UUID PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    nombrenormalizado VARCHAR(50) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX ux_tags_nombrenormalizado ON tags(nombrenormalizado);

CREATE TABLE tagsticket (
    idtagticket UUID PRIMARY KEY,
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idtag UUID NOT NULL REFERENCES tags(idtag) ON DELETE CASCADE,
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX ux_tagsticket_ticket_tag ON tagsticket(idticket, idtag);

INSERT INTO roles (idrol, nombrerol, descripcion, activo) VALUES
(1, 'Desarrollador', 'Ingeniero encargado del mantenimiento tecnico', TRUE),
(2, 'QA', 'Analista de calidad y pruebas', TRUE),
(3, 'LiderTecnico', 'Aprobador tecnico y administrador del flujo', TRUE),
(4, 'Planner', 'Planeador y certificador de entregas', TRUE);

INSERT INTO estadosticket (idestado, estado, descripcion, activo) VALUES
(1, 'EnAnalisis', 'El caso esta siendo revisado inicialmente', TRUE),
(2, 'EnProceso', 'El desarrollador esta trabajando en la solucion', TRUE),
(3, 'Bloqueado', 'El avance esta detenido por dependencia externa', TRUE),
(4, 'Entregado', 'Desarrollo finalizado listo para primer despliegue', TRUE),
(5, 'DespliegueApitesting', 'Desplegado en ambiente de pruebas de API', TRUE),
(6, 'EnRevisionApitesting', 'QA o Dev revisando comportamiento de API', TRUE),
(7, 'AprobadoApitesting', 'API validada con exito', TRUE),
(8, 'DespligueQA', 'Listo o desplegado en ambiente formal de QA', TRUE),
(9, 'EnRevisionQA', 'El equipo de QA esta ejecutando planes de prueba', TRUE),
(10, 'AprobadoQA', 'Pruebas de QA aprobadas satisfactoriamente', TRUE),
(11, 'PendienteCertificacion', 'En cola para aval del Planner', TRUE),
(12, 'Certificado', 'Caso formalmente certificado para produccion', TRUE),
(13, 'DespliegueProduccion', 'El cambio esta siendo liberado en vivo', TRUE),
(14, 'BUG', 'Defecto encontrado en revisiones intermedias', TRUE),
(15, 'Rollback', 'Reversion aplicada por fallos en despliegue', TRUE);

INSERT INTO origenesticket (idorigen, origen, descripcion, activo) VALUES
(1, 'SAIA', NULL, TRUE),
(2, 'GLPI', NULL, TRUE);

INSERT INTO areas (idarea, area, descripcion, activo) VALUES
(1, 'Mantenimiento', '', TRUE),
(2, 'Soporte', '', TRUE),
(3, 'Vulnerabilidades', '', TRUE);

INSERT INTO tiposentradaconocimiento (idtipoentrada, nombre, descripcion, activo) VALUES
(1, 'Diagnostico', 'Hipotesis y comprobaciones tecnicas realizadas', TRUE),
(2, 'Solucion', 'Solucion planteada o implementada', TRUE),
(3, 'ValidacionQa', 'Validacion funcional realizada por QA', TRUE);

INSERT INTO resultadosentradaconocimiento (idresultado, idtipoentrada, nombre, descripcion, activo) VALUES
(1, 1, 'Confirmado', 'La hipotesis fue confirmada', TRUE),
(2, 1, 'Descartado', 'La hipotesis fue descartada', TRUE),
(3, 1, 'Inconcluso', 'No fue posible confirmar ni descartar la hipotesis', TRUE),
(4, 2, 'Exitosa', 'La solucion produjo el resultado esperado', TRUE),
(5, 2, 'Fallida', 'La solucion no produjo el resultado esperado', TRUE),
(6, 2, 'Parcial', 'La solucion resolvio parcialmente el caso', TRUE),
(7, 2, 'NoImplementada', 'La solucion no fue implementada', TRUE),
(8, 3, 'Aprobada', 'QA aprobo la validacion', TRUE),
(9, 3, 'Rechazada', 'QA rechazo la validacion', TRUE),
(10, 3, 'ConObservaciones', 'QA registro observaciones pendientes', TRUE);

INSERT INTO ambientesticket (idambiente, nombre, descripcion, activo) VALUES
(1, 'Local', 'Ambiente local del desarrollador', TRUE),
(2, 'Desarrollo', 'Ambiente compartido de desarrollo', TRUE),
(3, 'ApiTesting', 'Ambiente de pruebas de API', TRUE),
(4, 'QA', 'Ambiente formal de calidad', TRUE),
(5, 'Produccion', 'Ambiente productivo', TRUE);
