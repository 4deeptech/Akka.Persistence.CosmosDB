# Akka.Persistence.CosmosDB
Persistence plugins for Akka.Net to support CosmosDB

## Configuration Sample
This config sample will use the local CosmosDB Emulator which you will need to install and start before trying things out

```
akka 
        {  
            suppress-json-serializer-warning = true
	        stdout-loglevel = DEBUG
            loglevel = DEBUG


            actor {
                
                debug {
                  receive = off
                  autoreceive = off
                  lifecycle = on
                  event-stream = on
                  unhandled = on
                }
            }
           persistence {
	            journal {
                    plugin = "akka.persistence.journal.cosmosdb"
		            cosmosdb {
			            # qualified type name of the CosmosDB persistence journal actor
			            class = "Akka.Persistence.CosmosDB.Journal.CosmosDBJournal, Akka.Persistence.CosmosDB"

			            # CosmosDB endpoint used for database access
			            service-uri = "https://localhost:8081"

			            # CosmosDB endpoint Api secret key
			            secret-key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

			            # should corresponding journal table's indexes be initialized automatically
			            auto-initialize = on

                        # dispatcher used to drive journal actor
                        plugin-dispatcher = "akka.actor.default-dispatcher"

			            # CosmosDB database corresponding with Persistence
			            database = "akkatecture"

			            # CosmosDB collection corresponding with persistent journal
			            collection = "persistence"

			            # metadata collection
			            metadata-collection = "persistence"
		            }
            }

            snapshot-store {
                    plugin = "akka.persistence.snapshot-store.cosmosdb"
		            cosmosdb {
			            # qualified type name of the CosmosDB persistence snapshot actor
			            class = "Akka.Persistence.CosmosDB.Snapshot.CosmosDBSnapshotStore, Akka.Persistence.CosmosDB"

			            # CosmosDB endpoint used for database access
			            service-uri = "https://localhost:8081"

			            # CosmosDB endpoint Api secret key
			            secret-key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

			            # should corresponding snapshot's indexes be initialized automatically
			            auto-initialize = on

                        # dispatcher used to drive snapshot storage actor
                        plugin-dispatcher = "akka.actor.default-dispatcher"

			            # CosmosDB database corresponding with Persistence
			            database = "akkatecture"

			            # CosmosDB collection corresponding with persistent snapshot store
			            collection = "persistence"
		            }
	            }
            }
        }
```        
