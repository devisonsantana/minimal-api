using minimal_api.Domain.Enums;

namespace minimal_api.Domain.DTOs
{
    public record UserDTO
    {
        public string Email { get; set; } = default;
        public string Password { get; set; } = default;
        public Role? Role { get; set; } = default;
    }
}