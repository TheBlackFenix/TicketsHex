IF COL_LENGTH(N'dbo.usuarios', N'debecambiarcontrasena') IS NULL
BEGIN
    ALTER TABLE dbo.usuarios
    ADD debecambiarcontrasena BIT NOT NULL
        CONSTRAINT df_usuarios_debecambiarcontrasena DEFAULT (0);
END;
GO
