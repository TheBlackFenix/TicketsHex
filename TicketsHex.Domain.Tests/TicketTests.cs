using TicketsHex.Domain.Entidades.Ticket;
using TicketsHex.Domain.Enums;
using Xunit;

namespace TicketsHex.Domain.Tests;

public class TicketTests
{
    [Fact]
    public void Bloqueado_permite_pasar_a_cualquier_estado()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.Bloqueado, 2, Rol.Desarrollador, "Dependencia externa");

        ticket.ActualizarEstado(TicketEstado.Certificado, 2, Rol.QA, "Dependencia resuelta");

        Assert.Equal(TicketEstado.Certificado, ticket.IdEstado);
    }

    [Fact]
    public void Bug_permite_pasar_a_cualquier_estado()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.BUG, 3, Rol.QA, "Prueba fallida");

        ticket.ActualizarEstado(TicketEstado.DespliegueProduccion, 2, Rol.Desarrollador, "Corrección iniciada");

        Assert.Equal(TicketEstado.DespliegueProduccion, ticket.IdEstado);
    }

    [Fact]
    public void Rollback_permite_pasar_a_cualquier_estado()
    {
        var ticket = CrearTicket();
        ticket.ActualizarEstado(TicketEstado.Rollback, 1, Rol.QA, "Devolución");

        ticket.ActualizarEstado(
            TicketEstado.EnRevisionQA,
            1,
            Rol.Desarrollador,
            "Retorno desde rollback");

        Assert.Equal(TicketEstado.EnRevisionQA, ticket.IdEstado);
    }

    [Fact]
    public void Flujo_normal_de_estados_es_secuencial_sin_restriccion_de_rol()
    {
        var ticket = CrearTicket();

        ticket.ActualizarEstado(TicketEstado.EnProceso, 2, Rol.QA, null);
        ticket.ActualizarEstado(TicketEstado.Bloqueado, 2, Rol.Planner, "Bloqueo");
        ticket.ActualizarEstado(TicketEstado.Entregado, 2, Rol.Desarrollador, null);
        ticket.ActualizarEstado(TicketEstado.DespliegueApitesting, 2, Rol.QA, null);

        Assert.Equal(TicketEstado.DespliegueApitesting, ticket.IdEstado);
    }

    [Fact]
    public void No_permite_saltar_estados_en_flujo_normal()
    {
        var ticket = CrearTicket();

        Assert.Throws<InvalidOperationException>(() =>
            ticket.ActualizarEstado(TicketEstado.Entregado, 2, Rol.Planner, "Salto no permitido"));
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
    public void Puede_registrar_la_HU_en_un_ticket_de_desarrollo()
    {
        var ticket = CrearTicket();

        ticket.ActualizarDatosDesarrollo(
            true,
            "HU-1234",
            "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
            1,
            Rol.Desarrollador);

        Assert.True(ticket.EsDesarrollo);
        Assert.Equal("HU-1234", ticket.NombreHu);
        Assert.Equal(
            "https://dev.azure.com/equipo/proyecto/_workitems/edit/1234",
            ticket.UrlHu);
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
