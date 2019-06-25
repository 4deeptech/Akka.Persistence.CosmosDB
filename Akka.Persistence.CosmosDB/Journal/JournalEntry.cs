using Newtonsoft.Json;
using Akka.Serialization;
using System;

namespace Akka.Persistence.CosmosDB.Journal
{
    public class JournalEntry : ITypedDocument
    {
        internal JournalEntry(IPersistentRepresentation message, Akka.Serialization.Serialization serialization)
        {
            Id = $"{DocumentType}-{message.PersistenceId}-{message.SequenceNr}";
            IsDeleted = message.IsDeleted;
            Payload = message.Payload;
            PersistenceId = message.PersistenceId;
            SequenceNr = message.SequenceNr;
            Manifest = message.Manifest;
            if(string.IsNullOrWhiteSpace(message.Manifest))
            {
                Manifest = message.Payload.GetType().AssemblyQualifiedName;
            }
            Serializer serializer = serialization.FindSerializerFor(Payload);
            if (serializer != null)
            {
                SerializerId = serializer.Identifier;
                if(serializer is SerializerWithStringManifest)
                {
                    Manifest = ((SerializerWithStringManifest)serializer).Manifest(message.Payload);
                }
                if (SerializerId.HasValue && SerializerId.Value == 1)
                {
                    Payload = message.Payload;
                }
                else
                {
                    Payload = Convert.ToBase64String(serializer.ToBinary(message.Payload));
                }
            }
        }

        public JournalEntry()
        {

        }

        [JsonProperty("id")]
        public string Id { get; set; }
        public string PersistenceId { get; set; }
        public long SequenceNr { get; set; }
        public bool IsDeleted { get; set; }
        public object Payload { get; set; }
        public string Manifest { get; set; }
        public int? SerializerId { get; set; }

        public string DocumentType
        {
            get { return "jrnl"; }
        }
    }
}
