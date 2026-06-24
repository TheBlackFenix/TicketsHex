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
        public CodigoCasoVO(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("El código de caso no puede estar vacío.", nameof(valor));

            Valor = valor;
        }
    }
}
