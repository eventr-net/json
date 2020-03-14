namespace EventR.Json
{
    using EventR.Abstractions;

    public static class Extensions
    {
        public static JsonBuilder Json(this BuilderBase coreBuilder)
        {
            return new JsonBuilder(coreBuilder.Context);
        }
    }
}
