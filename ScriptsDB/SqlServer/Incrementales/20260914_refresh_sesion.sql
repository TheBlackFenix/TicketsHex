SET XACT_ABORT ON;
GO

IF COL_LENGTH(N'dbo.sesionesusuario', N'refreshtokenhash') IS NULL
BEGIN
    ALTER TABLE dbo.sesionesusuario
        ADD refreshtokenhash CHAR(64) NULL;
END;
GO
