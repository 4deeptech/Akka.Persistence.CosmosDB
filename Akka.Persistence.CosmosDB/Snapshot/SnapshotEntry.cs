using Akka.Serialization;
using Newtonsoft.Json;
using System;

namespace Akka.Persistence.CosmosDB.Snapshot
{
    /// <summary>
    /// CosmosDB document storing the snapshot
    /// </summary>
    public class SnapshotEntry : ITypedDocument
    {
        public SnapshotEntry()
        {

        }

        public SnapshotEntry(SnapshotMetadata metadata, object snapshot, Akka.Serialization.Serialization serialization)
        {
            Snapshot = snapshot;
            Id = $"{DocumentType}-{metadata.PersistenceId}-{metadata.SequenceNr}";
            SequenceNr = metadata.SequenceNr;
            Timestamp = new DateTimeJsonObject(metadata.Timestamp);
            PersistenceId = metadata.PersistenceId;
            Manifest = snapshot.GetType().AssemblyQualifiedName;
            Serializer serializer = serialization.FindSerializerFor(snapshot);
            if (serializer != null)
            {
                SerializerId = serializer.Identifier;
                if (serializer is SerializerWithStringManifest)
                {
                    Manifest = ((SerializerWithStringManifest)serializer).Manifest(snapshot);
                }
                if (SerializerId.HasValue && SerializerId.Value == 1)
                {
                    Snapshot = snapshot;
                }
                else
                {
                    Snapshot = Convert.ToBase64String(serializer.ToBinary(snapshot));
                }
                
            }
        }

        [JsonProperty("id")]
        public string Id { get; set; }
        public string PersistenceId { get; set; }
        public long SequenceNr { get; set; }
        public DateTimeJsonObject Timestamp { get; set; }
        public object Snapshot { get; set; }
        public string Manifest { get; set; }
        public int? SerializerId { get; set; }
        public string DocumentType
        {
            get { return "snap"; }
        }
    }
}
