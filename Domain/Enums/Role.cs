using System.Text.Json.Serialization;

namespace minimal_api.Domain.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Role
    {
        ADMIN, EDITOR
    }
}