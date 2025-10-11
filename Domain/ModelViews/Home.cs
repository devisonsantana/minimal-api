namespace minimal_api.Domain.ModelViews
{
    public struct Home
    {
        public string Message => "Welcome to Vehicle Minimal API, feel free to test our endpoints, some requests require a token authentication";
        public string Documentation => "/swagger/index.html";
    }
}