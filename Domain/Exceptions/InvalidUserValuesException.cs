using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Domain.Exceptions
{
    public class InvalidUserValuesException : Exception
    {
        public List<string> Errors { get; }
        public InvalidUserValuesException(List<string> errors)
        {
            Errors = errors;
        }
    }
}