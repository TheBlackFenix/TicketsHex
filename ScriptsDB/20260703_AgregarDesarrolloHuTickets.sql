SET SEARCH_PATH TO public;

ALTER TABLE IF EXISTS Tickets
ADD COLUMN IF NOT EXISTS EsDesarrollo BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE IF EXISTS Tickets
ADD COLUMN IF NOT EXISTS NombreHu VARCHAR(100);

ALTER TABLE IF EXISTS Tickets
ADD COLUMN IF NOT EXISTS UrlHu VARCHAR(2048);

UPDATE Tickets
SET NombreHu = NULL,
    UrlHu = NULL
WHERE NOT EsDesarrollo;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'ck_tickets_hudesarrollo'
          AND conrelid = 'tickets'::regclass
    ) THEN
        ALTER TABLE Tickets
        ADD CONSTRAINT CK_Tickets_HuDesarrollo CHECK (
            (NOT EsDesarrollo AND NombreHu IS NULL AND UrlHu IS NULL)
            OR
            (EsDesarrollo AND (
                (NombreHu IS NULL AND UrlHu IS NULL)
                OR
                (NombreHu IS NOT NULL AND UrlHu IS NOT NULL)
            ))
        );
    END IF;
END $$;
