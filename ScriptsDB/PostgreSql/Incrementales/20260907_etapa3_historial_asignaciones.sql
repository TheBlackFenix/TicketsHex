BEGIN;

CREATE INDEX IF NOT EXISTS ix_historicoasignacionesticket_ticket_fecha
    ON historicoasignacionesticket(idticket, fechaasignacion DESC);

CREATE INDEX IF NOT EXISTS ix_responsablesticket_usuario_ticket
    ON responsablesticket(idusuario, idticket);

COMMIT;
