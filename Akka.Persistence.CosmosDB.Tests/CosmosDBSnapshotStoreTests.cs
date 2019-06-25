using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Persistence.TestKit.Snapshot;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests.Unit
{
    [Collection("CosmosDBSpec")]
    public class CosmosDBSnapshotStoreTests : SnapshotStoreSpec
    {
        private static readonly string SpecConfig = @"
            akka.test.single-expect-default = 30s
            akka.persistence {
                publish-plugin-commands = on
                snapshot-store {
                    plugin = ""akka.persistence.snapshot-store.cosmosdb""
                    cosmosdb {
                        class = ""Akka.Persistence.CosmosDB.Snapshot.CosmosDBSnapshotStore, Akka.Persistence.CosmosDB""
                        service-uri = ""<serviceUri>""
                        secret-key = ""<secretKey>""
                        auto-initialize = on
                        database = ""unittestactors""
                    }
                }
            }";

        private static IConfigurationRoot _config;

        public static string GetConfigSpec()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testappsettings.json");

            _config = builder.Build();

            return SpecConfig.Replace("<serviceUri>", _config["serviceUri"])
                .Replace("<secretKey>", _config["secretKey"]);
        }

        public CosmosDBSnapshotStoreTests() : base(GetConfigSpec(), "CosmosDBSnapshotStoreSpec")
        {
            Initialize();
        }

        protected override void Dispose(bool disposing)
        {
            var documentClient = new DocumentClient(new Uri(_config["serviceUri"]), _config["secretKey"]);

            documentClient.DeleteDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri("unittestactors", "persistence")).Wait();

            base.Dispose(disposing);
        }
    }
}
