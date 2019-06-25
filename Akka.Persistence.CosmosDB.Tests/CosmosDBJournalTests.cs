using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Configuration;
using Akka.Persistence.TestKit.Journal;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Tests.Unit
{
    [Collection("CosmosDBSpec")]
    public class CosmosDBJournalTests : JournalSpec
    {
        private static readonly string SpecConfig = @"
            akka.test.single-expect-default = 30s
            akka.persistence {
                publish-plugin-commands = on
                journal {
                    plugin= ""akka.persistence.journal.cosmosdb"" 
                    cosmosdb {
                        class = ""Akka.Persistence.CosmosDB.Journal.CosmosDBJournal, Akka.Persistence.CosmosDB""
                        service-uri = ""<serviceUri>""
                        secret-key = ""<secretKey>""
                        auto-initialize = on
                        database = ""unittestactors""
                    }
                }
            }";

        private static IConfigurationRoot _config;

        protected override bool SupportsRejectingNonSerializableObjects { get; } = false;

        public static string GetConfigSpec()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testappsettings.json");

            _config = builder.Build();

            return SpecConfig.Replace("<serviceUri>", _config["serviceUri"])
                .Replace("<secretKey>", _config["secretKey"]);
        }

        public CosmosDBJournalTests() : base(GetConfigSpec(), "CosmosDBJournalSpec")
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
