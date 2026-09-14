ALTER TABLE sesionesusuario
    ADD COLUMN IF NOT EXISTS refreshtokenhash CHAR(64);
