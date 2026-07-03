using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class TicketTests
{
    [Fact]
    public void Permite_regresar_de_bloqueado_a_en_proceso()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.Bloqueado, 2, Rol.Desarrollador, "Dependencia externa");

        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, "Dependencia resuelta");

        Assert.Equal(TicketEstado.EnProceso, ticket.IdEstado);
    }

    [Fact]
    public void Permite_regresar_de_bug_a_en_proceso()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.BUG, 3, Rol.QA, "Prueba fallida");

        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, "Corrección iniciada");

        Assert.Equal(TicketEstado.EnProceso, ticket.IdEstado);
    }

    [Fact]
    public void Desarrollador_solo_modifica_campos_tecnicos()
    {
        var ticket = CrearTicket();

        ticket.ActualizarDiagnostico(
            "Causa raíz",
            "Solución propuesta",
            2,
            Rol.Desarrollador);

        Assert.Equal("Causa raíz", ticket.CausaRaiz);
        Assert.Equal("Solución propuesta", ticket.SolucionPropuesta);
        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.ActualizarTitulo("Título modificado", 2, Rol.Desarrollador));
    }

    [Fact]
    public void Planner_realiza_eliminacion_logica()
    {
        var ticket = CrearTicket();

        ticket.EliminarLogicamente(1, Rol.Planner, "Ticket duplicado");

        Assert.False(ticket.Activo);
        Assert.Equal(1, ticket.IdUsuarioEliminacion);
        Assert.NotNull(ticket.FechaEliminacion);
    }

    [Theory]
    [InlineData(Rol.Planner)]
    [InlineData(Rol.LiderTecnico)]
    public void Planner_y_lider_tecnico_pueden_cambiar_a_cualquier_estado(Rol rol)
    {
        var ticket = CrearTicket();

        ticket.ActualizarEstado(
            TicketEstado.DespliegueProduccion,
            1,
            rol,
            "Cambio administrativo");

        Assert.Equal(TicketEstado.DespliegueProduccion, ticket.IdEstado);

        ticket.ActualizarEstado(
            TicketEstado.EnAnalisis,
            1,
            rol,
            "Retorno administrativo");

        Assert.Equal(TicketEstado.EnAnalisis, ticket.IdEstado);
    }

    [Theory]
    [InlineData(Rol.Planner)]
    [InlineData(Rol.LiderTecnico)]
    public void Planner_y_lider_tecnico_pueden_reasignar_el_ticket(Rol rol)
    {
        var ticket = CrearTicket();

        ticket.ReasignarTicket(3, 1, rol, "Cambio de responsable");

        Assert.Equal(3, ticket.IdUsuarioAsignado);
    }

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Título válido",
        "Descripción suficientemente larga",
        usuarioAsignado: 2,
        idUsuarioCreador: 1,
        TicketOrigen.SAIA);
}
