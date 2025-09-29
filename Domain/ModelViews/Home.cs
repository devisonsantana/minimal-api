using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace minimal_api.Domain.ModelViews
{
    public struct Home
    {
        public string Message { get => "Welcome to Vehicle Minimal API, feel free to test our endpoints, some requests require a token authentication"; }
        public string Documentation { get => "/swagger/index.html"; }
    }
}