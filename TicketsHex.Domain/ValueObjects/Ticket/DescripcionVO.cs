using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicketsHex.Domain.ValueObjects.Ticket
{
    public class DescripcionVO
    {
        public string Value { get; private set; }
        public DescripcionVO(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("La descripción no puede estar vacía.", nameof(value));
            if (value.Length > 500 || value.Length < 10)
                throw new ArgumentException("La descripción debe tener entre 10 y 500 caracteres.", nameof(value));
            Value = value;
        }
    }
}
