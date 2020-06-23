# Lightest Night
## Event Sourcing > Checkpoints > Postgres

The elements required to manage a Stream checkpoint inside a Postgres data store

### Build Status
![](https://github.com/lightest-night/system.eventsourcing.checkpoints.postgres/workflows/CI/badge.svg)
![](https://github.com/lightest-night/system.eventsourcing.checkpoints.postgres/workflows/Release/badge.svg)

#### How To Use
##### Registration
* Asp.Net Standard/Core Dependency Injection
  * Use the provided `services.AddPostgresCheckpointManagement(Action<PostgresCheckpointOptions>? optionsAccessor = null)` method

* Other Containers
  * Register the PostgresCheckpointOptions
  * Register a new instance of `ICheckpointManager` with concrete implementation `PostgresCheckpointManager`
##### Usage
* `Task SetCheckpoint(string checkpointName, long checkpoint, CancellationToken cancellationToken = default)`
  * An asynchronous function to call when setting the checkpoint
* `Task<long?> GetCheckpoint(string checkpointName, CancellationToken cancellationToken = default)`
  * An asynchronous function to call when getting the checkpoint
* `Task ClearCheckpoint(string checkpointName, CancellationToken cancellationToken = default)`
  * An asynchronous function to call when clearing the checkpoint