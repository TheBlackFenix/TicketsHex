SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.tiposrepositorio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tiposrepositorio (
        idtiporepositorio INT PRIMARY KEY,
        tipo VARCHAR(50) NOT NULL,
        descripcion VARCHAR(200) NULL,
        activo BIT NOT NULL CONSTRAINT df_tiposrepositorio_activo DEFAULT (1)
    );
END;

MERGE dbo.tiposrepositorio AS destino
USING (VALUES
    (1, 'Frontend', 'Repositorio de interfaz de usuario', 1),
    (2, 'Backend', 'Repositorio de servicios y logica de servidor', 1),
    (3, 'Scripts', 'Repositorio de scripts de datos, despliegue o automatizacion', 1)
) AS origen (idtiporepositorio, tipo, descripcion, activo)
ON destino.idtiporepositorio = origen.idtiporepositorio
WHEN MATCHED THEN
    UPDATE SET tipo = origen.tipo, descripcion = origen.descripcion, activo = origen.activo
WHEN NOT MATCHED THEN
    INSERT (idtiporepositorio, tipo, descripcion, activo)
    VALUES (origen.idtiporepositorio, origen.tipo, origen.descripcion, origen.activo);

IF COL_LENGTH(N'dbo.repositorios', N'idtiporepositorio') IS NULL
BEGIN
    ALTER TABLE dbo.repositorios ADD idtiporepositorio INT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'fk_repositorios_tiposrepositorio'
      AND parent_object_id = OBJECT_ID(N'dbo.repositorios')
)
BEGIN
    ALTER TABLE dbo.repositorios
        ADD CONSTRAINT fk_repositorios_tiposrepositorio
        FOREIGN KEY (idtiporepositorio)
        REFERENCES dbo.tiposrepositorio(idtiporepositorio);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_repositorios_tiporepositorio'
      AND object_id = OBJECT_ID(N'dbo.repositorios')
)
BEGIN
    CREATE INDEX ix_repositorios_tiporepositorio
        ON dbo.repositorios(idtiporepositorio);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'ix_repositoriosaplicativo_aplicativo'
      AND object_id = OBJECT_ID(N'dbo.repositoriosaplicativo')
)
BEGIN
    CREATE INDEX ix_repositoriosaplicativo_aplicativo
        ON dbo.repositoriosaplicativo(idaplicativo);
END;

COMMIT TRANSACTION;
GO
