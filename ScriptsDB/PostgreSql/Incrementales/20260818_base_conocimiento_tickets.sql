BEGIN;

CREATE TABLE IF NOT EXISTS tiposentradaconocimiento (
    idtipoentrada INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS resultadosentradaconocimiento (
    idresultado INT PRIMARY KEY,
    idtipoentrada INT NOT NULL REFERENCES tiposentradaconocimiento(idtipoentrada),
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT ux_resultadosentrada_tipo_nombre UNIQUE (idtipoentrada, nombre),
    CONSTRAINT ux_resultadosentrada_resultado_tipo UNIQUE (idresultado, idtipoentrada)
);

CREATE TABLE IF NOT EXISTS ambientesticket (
    idambiente INT PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS entradasconocimientoticket (
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

CREATE INDEX IF NOT EXISTS ix_entradasconocimiento_ticket_fecha
    ON entradasconocimientoticket(idticket, fechacreacion DESC);
CREATE INDEX IF NOT EXISTS ix_entradasconocimiento_tipo_resultado
    ON entradasconocimientoticket(idtipoentrada, idresultado);

CREATE TABLE IF NOT EXISTS referenciasentradaconocimiento (
    idreferencia UUID PRIMARY KEY,
    identrada UUID NOT NULL REFERENCES entradasconocimientoticket(identrada) ON DELETE CASCADE,
    tiporeferencia INT NOT NULL,
    url VARCHAR(2048) NOT NULL,
    descripcion VARCHAR(300)
);

CREATE INDEX IF NOT EXISTS ix_referenciasconocimiento_entrada
    ON referenciasentradaconocimiento(identrada);

CREATE TABLE IF NOT EXISTS revisionesentradaconocimiento (
    idrevision UUID PRIMARY KEY,
    identrada UUID NOT NULL REFERENCES entradasconocimientoticket(identrada) ON DELETE CASCADE,
    contenidoanterior TEXT NOT NULL,
    idusuarioaccion BIGINT NOT NULL REFERENCES usuarios(idusuario),
    idrolusuarioaccion INT NOT NULL REFERENCES roles(idrol),
    idestadoticket INT NOT NULL REFERENCES estadosticket(idestado),
    fecharevision TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_revisionesconocimiento_entrada_fecha
    ON revisionesentradaconocimiento(identrada, fecharevision DESC);

CREATE TABLE IF NOT EXISTS tags (
    idtag UUID PRIMARY KEY,
    nombre VARCHAR(50) NOT NULL,
    nombrenormalizado VARCHAR(50) NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_tags_nombrenormalizado ON tags(nombrenormalizado);

CREATE TABLE IF NOT EXISTS tagsticket (
    idtagticket UUID PRIMARY KEY,
    idticket UUID NOT NULL REFERENCES tickets(idticket) ON DELETE CASCADE,
    idtag UUID NOT NULL REFERENCES tags(idtag) ON DELETE CASCADE,
    fechaasignacion TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_tagsticket_ticket_tag ON tagsticket(idticket, idtag);

INSERT INTO tiposentradaconocimiento (idtipoentrada, nombre, descripcion, activo) VALUES
(1, 'Diagnostico', 'Hipotesis y comprobaciones tecnicas realizadas', TRUE),
(2, 'Solucion', 'Solucion planteada o implementada', TRUE),
(3, 'ValidacionQa', 'Validacion funcional realizada por QA', TRUE)
ON CONFLICT (idtipoentrada) DO NOTHING;

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
(10, 3, 'ConObservaciones', 'QA registro observaciones pendientes', TRUE)
ON CONFLICT (idresultado) DO NOTHING;

INSERT INTO ambientesticket (idambiente, nombre, descripcion, activo) VALUES
(1, 'Local', 'Ambiente local del desarrollador', TRUE),
(2, 'Desarrollo', 'Ambiente compartido de desarrollo', TRUE),
(3, 'ApiTesting', 'Ambiente de pruebas de API', TRUE),
(4, 'QA', 'Ambiente formal de calidad', TRUE),
(5, 'Produccion', 'Ambiente productivo', TRUE)
ON CONFLICT (idambiente) DO NOTHING;

COMMIT;
