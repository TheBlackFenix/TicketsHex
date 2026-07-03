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

    [Fact]
    public void Ticket_inicia_sin_datos_de_HU_y_como_no_desarrollo_por_defecto()
    {
        var ticket = CrearTicket();

        Assert.False(ticket.EsDesarrollo);
        Assert.Null(ticket.NombreHu);
        Assert.Null(ticket.UrlHu);
    }

    [Fact]
    public void Planner_registra_la_HU_en_un_ticket_de_desarrollo()
    {
        var ticket = CrearTicket();

        ticket.ActualizarDatosDesarrollo(
            true,
            "HU-1234",
            "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
            1,
            Rol.Planner);

        Assert.True(ticket.EsDesarrollo);
        Assert.Equal("HU-1234", ticket.NombreHu);
        Assert.Equal(
            "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
            ticket.UrlHu);
    }

    [Theory]
    [InlineData(Rol.Desarrollador)]
    [InlineData(Rol.QA)]
    [InlineData(Rol.LiderTecnico)]
    public void Solo_planner_puede_registrar_la_HU(Rol rol)
    {
        var ticket = CrearTicket();

        Assert.Throws<UnauthorizedAccessException>(() =>
            ticket.ActualizarDatosDesarrollo(
                true,
                "HU-1234",
                "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
                1,
                rol));
    }

    [Fact]
    public void No_permite_registrar_HU_si_no_es_desarrollo()
    {
        var ticket = CrearTicket();

        Assert.Throws<InvalidOperationException>(() =>
            ticket.ActualizarDatosDesarrollo(
                false,
                "HU-1234",
                "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
                1,
                Rol.Planner));
    }

    [Fact]
    public void Nombre_y_url_de_HU_deben_registrarse_juntos()
    {
        var ticket = CrearTicket();

        Assert.Throws<ArgumentException>(() =>
            ticket.ActualizarDatosDesarrollo(
                true,
                "HU-1234",
                null,
                1,
                Rol.Planner));
    }

    private static Ticket CrearTicket() => new(
        "CASO-001",
        "Título válido",
        "Descripción suficientemente larga",
        usuarioAsignado: 2,
        idUsuarioCreador: 1,
        TicketOrigen.SAIA);
}
