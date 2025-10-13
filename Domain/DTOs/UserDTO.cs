using System.Text.Json.Serialization;
using minimal_api.Domain.Enums;
using minimal_api.Domain.Filters;

namespace minimal_api.Domain.DTOs
{
    public record UserDTO
    {
        public string Email { get; set; } = default;
        public string Password { get; set; } = default;
        [JsonConverter(typeof(StrictStringEnumConverter<Role>))]
        public Role Role { get; set; } = Role.EDITOR;
    }
}