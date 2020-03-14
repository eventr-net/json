namespace EventR.Json
{
    using EventR.Abstractions;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Fluent API configuration related to EventR.Json module features.
    /// </summary>
    public class JsonBuilder : BuilderBase
    {
        private HashSet<JsonConverter> customConverters;
        private JsonSerializerOptions serializerOptions;
        private bool useTypeNameAliasing;

        public JsonBuilder(ConfigurationContext context)
            : base(context)
        {
            context.RegisterSerializer(CreateSerializer, Serializer.Id);
        }

        public static JsonSerializerOptions CreateDefaultSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
            };
            options.Converters.Add(new TimeSpanConverter());
            options.Converters.Add(new NullableTimeSpanConverter());

            return options;
        }

        private ISerializeEvents CreateSerializer(IEventFactory eventFactory)
        {
            var translator = useTypeNameAliasing ? new TypeIdTranslator(eventFactory.GetKnownEvents()) : null;
            var options = serializerOptions ?? CreateDefaultSerializerOptions();
            if (customConverters != null)
            {
                foreach (var converter in customConverters)
                {
                    options.Converters.Add(converter);
                }
            }

            return new Serializer(options, eventFactory, translator);
        }

        public JsonBuilder WithConverters(params JsonConverter[] converters)
        {
            Expect.NotEmpty(converters, nameof(converters));
            if (customConverters == null)
            {
                customConverters = new HashSet<JsonConverter>();
            }

            foreach (var converter in converters.Where(c => !customConverters.Contains(c)))
            {
                customConverters.Add(converter);
            }

            return this;
        }

        public JsonBuilder WithSerializerOptions(JsonSerializerOptions options)
        {
            Expect.NotNull(options, nameof(options));
            serializerOptions = options;
            return this;
        }

        public JsonBuilder UseTypeNameAliasing()
        {
            useTypeNameAliasing = true;
            return this;
        }
    }
}
