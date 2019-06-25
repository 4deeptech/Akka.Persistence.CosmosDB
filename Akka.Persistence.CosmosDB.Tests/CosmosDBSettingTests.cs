using Akka.Persistence.CosmosDB;
using FluentAssertions;
using Xunit;

namespace Tests.Unit
{
    [Collection("CosmosDBSpec")]
    public class CosmosDBSettingTests : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void CosmosDB_JournalSettings_must_have_default_values()
        {
            var cosmosDBPersistence = CosmosDBPersistence.Get(Sys);

            cosmosDBPersistence.JournalSettings.ServiceUri.Should().Be(string.Empty);
            cosmosDBPersistence.JournalSettings.SecretKey.Should().Be(string.Empty);
            cosmosDBPersistence.JournalSettings.AutoInitialize.Should().BeFalse();
            cosmosDBPersistence.JournalSettings.Database.Should().Be("Actors");
            cosmosDBPersistence.JournalSettings.Collection.Should().Be("persistence");
            cosmosDBPersistence.JournalSettings.MetadataCollection.Should().Be("persistence");
        }

        [Fact]
        public void CosmosDB_SnapshotStoreSettingsSettings_must_have_default_values()
        {
            var cosmosDBPersistence = CosmosDBPersistence.Get(Sys);

            cosmosDBPersistence.SnapshotStoreSettings.ServiceUri.Should().Be(string.Empty);
            cosmosDBPersistence.SnapshotStoreSettings.SecretKey.Should().Be(string.Empty);
            cosmosDBPersistence.SnapshotStoreSettings.AutoInitialize.Should().BeFalse();
            cosmosDBPersistence.SnapshotStoreSettings.Database.Should().Be("Actors");
            cosmosDBPersistence.SnapshotStoreSettings.Collection.Should().Be("persistence");
        }
    }
}
