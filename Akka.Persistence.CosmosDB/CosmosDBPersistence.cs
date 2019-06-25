using System;
using Akka.Actor;
using Akka.Configuration;

namespace Akka.Persistence.CosmosDB
{
    /// <summary>
    /// Persistence Extensions
    /// </summary>
    /// <seealso cref="Akka.Actor.IExtension" />
    public class CosmosDBPersistence : IExtension
    {
        /// <summary>
        /// Gets the default configuration
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            return ConfigurationFactory.FromResource<CosmosDBPersistence>
                ("Akka.Persistence.CosmosDB.reference.conf");
        }

        /// <summary>
        /// Gets persistence provided for specified actor system.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <returns></returns>
        public static CosmosDBPersistence Get(ActorSystem system)
        {
            return system.WithExtension<CosmosDBPersistence, CosmosDBPersistenceProvider>();
        }

        public CosmosDBJournalSettings JournalSettings { get; }
        public CosmosDBSnapshotSettings SnapshotStoreSettings { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosDBPersistence"/> class.
        /// </summary>
        /// <param name="system">The system.</param>
        /// <exception cref="ArgumentNullException">system</exception>
        public CosmosDBPersistence(ExtendedActorSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            system.Settings.InjectTopLevelFallback(DefaultConfiguration());

            var journalConfig = system.Settings.Config.GetConfig("akka.persistence.journal.cosmosdb");
            JournalSettings = new CosmosDBJournalSettings(journalConfig);

            var snapShotConfig = system.Settings.Config.GetConfig("akka.persistence.snapshot-store.cosmosdb");
            SnapshotStoreSettings = new CosmosDBSnapshotSettings(snapShotConfig);
        }
    }
}
