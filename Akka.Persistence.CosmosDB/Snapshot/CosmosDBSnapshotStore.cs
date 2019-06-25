using System;
using System.Linq;
using System.Threading.Tasks;
using Akka.Persistence.Snapshot;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akka.Persistence.CosmosDB.Snapshot
{
    public class CosmosDBSnapshotStore : SnapshotStore
    {
        private readonly CosmosDBSnapshotSettings settings;
        private Lazy<IDocumentClient> documentClient;
        private Lazy<Database> cosmosDBDatabase;
        private Lazy<DocumentCollection> snapShotCollection;
        private readonly Akka.Serialization.Serialization serialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBSnapshotStore"/> class.
        /// </summary>
        public CosmosDBSnapshotStore()
        {
            this.settings = CosmosDBPersistence.Get(Context.System).SnapshotStoreSettings;
            this.serialization = Context.System.Serialization;
        }

        /// <summary>
        /// User overridable callback.
        /// <p />
        /// Is called when an Actor is started.
        /// Actors are automatically started asynchronously when created.
        /// Empty default implementation.
        /// </summary>
        protected override void PreStart()
        {
            base.PreStart();

            documentClient = new Lazy<IDocumentClient>(() =>
            {
                return new DocumentClient(new Uri(settings.ServiceUri),
                    settings.SecretKey,new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp });
            });

            cosmosDBDatabase = new Lazy<Database>(() =>
            {
                var database = documentClient.Value.CreateDatabaseQuery()
                    .Where(db => db.Id == settings.Database).AsEnumerable().FirstOrDefault();
                if (database == null && settings.AutoInitialize)
                {
                    database = documentClient.Value.CreateDatabaseAsync(new Database
                    {
                        Id = settings.Database
                    }).GetAwaiter().GetResult();
                }
                else if (database == null)
                {
                    throw new ApplicationException("CosmosDB database is not initialized, set auto-initialize to on if you want it to be initialized");
                }
                return database;
            });

            snapShotCollection = new Lazy<DocumentCollection>(() =>
            {
                var cosmosDBName = cosmosDBDatabase.Value.Id;
                var documentCollection = documentClient.Value.CreateDocumentCollectionQuery
                    (UriFactory.CreateDatabaseUri(cosmosDBName))
                    .Where(a => a.Id == settings.Collection).AsEnumerable().FirstOrDefault();
                if (documentCollection == null && settings.AutoInitialize)
                {
                    documentCollection = documentClient.Value
                        .CreateDocumentCollectionAsync(UriFactory.CreateDatabaseUri(cosmosDBName),
                    new DocumentCollection
                    {
                        Id = settings.Collection,
                        PartitionKey = new PartitionKeyDefinition()
                        {
                            Paths = new System.Collections.ObjectModel.Collection<string>() { "/PersistenceId" }
                        }
                    }, new RequestOptions { OfferThroughput = 10100 }).GetAwaiter().GetResult();
                }
                else if (documentCollection == null)
                {
                    throw new ApplicationException("CosmosDB document collection is not initialized, set auto-initialize to on if you want it to be initialized");
                }

                return documentCollection;
            });

        }

        private IQueryable<SnapshotEntry> GetSnapshotQuery(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            IQueryable<SnapshotEntry> query = documentClient.Value.CreateDocumentQuery<SnapshotEntry>(snapShotCollection.Value.SelfLink, new FeedOptions { PartitionKey = new PartitionKey(persistenceId) });

            query = query.Where(a => a.PersistenceId == persistenceId);

            if (criteria.MaxSequenceNr > 0 && criteria.MaxSequenceNr < long.MaxValue)
                query = query.Where(a => a.SequenceNr <= criteria.MaxSequenceNr);

            if (criteria.MaxTimeStamp != DateTime.MinValue && criteria.MaxTimeStamp != DateTime.MaxValue)
            {
                var dateTimeAsJson = new DateTimeJsonObject(criteria.MaxTimeStamp);
                query = query.Where(a => a.Timestamp.Date < dateTimeAsJson.Date || 
                    a.Timestamp.Ticks <= dateTimeAsJson.Ticks);
            }
                

            return query;
        }

        /// <summary>
        /// Deletes the snapshot identified by <paramref name="metadata" />.
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="metadata">TBD</param>
        /// <returns>
        /// TBD
        /// </returns>
        protected override async Task DeleteAsync(SnapshotMetadata metadata)
        {
            IQueryable<SnapshotEntry> query = documentClient.Value.CreateDocumentQuery<SnapshotEntry>
                (snapShotCollection.Value.SelfLink);

            query = query.Where(a => a.PersistenceId == metadata.PersistenceId);

            if (metadata.SequenceNr > 0 && metadata.SequenceNr < long.MaxValue)
                query = query.Where(a => a.SequenceNr == metadata.SequenceNr);

            if (metadata.Timestamp != DateTime.MinValue && metadata.Timestamp != DateTime.MaxValue)
            {
                var dateTimeAsJson = new DateTimeJsonObject(metadata.Timestamp);
                query = query.Where(a => a.Timestamp.Date == dateTimeAsJson.Date && a.Timestamp.Ticks == dateTimeAsJson.Ticks);
            }   

            var document = query.ToList().FirstOrDefault();

            if (document != null)
                await documentClient.Value.DeleteDocumentAsync(
                    UriFactory.CreateDocumentUri(cosmosDBDatabase.Value.Id, snapShotCollection.Value.Id, document.Id), new RequestOptions() { PartitionKey = new PartitionKey(metadata.PersistenceId) });
        }

        /// <summary>
        /// Deletes all snapshots matching provided <paramref name="criteria" />.
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="persistenceId">persistenceId</param>
        /// <param name="criteria">Criteria</param>
        /// <returns>
        /// TBD
        /// </returns>
        protected override async Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            var query = GetSnapshotQuery(persistenceId, criteria);
            var documents = query.ToList();
            var deleteTasks = documents.Select(async a =>
            {
                await documentClient.Value.DeleteDocumentAsync(
                    UriFactory.CreateDocumentUri(cosmosDBDatabase.Value.Id, snapShotCollection.Value.Id, a.Id), new RequestOptions() { PartitionKey = new PartitionKey(persistenceId) });
            });

            await Task.WhenAll(deleteTasks.ToArray());
        }

        /// <summary>
        /// Asynchronously loads a snapshot.
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="persistenceId">PersistenceId</param>
        /// <param name="criteria">Selection criteria</param>
        /// <returns>
        /// TBD
        /// </returns>
        protected override Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            var query = GetSnapshotQuery(persistenceId, criteria);
            var result = query
                .Where(a => a.DocumentType == "snap")
                    .OrderByDescending(a => a.SequenceNr)
                    .ToList()//CosmosDB doesn't allow constructor invocation
                    .Select(a => {
                        if (a.SerializerId.HasValue && a.Snapshot != null)
                        {
                            if (a.SerializerId.Value != 1)
                            {
                                a.Snapshot = serialization.Deserialize(Convert.FromBase64String((string)a.Snapshot), a.SerializerId.Value, a.Manifest);
                            }
                            else
                            {
                                if (a.Snapshot is JObject)
                                {
                                    a.Snapshot = JsonConvert.DeserializeObject(((JObject)a.Snapshot).ToString(Newtonsoft.Json.Formatting.None), Type.GetType(a.Manifest), new JsonSerializerSettings
                                        {
                                            TypeNameHandling = TypeNameHandling.All
                                        }
                                    );
        }
                            }
                        }
                        return new SelectedSnapshot(new SnapshotMetadata(a.PersistenceId, a.SequenceNr,
                             a.Timestamp.ToDateTime()), a.Snapshot);
                             })
                    .FirstOrDefault();
            return Task.FromResult(result);
        }

        /// <summary>
        /// Asynchronously saves a snapshot.
        /// This call is protected with a circuit-breaker
        /// </summary>
        /// <param name="metadata">Metadata</param>
        /// <param name="snapshot">snapshot</param>
        /// <returns>
        /// TBD
        /// </returns>
        protected override async Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            var snapshotEntry = new SnapshotEntry(metadata, snapshot, serialization);
            await documentClient.Value.UpsertDocumentAsync( snapShotCollection.Value.SelfLink, snapshotEntry, 
                new RequestOptions() {
                    PartitionKey = new PartitionKey(metadata.PersistenceId),
                    JsonSerializerSettings = new JsonSerializerSettings() {
                        TypeNameHandling = TypeNameHandling.All
                    } });
        }

    }
}
