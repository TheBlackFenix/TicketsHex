using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsHex.Domain.Enums
{
    public enum TicketEstado
    {
        EnAnalisis = 1,
        EnProceso = 2,
        Bloqueado = 3,
        Entregado = 4,
        DespliegueApitesting = 5,
        EnRevisionApitesting = 6,
        AprobadoApitesting = 7,
        DespligueQA = 8,
        EnRevisionQA = 9,
        AprobadoQA = 10,
        PendienteCertificacion = 11,
        Certificado = 12,
        DespliegueProduccion = 13,
        BUG = 14,
        Rollback = 15
    }
}
