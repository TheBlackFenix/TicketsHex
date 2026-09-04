using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class TicketTests
{
    [Fact]
    public void Flujo_normal_respeta_secuencia_y_roles()
    {
        var ticket = CrearTicket();

        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.Entregado, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.DespliegueApitesting, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.EnRevisionApitesting, 3, Rol.QA, null);

        Assert.Equal(TicketEstado.EnRevisionApitesting, ticket.IdEstado);
        Assert.Equal(3, ticket.IdUsuarioAsignado);
    }

    [Fact]
    public void Desarrollador_no_puede_saltar_estados()
    {
        var ticket = CrearTicket();

        Assert.Throws<InvalidOperationException>(() =>
            ticket.ActualizarEstado(TicketEstado.Entregado, 2, Rol.Desarrollador, null));
    }

    [Fact]
    public void Lider_tecnico_puede_hacer_override_con_comentario()
    {
        var ticket = CrearTicket();

        ticket.ActualizarEstado(
            TicketEstado.PendienteCertificacion,
            1,
            Rol.LiderTecnico,
            "Flujo ejecutado por un equipo externo");

        Assert.Equal(TicketEstado.PendienteCertificacion, ticket.IdEstado);
        Assert.Throws<ArgumentException>(() =>
            CrearTicket().ActualizarEstado(
                TicketEstado.PendienteCertificacion,
                1,
                Rol.LiderTecnico,
                null));
    }

    [Fact]
    public void QA_no_puede_ejecutar_una_transicion_de_desarrollo()
    {
        var ticket = CrearTicket();

        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.ActualizarEstado(TicketEstado.EnProceso, 3, Rol.QA, null));
    }

    [Fact]
    public void QA_puede_validar_sin_ser_el_responsable_designado()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.Entregado, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.DespliegueApitesting, 2, Rol.Desarrollador, null);

        ticket.ActualizarEstado(TicketEstado.EnRevisionApitesting, 4, Rol.QA, null);

        Assert.Equal(3, ticket.IdUsuarioAsignado);
    }

    [Fact]
    public void Replica_QA_transfiere_custodia_y_regresa_a_desarrollo_con_comentario()
    {
        var ticket = CrearTicket();

        ticket.ActualizarEstado(TicketEstado.EnReplicaQA, 2, Rol.Desarrollador, null);
        Assert.Equal(3, ticket.IdUsuarioAsignado);

        ticket.ActualizarEstado(
            TicketEstado.EnAnalisis,
            3,
            Rol.QA,
            "Escenario replicado y documentado");

        Assert.Equal(2, ticket.IdUsuarioAsignado);
        Assert.Equal(TicketEstado.EnAnalisis, ticket.IdEstado);
    }

    [Fact]
    public void Replica_QA_no_permite_saltar_a_otro_estado_ni_con_override()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnReplicaQA, 2, Rol.Desarrollador, null);

        Assert.Throws<InvalidOperationException>(() =>
            ticket.ActualizarEstado(
                TicketEstado.EnProceso,
                1,
                Rol.LiderTecnico,
                "Intento de salto"));
    }

    [Fact]
    public void No_entra_a_etapa_QA_sin_responsable_QA()
    {
        var ticket = CrearTicket(incluirQa: false);

        var error = Assert.Throws<InvalidOperationException>(() =>
            ticket.ActualizarEstado(TicketEstado.EnReplicaQA, 2, Rol.Desarrollador, null));

        Assert.Contains("QA_NO_ASIGNADO", error.Message);
        Assert.Equal(TicketEstado.EnAnalisis, ticket.IdEstado);
    }

    [Theory]
    [InlineData(TicketEstado.Bloqueado)]
    [InlineData(TicketEstado.BUG)]
    [InlineData(TicketEstado.Rollback)]
    public void Estados_excepcionales_permiten_retomar_el_flujo(TicketEstado estadoExcepcional)
    {
        var ticket = CrearTicket();
        if (estadoExcepcional == TicketEstado.Bloqueado)
        {
            ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
            ticket.ActualizarEstado(estadoExcepcional, 2, Rol.Desarrollador, "Dependencia externa");
        }
        else
        {
            var rol = estadoExcepcional == TicketEstado.BUG ? Rol.QA : Rol.LiderTecnico;
            var usuario = estadoExcepcional == TicketEstado.BUG ? 3 : 1;
            ticket.ActualizarEstado(estadoExcepcional, usuario, rol, "Incidencia detectada");
        }

        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);

        Assert.Equal(TicketEstado.EnProceso, ticket.IdEstado);
        Assert.Equal(2, ticket.IdUsuarioAsignado);
    }

    [Fact]
    public void Solo_planner_o_lider_finalizan_desde_cualquier_estado_y_el_ticket_queda_terminal()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);

        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.ActualizarEstado(TicketEstado.Finalizado, 2, Rol.Desarrollador, "Error de usuario"));

        ticket.ActualizarEstado(TicketEstado.Finalizado, 1, Rol.Planner, "Error de usuario");

        Assert.Equal(TicketEstado.Finalizado, ticket.IdEstado);
        Assert.Throws<InvalidOperationException>(() =>
            ticket.AgregarComentarioLibre("Intento posterior", 1, Rol.Planner));
    }

    [Fact]
    public void Antiguo_asignado_no_puede_modificar_el_ticket()
    {
        var ticket = CrearTicket();
        ticket.AsignarResponsable(
            TipoResponsabilidadTicket.Desarrollo,
            5,
            1,
            Rol.Planner,
            "Cambio de desarrollador");

        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.ActualizarDescripcion(
                new Domain.ValueObjects.Ticket.DescripcionVO("Descripción actualizada por antiguo asignado"),
                2,
                Rol.Desarrollador));
    }

    [Fact]
    public void Reasignacion_funcional_actualiza_custodia_e_historial()
    {
        var ticket = CrearTicket();

        ticket.AsignarResponsable(
            TipoResponsabilidadTicket.Desarrollo,
            5,
            1,
            Rol.Planner,
            "Cambio de equipo");

        Assert.Equal(5, ticket.ObtenerIdResponsable(TipoResponsabilidadTicket.Desarrollo));
        Assert.Equal(5, ticket.IdUsuarioAsignado);
        var movimiento = ticket.HistoricoAsignaciones.OrderBy(item => item.FechaAsignacion).Last();
        Assert.Equal(2, movimiento.IdUsuarioAnterior);
        Assert.Equal(TipoMovimientoAsignacionTicket.ReasignacionDesarrollo, movimiento.IdTipoMovimiento);
    }

    [Fact]
    public void Campos_HU_son_exclusivos_de_planner_y_lider_tecnico()
    {
        var ticket = CrearTicket();

        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.ActualizarDatosDesarrollo(
                true,
                "HU-1234",
                "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
                null,
                2,
                Rol.Desarrollador));

        ticket.ActualizarDatosDesarrollo(
            true,
            "HU-1234",
            "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
            "medios/caso-001",
            1,
            Rol.Planner);

        Assert.Equal("HU-1234", ticket.NombreHu);
    }

    [Fact]
    public void Desarrollador_asignado_puede_actualizar_datos_tecnicos()
    {
        var ticket = CrearTicket();

        ticket.ActualizarDescripcion(
            new Domain.ValueObjects.Ticket.DescripcionVO("Descripción actualizada por responsable"),
            2,
            Rol.Desarrollador);
        ticket.ActualizarDiagnostico("Causa raíz", "Solución propuesta", 2, Rol.Desarrollador);

        Assert.Equal("Causa raíz", ticket.CausaRaiz);
    }

    [Fact]
    public void Solo_planner_elimina_ticket()
    {
        var ticket = CrearTicket();

        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.EliminarLogicamente(1, Rol.LiderTecnico, "Duplicado"));
        ticket.EliminarLogicamente(1, Rol.Planner, "Duplicado");

        Assert.False(ticket.Activo);
    }

    private static Ticket CrearTicket(bool incluirQa = true) => new(
        "CASO-001",
        "Título válido",
        "Descripción suficientemente larga",
        usuarioAsignado: 2,
        idUsuarioCreador: 1,
        TicketOrigen.SAIA,
        usuarioQa: incluirQa ? 3 : null);
}
