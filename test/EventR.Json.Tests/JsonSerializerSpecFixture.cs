namespace EventR.Json.Tests
{
    using EventR.Abstractions;
    using EventR.Spec;
    using EventR.Spec.Serialization;
    using System.Linq;

    public sealed class JsonSerializerSpecFixture : ISerializerSpecFixture
    {
        public object[] EventsFromDomain { get; }

        public ISerializeEvents Serializer { get; }

        public string Description => "Default settings: with type ID shortening and no custom Json converters.";

        public JsonSerializerSpecFixture()
        {
            var services = Helper.CreateAggregateRootServices(0);
            var rootAggregate = UseCases.Full().AsDirtyCustomerAggregate(services);
            EventsFromDomain = rootAggregate.UncommitedEvents;

            var eventTypes = EventsFromDomain.Select(x => x.GetType()).ToArray();
            var options = JsonBuilder.CreateDefaultSerializerOptions();
            Serializer = new Serializer(options, new EventFactory(eventTypes), new TypeIdTranslator(eventTypes));
        }
    }
}
