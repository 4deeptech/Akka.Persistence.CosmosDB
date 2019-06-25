using System;
using Akka.Configuration;

namespace Akka.Persistence.CosmosDB
{
    /// <summary>
    /// Document Db Settings abstract class
    /// </summary>
    public abstract class CosmosDBSettings
    {
        public string ServiceUri { get; private set; }
        public string SecretKey { get; private set; }
        public bool AutoInitialize { get; private set; }
        public string Database { get; private set; }
        public string Collection { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBSettings"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public CosmosDBSettings(Config config)
        {
            ServiceUri = config.GetString("service-uri");
            SecretKey = config.GetString("secret-key");
            Database = config.GetString("database");
            Collection = config.GetString("collection");
            AutoInitialize = config.GetBoolean("auto-initialize");
        }
    }

    /// <summary>
    /// Journal Settings
    /// </summary>
    /// <seealso cref="Akka.Persistence.CosmosDB.CosmosDBSettings" />
    public class CosmosDBJournalSettings : CosmosDBSettings
    {
        public string MetadataCollection { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBJournalSettings"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <exception cref="ArgumentNullException">config - CosmosDB settings cannot be initialized, because required HOCON section couldn't been found</exception>
        public CosmosDBJournalSettings(Config config)
            : base(config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config),
                    "CosmosDB settings cannot be initialized, because required HOCON section couldn't been found");
            MetadataCollection = config.GetString("metadata-collection");
        }
    }

    /// <summary>
    /// Snapshot store settings
    /// </summary>
    /// <seealso cref="Akka.Persistence.CosmosDB.CosmosDBSettings" />
    public class CosmosDBSnapshotSettings : CosmosDBSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBSnapshotSettings"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <exception cref="ArgumentNullException">config - CosmosDB settings cannot be initialized, because required HOCON section couldn't been found</exception>
        public CosmosDBSnapshotSettings(Config config)
            : base(config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config),
                    "CosmosDB settings cannot be initialized, because required HOCON section couldn't been found");
        }
    }
}
