using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketsHex.Domain.Enums;

namespace TicketsHex.Domain.ValueObjects.Ticket
{
    public class CodigoCasoVO
    {
        public string Valor { get; private set; }
        public CodigoCasoVO(int valor, TicketFuente fuente = TicketFuente.SAIA)
        {
            if (valor <= 0)
                throw new ArgumentException("El código de caso debe ser un número positivo.", nameof(valor));

            Valor = $"{fuente}-{valor}";
        }
    }
}
