namespace minimal_api.Domain.Exceptions
{
    public class InvalidEnumValueException : Exception
    {
        public string EnumType{ get; }
        public string? ProvidedValue { get; }
        public InvalidEnumValueException(string enumType, string? providedValue, string message) : base(message)
        {
            EnumType = enumType;
            ProvidedValue = providedValue;
        }
    }
}