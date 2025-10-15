using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Domain.Exceptions
{
    public class LoginCredentialsException(string message = "Invalid email or password") : Exception(message)
    {
    }
}