using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Domain.Exceptions
{
    public class InvalidParameterException : Exception
    {
        public int ProvidedValue { get; }
        public InvalidParameterException(int providedValue, string? message) : base(message)
        {
            ProvidedValue = providedValue;
        }
    }
}