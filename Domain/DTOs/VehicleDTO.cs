namespace minimal_api.Domain.DTOs
{
    public record VehicleDTO
    {
        public string Name { get; set; } = default;
        public string Brand { get; set; } = default;
        public int Year { get; set; } = default;
    }
}