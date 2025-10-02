using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Domain.ModelViews
{
    public class ErrorValidation
    {
        public List<string> Messages { get; set; } = new List<string>();
    }
}