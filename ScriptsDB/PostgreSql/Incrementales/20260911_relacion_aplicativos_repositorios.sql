BEGIN;

CREATE TABLE IF NOT EXISTS tiposrepositorio (
    idtiporepositorio INT PRIMARY KEY,
    tipo VARCHAR(50) NOT NULL,
    descripcion VARCHAR(200),
    activo BOOLEAN NOT NULL DEFAULT TRUE
);

INSERT INTO tiposrepositorio (idtiporepositorio, tipo, descripcion, activo) VALUES
(1, 'Frontend', 'Repositorio de interfaz de usuario', TRUE),
(2, 'Backend', 'Repositorio de servicios y logica de servidor', TRUE),
(3, 'Scripts', 'Repositorio de scripts de datos, despliegue o automatizacion', TRUE)
ON CONFLICT (idtiporepositorio) DO UPDATE SET
    tipo = EXCLUDED.tipo,
    descripcion = EXCLUDED.descripcion,
    activo = EXCLUDED.activo;

ALTER TABLE repositorios
    ADD COLUMN IF NOT EXISTS idtiporepositorio INT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_repositorios_tiposrepositorio'
    ) THEN
        ALTER TABLE repositorios
            ADD CONSTRAINT fk_repositorios_tiposrepositorio
            FOREIGN KEY (idtiporepositorio)
            REFERENCES tiposrepositorio(idtiporepositorio);
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_repositorios_tiporepositorio
    ON repositorios(idtiporepositorio);

CREATE INDEX IF NOT EXISTS ix_repositoriosaplicativo_aplicativo
    ON repositoriosaplicativo(idaplicativo);

COMMIT;
