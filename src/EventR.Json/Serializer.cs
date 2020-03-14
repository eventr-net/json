namespace EventR.Json
{
    using EventR.Abstractions;
    using EventR.Abstractions.Exceptions;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;

    public class Serializer : ISerializeEvents
    {
        private const byte NewLineByte = 0x10; // \n
        private readonly IEventFactory eventFactory;
        private readonly ITranslateTypeIds idTranslator;
        private readonly bool idTranslationEnabled;
        private readonly JsonSerializerOptions options;

        public const string Id = "json";

        public int UsualCommitSize { get; set; } = 2048;

        public Serializer(JsonSerializerOptions options, IEventFactory eventFactory, ITranslateTypeIds idTranslator = null)
        {
            Expect.NotNull(eventFactory, nameof(eventFactory));
            Expect.NotNull(options, nameof(options));

            this.options = options;
            this.eventFactory = eventFactory;
            this.idTranslator = idTranslator;
            idTranslationEnabled = idTranslator != null;
        }

        public void Serialize(Commit commit, object[] events)
        {
            Expect.NotNull(commit, "commit");
            Expect.NotEmpty(events, "events");

            var position = 0;
            commit.PayloadLayout = new PayloadLayout(events.Length);
            using (var stream = new MemoryStream(UsualCommitSize * events.Length))
            {
                for (int i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    var evtType = evt.GetType();
                    var evtTypeId = CreateTypeId(evtType);

                    var concreteType = eventFactory.GetConcreteType(evtType);
                    var bytes = JsonSerializer.SerializeToUtf8Bytes(evt, concreteType, options);
                    var s = Encoding.UTF8.GetString(bytes);
                    stream.Write(bytes, 0, bytes.Length);

                    commit.PayloadLayout.Add(position, bytes.Length, evtTypeId);

                    if (i != events.Length - 1)
                    {
                        stream.WriteByte(NewLineByte);
                        stream.WriteByte(NewLineByte);
                        position += bytes.Length + 2;
                    }
                }

                commit.Payload = stream.ToArray();
            }

            commit.ItemsCount = (short)events.Length;
            commit.SerializerId = SerializerId;
        }

        private string CreateTypeId(Type type)
        {
            var t = eventFactory.GetOriginalType(type) ?? type;
            if (idTranslationEnabled)
            {
                return idTranslator.Translate(t);
            }

            var ns = type.Namespace != null ? $"{type.Namespace}." : string.Empty;
            return $"{ns}{t.Name}, {t.GetTypeInfo().Assembly.GetName().Name}";
        }

        private Type GetTypeFromTypeId(string typeId, Commit commit)
        {
            if (idTranslationEnabled)
            {
                var t = idTranslator.Translate(typeId, false);
                if (t != null)
                {
                    return t;
                }
            }

            try
            {
                return Type.GetType(typeId);
            }
            catch (Exception ex)
            {
                var msg = $"Failed to deserialize type '{typeId}'. " +
                    $"Type name might be shortened by {nameof(ITranslateTypeIds)} when the commit has been serialized.";
                throw new SerializationException(msg, ex, commit);
            }
        }

        public object[] Deserialize(Commit commit)
        {
            Expect.NotNull(commit, "commit");
            if (!commit.HasPayloadLayout)
            {
                throw new SerializationException($"Commit does not have a payload layout (required for {Id} serializer).", commit);
            }

            var layouts = commit.PayloadLayout.Items;
            if (commit.ItemsCount != layouts.Length)
            {
                throw new SerializationException("Payload layout describes different amount of events than the commit holds.", commit);
            }

            int i = 0;
            try
            {
                var events = new object[layouts.Length];
                for (i = 0; i < layouts.Length; i++)
                {
                    var (offset, length, typeId) = layouts[i];
                    var type = GetTypeFromTypeId(typeId, commit);
                    var concreteType = eventFactory.GetConcreteType(type);
                    var bytes = new ReadOnlySpan<byte>(commit.Payload, offset, length);
                    var evt = JsonSerializer.Deserialize(bytes, concreteType, options);

                    events[i] = evt;
                }

                return events;
            }
            catch (Exception ex)
            {
                throw new SerializationException($"Failed to deserialize commit payload (item: {i + 1}).", ex, commit);
            }
        }

        public string SerializerId => Id;
    }
}
