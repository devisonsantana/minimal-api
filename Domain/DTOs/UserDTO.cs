using System.Text.Json.Serialization;
using minimal_api.Domain.Enums;
using minimal_api.Domain.Filters;

namespace minimal_api.Domain.DTOs
{
    public record UserDTO
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        [JsonConverter(typeof(StrictStringEnumConverter<Role>))]
        public Role Role { get; set; } = Role.EDITOR;
    }
}