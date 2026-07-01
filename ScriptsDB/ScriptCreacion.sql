SET SEARCH_PATH TO public;

-- 1. TABLAS MAESTRAS (OPCIONALES SI USAS ENUMS, PERO PREPARADAS SIN AUTOINCREMENTAL PARA CONTROLAR LOS IDS)
CREATE TABLE IF NOT EXISTS Roles (
    IdRol INT PRIMARY KEY,
    NombreRol VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(200)
);

CREATE TABLE IF NOT EXISTS EstadosTicket (
    IdEstado INT PRIMARY KEY,
    Estado VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(500),
    Activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS OrigenesTicket (
    IdOrigen INT PRIMARY KEY,
    Origen VARCHAR NOT NULL,
    Activo BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS AreasTicket (
    IdArea INT PRIMARY KEY,
    Area VARCHAR NOT NULL,
    Descripcion VARCHAR(200),
    Activo BOOLEAN DEFAULT TRUE
);


-- 2. ENTIDADES PRINCIPALES
CREATE TABLE IF NOT EXISTS Usuarios (
    IdUsuario BIGINT PRIMARY KEY, -- Mapeado de tu ID de sistema externo o Active Directory
    NombreUsuario VARCHAR(50) NOT NULL,
    Nombres VARCHAR(100) NOT NULL,
    Apellidos VARCHAR(100),
    IdRol INT REFERENCES Roles(IdRol),
    IdArea INT REFERENCES AreasTicket(IdArea),
    Activo BOOLEAN DEFAULT TRUE,
    ContrasenaHash VARCHAR(500),
    IntentosFallidos INT NOT NULL DEFAULT 0 CHECK (IntentosFallidos >= 0),
    Bloqueado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaBloqueo TIMESTAMPTZ,
    FechaCambioContrasena TIMESTAMPTZ
);

ALTER TABLE IF EXISTS Usuarios
ADD Column IF NOT EXISTS IdArea INT REFERENCES AreasTicket(IdArea);

ALTER TABLE IF EXISTS Usuarios
ADD COLUMN IF NOT EXISTS ContrasenaHash VARCHAR(500);

ALTER TABLE IF EXISTS Usuarios
ADD COLUMN IF NOT EXISTS IntentosFallidos INT NOT NULL DEFAULT 0;

ALTER TABLE IF EXISTS Usuarios
ADD COLUMN IF NOT EXISTS Bloqueado BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE IF EXISTS Usuarios
ADD COLUMN IF NOT EXISTS FechaBloqueo TIMESTAMPTZ;

ALTER TABLE IF EXISTS Usuarios
ADD COLUMN IF NOT EXISTS FechaCambioContrasena TIMESTAMPTZ;

CREATE UNIQUE INDEX IF NOT EXISTS UX_Usuarios_NombreUsuario
ON Usuarios(LOWER(NombreUsuario));

CREATE TABLE IF NOT EXISTS SesionesUsuario (
    IdSesion UUID PRIMARY KEY,
    IdUsuario BIGINT NOT NULL REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    TokenHash VARCHAR(64) NOT NULL UNIQUE,
    FechaCreacion TIMESTAMPTZ NOT NULL,
    FechaExpiracion TIMESTAMPTZ NOT NULL,
    FechaRevocacion TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_SesionesUsuario_Activa
ON SesionesUsuario(IdUsuario)
WHERE FechaRevocacion IS NULL;

CREATE INDEX IF NOT EXISTS IX_SesionesUsuario_TokenHash
ON SesionesUsuario(TokenHash);

CREATE TABLE IF NOT EXISTS Tickets (
    IdTicket UUID PRIMARY KEY,
    CodigoCaso VARCHAR(20) NOT NULL UNIQUE, -- Unificado semánticamente con tu CodigoCasoVO
    Titulo VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(500) NOT NULL,
    FechaAsignacion TIMESTAMPTZ NOT NULL DEFAULT NOW(), -- Uso de zonas horarias reales
    FechaUltimaActualizacion TIMESTAMPTZ,
    IdUsuarioAsignado BIGINT REFERENCES Usuarios(IdUsuario),
    IdOrigen INT REFERENCES OrigenesTicket(IdOrigen),
    IdEstado INT REFERENCES EstadosTicket(IdEstado) NOT NULL,
    CarpetaMedios VARCHAR(200),
    CausaRaiz VARCHAR(1000),
    SolucionPropuesta VARCHAR(1000),
    Activo BOOLEAN NOT NULL DEFAULT TRUE,
    FechaEliminacion TIMESTAMPTZ,
    IdUsuarioEliminacion BIGINT REFERENCES Usuarios(IdUsuario)
);

ALTER TABLE IF EXISTS Tickets
ADD Column IF NOT EXISTS IdOrigen INT REFERENCES OrigenesTicket(IdOrigen);

ALTER TABLE IF EXISTS Tickets
ADD COLUMN IF NOT EXISTS Activo BOOLEAN NOT NULL DEFAULT TRUE;

ALTER TABLE IF EXISTS Tickets
ADD COLUMN IF NOT EXISTS FechaEliminacion TIMESTAMPTZ;

ALTER TABLE IF EXISTS Tickets
ADD COLUMN IF NOT EXISTS IdUsuarioEliminacion BIGINT REFERENCES Usuarios(IdUsuario);

-- 3. TRAZABILIDAD (HISTÓRICO DE ESTADOS)
CREATE TABLE IF NOT EXISTS HistoricoEstadosTicket (
    IdHistorico UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    IdTicket UUID NOT NULL REFERENCES Tickets(IdTicket) ON DELETE CASCADE, -- ¡CORREGIDO! Relación indispensable
    IdEstadoOrigen INT REFERENCES EstadosTicket(IdEstado),
    IdEstadoDestino INT REFERENCES EstadosTicket(IdEstado) NOT NULL,
    IdUsuarioAccion BIGINT REFERENCES Usuarios(IdUsuario) NOT NULL,
    Comentario VARCHAR(1000),
    FechaCambio TIMESTAMPTZ NOT NULL DEFAULT NOW() -- ¡CORREGIDO! Sintaxis nativa Postgres
);

-- ÍNDICE DE CONTROL DE RENDIMIENTO PARA LA TRAZABILIDAD
CREATE INDEX IF NOT EXISTS IX_HistoricoEstadosTicket_IdTicket ON HistoricoEstadosTicket(IdTicket);
CREATE INDEX IF NOT EXISTS IX_Tickets_Activo_FechaAsignacion ON Tickets(Activo, FechaAsignacion DESC);
CREATE INDEX IF NOT EXISTS IX_Tickets_UsuarioAsignado_Activo ON Tickets(IdUsuarioAsignado, Activo);
CREATE INDEX IF NOT EXISTS IX_Tickets_Estado_Activo ON Tickets(IdEstado, Activo);


-- 4. GESTIÓN DE CONFIGURACIÓN Y GIT (REPOSITORIOS Y RAMAS)
CREATE TABLE IF NOT EXISTS Repositorios (
    IdRepositorio UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    Repositorio VARCHAR(100) NOT NULL, -- Ampliado un poco el tamaño
    Link VARCHAR(255),
    Descripcion VARCHAR(500)
);

CREATE TABLE IF NOT EXISTS Ramas (
    IdRama UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    IdRepositorio UUID NOT NULL REFERENCES Repositorios(IdRepositorio) ON DELETE CASCADE,
    NombreRama VARCHAR(150) NOT NULL, -- ¡AGREGADO! Esencial para saber qué rama es
    FechaCreacion TIMESTAMPTZ NOT NULL DEFAULT NOW() -- ¡CORREGIDO! Sintaxis nativa Postgres
);

CREATE TABLE IF NOT EXISTS RamasTicket (
    IdRamaTicket UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    IdTicket UUID NOT NULL REFERENCES Tickets(IdTicket) ON DELETE CASCADE,
    IdRama UUID NOT NULL REFERENCES Ramas(IdRama) ON DELETE CASCADE,
    FechaAsignacion TIMESTAMPTZ NOT NULL DEFAULT NOW() -- ¡CORREGIDO! Sintaxis nativa Postgres
);


-- 5. DATA INICIAL (SEED MASTER DATA COINCIDENTE CON TUS ENUMS DE C#)
INSERT INTO Roles (IdRol, NombreRol, Descripcion) VALUES
(1, 'Desarrollador', 'Ingeniero encargado del mantenimiento técnico'),
(2, 'QA', 'Analista de calidad y pruebas'),
(3, 'LiderTecnico', 'Aprobador técnico y administrador del flujo'),
(4, 'Planner', 'Planeador y certificador de entregas')
ON CONFLICT (IdRol) DO NOTHING ;

INSERT INTO EstadosTicket (IdEstado, Estado, Descripcion) VALUES
(1, 'EnAnalisis', 'El caso está siendo revisado inicialmente'),
(2, 'EnProceso', 'El desarrollador está trabajando en la solución'),
(3, 'Bloqueado', 'El avance está detenido por dependencia externa'),
(4, 'Entregado', 'Desarrollo finalizado listo para primer despliegue'),
(5, 'DespliegueApitesting', 'Desplegado en ambiente de pruebas de API'),
(6, 'EnRevisionApitesting', 'QA o Dev revisando comportamiento de API'),
(7, 'AprobadoApitesting', 'API validada con éxito'),
(8, 'DespligueQA', 'Listo o desplegado en ambiente formal de QA'),
(9, 'EnRevisionQA', 'El equipo de QA está ejecutando planes de prueba'),
(10, 'AprobadoQA', 'Pruebas de QA aprobadas satisfactoriamente'),
(11, 'PendienteCertificacion', 'En cola para aval del Planner'),
(12, 'Certificado', 'Caso formalmente certificado para producción'),
(13, 'DespliegueProduccion', 'El cambio está siendo liberado en vivo'),
(14, 'BUG', 'Defecto encontrado en revisiones intermedias'),
(15, 'Rollback', 'Reversión aplicada por fallos en despliegue')
ON CONFLICT (IdEstado) DO NOTHING ;

INSERT INTO OrigenesTicket(IdOrigen, Origen) VALUES
(1, 'SAIA'),
(2,'GLPI')
ON CONFLICT (IdOrigen) DO NOTHING ;

INSERT INTO AreasTicket(IdArea, Area, Descripcion) VALUES
(1, 'Mantenimiento',''),
(2, 'Soporte',''),
(3, 'Vulnerabilidades','')
ON CONFLICT (IdArea) DO UPDATE SET
Area = EXCLUDED.Area,
Descripcion = EXCLUDED.Descripcion ;
