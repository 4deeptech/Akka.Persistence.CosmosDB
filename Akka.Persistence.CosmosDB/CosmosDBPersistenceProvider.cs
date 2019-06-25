using Akka.Actor;

namespace Akka.Persistence.CosmosDB
{
    /// <summary>
    /// Persistence Provider
    /// </summary>
    /// <seealso cref="Akka.Actor.ExtensionIdProvider{Akka.Persistence.CosmosDB.CosmosDBPersistence}" />
    public class CosmosDBPersistenceProvider : ExtensionIdProvider<CosmosDBPersistence>
    {
        /// <summary>
        /// Creates the current extension using a given actor system.
        /// </summary>
        /// <param name="system">The actor system to use when creating the extension.</param>
        /// <returns>
        /// The extension created using the given actor system.
        /// </returns>
        public override CosmosDBPersistence CreateExtension(ExtendedActorSystem system)
        {
            return new CosmosDBPersistence(system);
        }
    }
}