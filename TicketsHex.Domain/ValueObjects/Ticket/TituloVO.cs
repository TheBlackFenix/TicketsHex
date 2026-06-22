using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsHex.Domain.ValueObjects.Ticket
{
    public class TituloVO
    {
        public string Value { get; private set; }
        public TituloVO(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("El título no puede estar vacío.", nameof(value));
            if (value.Length > 100 || value.Length < 5)
                throw new ArgumentException("El título debe tener entre 5 y 100 caracteres.", nameof(value));
            Value = value;
        }
    }
}
