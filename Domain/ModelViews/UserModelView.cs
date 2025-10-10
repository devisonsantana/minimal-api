namespace minimal_api.Domain.ModelViews
{
    public record UserModelView
    {
        public int Id { get; set; } = default;
        public string Email { get; set; } = default;
        public string Role { get; set; } = default;
    }
}