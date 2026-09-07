SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'ix_historicoasignacionesticket_ticket_fecha'
          AND object_id = OBJECT_ID(N'dbo.historicoasignacionesticket'))
    BEGIN
        CREATE INDEX ix_historicoasignacionesticket_ticket_fecha
            ON dbo.historicoasignacionesticket(idticket, fechaasignacion DESC);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'ix_responsablesticket_usuario_ticket'
          AND object_id = OBJECT_ID(N'dbo.responsablesticket'))
    BEGIN
        CREATE INDEX ix_responsablesticket_usuario_ticket
            ON dbo.responsablesticket(idusuario, idticket);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
