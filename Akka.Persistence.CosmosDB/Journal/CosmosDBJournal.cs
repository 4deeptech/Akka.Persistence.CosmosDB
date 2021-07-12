using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Akka.Persistence.CosmosDB.Journal
{
    public class CosmosDBJournal : AsyncWriteJournal
    {
        private readonly CosmosDBJournalSettings settings;
        private Lazy<IDocumentClient> documentClient;
        private Lazy<Database> cosmosDBDatabase;
        private Lazy<DocumentCollection> journalCollection;
        private Lazy<DocumentCollection> metadataCollection;
        private readonly Akka.Serialization.Serialization serialization;

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBJournal"/> class.
        /// </summary>
        public CosmosDBJournal()
        {
            this.settings = CosmosDBPersistence.Get(Context.System).JournalSettings;
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
                    settings.SecretKey, new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp });
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

            journalCollection = new Lazy<DocumentCollection>(() =>
            {
                var cosmosDBName = cosmosDBDatabase.Value.Id;
                var documentCollection = documentClient.Value.CreateDocumentCollectionQuery(cosmosDBDatabase.Value.SelfLink)
                    .Where(a => a.Id == settings.Collection).AsEnumerable().FirstOrDefault();
                if (documentCollection == null && settings.AutoInitialize)
                {
                    documentCollection = documentClient.Value
                        .CreateDocumentCollectionAsync(cosmosDBDatabase.Value.SelfLink,
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

            metadataCollection = new Lazy<DocumentCollection>(() =>
            {
                var cosmosDBName = cosmosDBDatabase.Value.Id;

                var collection = documentClient.Value.CreateDocumentCollectionQuery
                    (cosmosDBDatabase.Value.SelfLink)
                    .Where(a => a.Id == settings.MetadataCollection).AsEnumerable().FirstOrDefault();
                if (collection == null && settings.AutoInitialize)
                {
                    collection = documentClient.Value.
                        CreateDocumentCollectionAsync(cosmosDBDatabase.Value.SelfLink,
                    new DocumentCollection
                    {
                        Id = settings.MetadataCollection,
                        PartitionKey = new PartitionKeyDefinition()
                            {
                                Paths = new System.Collections.ObjectModel.Collection<string>() { "/PersistenceId" }
                            }
                        }, new RequestOptions { OfferThroughput = 10100 }).GetAwaiter().GetResult();
                }
                else if (collection == null)
                {
                    throw new ApplicationException("CosmosDB metadata collection is not initialized, set auto-initialize to on if you want it to be initialized");
                }

                return collection;
            });
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            var documentLink = UriFactory.CreateDocumentUri(cosmosDBDatabase.Value.Id, metadataCollection.Value.Id, persistenceId);
            try
            {
                var document = await documentClient.Value.ReadDocumentAsync(documentLink, new RequestOptions() { PartitionKey = new PartitionKey(persistenceId) });
                return ((MetadataEntry)((dynamic)document.Resource)).SequenceNr;
            }
            catch
            {
                return 0;
            }
            
        }

        public override Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> recoveryCallback)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            // Limit allows only integer
            var limitValue = max >= int.MaxValue ? int.MaxValue : (int)max;

            // Do not replay messages if limit equal zero
            if (limitValue == 0)
                return Task.FromResult(false);

            IQueryable<JournalEntry> query = GetJournalEntryQuery(persistenceId)
                .Where(a => a.PersistenceId == persistenceId 
                            && a.SequenceNr >= fromSequenceNr 
                            && a.SequenceNr <= toSequenceNr
                            && a.DocumentType == "jrnl")
                .OrderBy(a=> a.SequenceNr)
                .Take(limitValue); 

            var documents = query.ToList();

            documents.ForEach(doc =>
            {
                if(doc.SerializerId.HasValue)
                {
                    if(doc.SerializerId.Value != 1)
                    {
                        doc.Payload = serialization.Deserialize(Convert.FromBase64String((string)doc.Payload), doc.SerializerId.Value, doc.Manifest);
                    }
                    else
                    {
                        if(doc.Payload is JObject)
                        {
                            doc.Payload = JsonConvert.DeserializeObject(((JObject)doc.Payload).ToString(Newtonsoft.Json.Formatting.None), Type.GetType(doc.Manifest));
                        }
                    }
                }
                recoveryCallback(new Persistent(doc.Payload, doc.SequenceNr, doc.PersistenceId, doc.Manifest, doc.IsDeleted, context.Sender));
            });

            return Task.FromResult(0);
        }

        private IQueryable<JournalEntry> GetJournalEntryQuery(string persistenceId)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            return documentClient.Value.CreateDocumentQuery<JournalEntry>( journalCollection.Value.SelfLink, new FeedOptions { MaxItemCount = -1, PartitionKey = new PartitionKey(persistenceId)});
        }

        private IQueryable<MetadataEntry> GetMetadataEntryQuery(string persistenceId)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            return documentClient.Value.CreateDocumentQuery<MetadataEntry>(metadataCollection.Value.SelfLink, new FeedOptions { MaxItemCount = -1, PartitionKey = new PartitionKey(persistenceId) });
        }

        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            persistenceId = IdNormalizer.Normalize(persistenceId);
            var query = GetJournalEntryQuery(persistenceId)
                .Where(a => a.PersistenceId == persistenceId
                    && a.DocumentType == "jrnl");

            if (toSequenceNr != long.MaxValue)
                query = query.Where(a => a.SequenceNr <= toSequenceNr);

            var deleteTasks = query.ToList().Select(async a =>
            {
                await documentClient.Value.DeleteDocumentAsync(
                    UriFactory.CreateDocumentUri(cosmosDBDatabase.Value.Id, journalCollection.Value.Id, a.Id), new RequestOptions() { PartitionKey = new PartitionKey(persistenceId) });
            });

            await Task.WhenAll(deleteTasks);
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var messageList = messages.ToList();
            var persistenceId = messageList.First().PersistenceId;
            persistenceId = IdNormalizer.Normalize(persistenceId);
            var cosmosDBWriteTasks = messageList.Select(async (message) =>
            {
                var persistentMessages = ((IImmutableList<IPersistentRepresentation>)message.Payload).ToArray();
                var journalEntries = persistentMessages.Select(a => new JournalEntry(a,serialization)).ToList();
                foreach(JournalEntry entry in journalEntries)
                {
                    entry.Id = IdNormalizer.Normalize(entry.Id);
                    entry.PersistenceId = IdNormalizer.Normalize(entry.PersistenceId);
                }

                var individualWriteTasks = journalEntries.Select(async a => await documentClient.Value.CreateDocumentAsync(journalCollection.Value.SelfLink, a, new RequestOptions() { PartitionKey = new PartitionKey(persistenceId) }));
                return await Task.WhenAll(individualWriteTasks.ToArray());
            });

            await SetHighestSequenceId(messageList);

            return await Task<ImmutableList<Exception>>
                .Factory
                .ContinueWhenAll(cosmosDBWriteTasks.ToArray(),
                    tasks => tasks.Select(t => t.IsFaulted ? TryUnwrapException(t.Exception) : null)
                    .ToImmutableList());
        }

        private async Task SetHighestSequenceId(List<AtomicWrite> messages)
        {
            var persistenceId = messages.Select(c => c.PersistenceId).First();
            persistenceId = IdNormalizer.Normalize(persistenceId);
            var highSequenceId = messages.Max(c => c.HighestSequenceNr);
            
            var metadataEntry = new MetadataEntry
            {
                Id = persistenceId,
                PersistenceId = persistenceId,
                SequenceNr = highSequenceId
            };

            await documentClient.Value.UpsertDocumentAsync(metadataCollection.Value.SelfLink, metadataEntry, new RequestOptions() { PartitionKey = new PartitionKey(persistenceId) });
        }
    }
}
