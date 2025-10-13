using System.Text.Json;
using System.Text.Json.Serialization;
using minimal_api.Domain.Exceptions;

namespace minimal_api.Domain.Filters
{

    public class StrictStringEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Rejeita números inteiros
            if (reader.TokenType == JsonTokenType.Number)
            {
                throw new InvalidEnumValueException(
                    enumType: typeof(T).Name,
                    providedValue: reader.GetInt32().ToString(),
                    message: $"Numeric value not allowed for enum {typeof(T).Name}"
                );
            }

            // Rejeita strings que sejam numéricas ("1", "2" etc.)
            if (reader.TokenType == JsonTokenType.String)
            {
                string? strValue = reader.GetString();
                if (int.TryParse(strValue, out _))
                {
                    throw new InvalidEnumValueException(
                        enumType: typeof(T).Name,
                        providedValue: strValue,
                        message: $"Numeric value in string not allowed for enum {typeof(T).Name}"
                    );
                }

                if (Enum.TryParse<T>(strValue, ignoreCase: true, out var result))
                {
                    return result;
                }

                throw new InvalidEnumValueException(
                    enumType: typeof(T).Name,
                    providedValue: strValue,
                    message: $"Value '{strValue}' not valid for enum {typeof(T).Name}"
                );
            }

            throw new InvalidEnumValueException(
                enumType: typeof(T).Name,
                providedValue: null,
                message: $"Invalid token for enum {typeof(T).Name}: {reader.TokenType}"
                );
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }

}