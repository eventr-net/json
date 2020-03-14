namespace EventR.Json
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// https://github.com/dotnet/corefx/issues/38641
    /// </summary>
    public sealed class NullableTimeSpanConverter : JsonConverter<TimeSpan?>
    {
        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string s = reader.GetString();
            return TimeSpan.TryParse(s, out var ts)
                ? ts
                : (TimeSpan?)null;
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("g"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
