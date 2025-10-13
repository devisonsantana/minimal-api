using System.Text.Json.Serialization;
using minimal_api.Domain.Filters;

namespace minimal_api.Domain.Enums
{
    [JsonConverter(typeof(StrictStringEnumConverter<Role>))]
    public enum Role
    {
        ADMIN, EDITOR
    }
}