using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Domain.Exceptions
{
    public class InvalidPageNumberException : Exception
    {
        public int ProvidedValue { get; }
        public InvalidPageNumberException(int providedValue, string? message) : base(message)
        {
            ProvidedValue = providedValue;
        }
    }
}