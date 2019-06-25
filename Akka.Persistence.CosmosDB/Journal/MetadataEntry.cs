using Newtonsoft.Json;

namespace Akka.Persistence.CosmosDB.Journal
{
    public class MetadataEntry : ITypedDocument
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string PersistenceId { get; set; }
        public long SequenceNr { get; set; }

        public string DocumentType
        {
            get { return "meta"; }
        }
    }
}
