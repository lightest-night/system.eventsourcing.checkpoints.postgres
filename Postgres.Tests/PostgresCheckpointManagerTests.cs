using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LightestNight.System.Data.Postgres;
using Npgsql;
using Shouldly;
using Xunit;

namespace LightestNight.System.EventSourcing.Checkpoints.Postgres.Tests
{
    public class PostgresCheckpointManagerTests
    {
        private readonly IPostgresConnection _connection;
        private readonly PostgresCheckpointOptions _options;
        private readonly ICheckpointManager _sut;

        private const string CheckpointName = "Test Checkpoint";
        private const long Checkpoint = 100;

        public PostgresCheckpointManagerTests()
        {
            var fixture = new Fixture();
            _options = fixture.Build<PostgresCheckpointOptions>()
                .Without(o => o.Host)
                .Without(o => o.Port)
                .Without(o => o.Database)
                .Without(o => o.Username)
                .Without(o => o.Password)
                .Without(o => o.Pooling)
                .Without(o => o.Schema)
                .Do(o =>
                {
                    o.Host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
                    o.Port = Convert.ToInt32(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432");
                    o.Database = Environment.GetEnvironmentVariable("POSTGRES_DATABASE") ?? "postgres";
                    o.Username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME") ?? "postgres";
                    o.Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
                    o.Pooling = Convert.ToBoolean(Environment.GetEnvironmentVariable("POSTGRES_POOLING") ?? "False");
                    o.Schema = Environment.GetEnvironmentVariable("POSTGRES_SCHEMA") ?? "public";
                })
                .Create();

            _connection = new PostgresConnection(_options);
            
            _sut = new PostgresCheckpointManager(_options, _connection);
        }

        [Fact, Trait("Category", "Integration")]
        public async Task ShouldCreateSchema()
        {
            // Assert
            await using var connection = _connection.Build();
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command =
                new NpgsqlCommand(
                    $"SELECT EXISTS(SELECT schema_name FROM information_schema.schemata WHERE schema_name = '{_options.Schema}')",
                    connection);
            (await command.ExecuteScalarAsync().ConfigureAwait(false) as bool? ?? false).ShouldBeTrue();
        }

        [Fact, Trait("Category", "Unit")]
        public void ShouldThrowIfCancellationRequestedWhenSettingCheckpoint()
        {
            // Arrange
            using var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            cancellationSource.Cancel();
            
            // Act/Assert
            Should.Throw<TaskCanceledException>(async () =>
                await _sut.SetCheckpoint(CheckpointName, Checkpoint, token).ConfigureAwait(false));
        }

        [Fact, Trait("Category", "Integration")]
        public async Task ShouldSetCheckpoint()
        {
            // Act
            await _sut.SetCheckpoint(CheckpointName, Checkpoint);
            
            // Assert
            await using var connection = _connection.Build();
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command =
                new NpgsqlCommand(
                    $"SELECT checkpoint FROM {_options.Schema}.{Constants.TableName} WHERE checkpoint_name = '{CheckpointName}'",
                    connection);
            (await command.ExecuteScalarAsync().ConfigureAwait(false) as long? ?? 0).ShouldBe(Checkpoint);
        }
        
        [Fact, Trait("Category", "Unit")]
        public void ShouldThrowIfCancellationRequestedWhenGettingCheckpoint()
        {
            // Arrange
            using var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            cancellationSource.Cancel();
            
            // Act/Assert
            Should.Throw<TaskCanceledException>(async () =>
                await _sut.GetCheckpoint(CheckpointName, token).ConfigureAwait(false));
        }

        [Fact, Trait("Category", "Integration")]
        public async Task ShouldGetCheckpoint()
        {
            // Arrange
            await using var connection = _connection.Build();
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command =
                new NpgsqlCommand(
                    $"INSERT INTO {_options.Schema}.{Constants.TableName} (checkpoint_name, checkpoint) VALUES ('{CheckpointName}', {Checkpoint}) ON CONFLICT (checkpoint_name) DO UPDATE SET checkpoint = {Checkpoint}",
                    connection);
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            await connection.CloseAsync().ConfigureAwait(false);
            
            // Act
            var result = await _sut.GetCheckpoint(CheckpointName);
            
            // Assert
            result.ShouldBe(Checkpoint);
        }

        [Fact, Trait("Category", "Unit")]
        public void ShouldThrowIfCancellationRequestedWhenClearingCheckpoint()
        {
            // Arrange
            using var cancellationSource = new CancellationTokenSource();
            var token = cancellationSource.Token;
            cancellationSource.Cancel();
            
            // Act/Assert
            Should.Throw<TaskCanceledException>(async () =>
                await _sut.ClearCheckpoint(CheckpointName, token).ConfigureAwait(false));
        }
        
        [Fact, Trait("Category", "Integration")]
        public async Task ShouldClearCheckpoint()
        {
            // Arrange
            await using var connection = _connection.Build();
            await connection.OpenAsync().ConfigureAwait(false);
            await using (var command =
                new NpgsqlCommand(
                    $"INSERT INTO {_options.Schema}.{Constants.TableName} (checkpoint_name, checkpoint) VALUES ('{CheckpointName}', {Checkpoint}) ON CONFLICT (checkpoint_name) DO UPDATE SET checkpoint = {Checkpoint}",
                    connection))
                await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            await connection.CloseAsync().ConfigureAwait(false);
            
            // Act
            await _sut.ClearCheckpoint(CheckpointName).ConfigureAwait(false);
            
            // Assert
            await connection.OpenAsync().ConfigureAwait(false);
            await using (var command =
                new NpgsqlCommand(
                    $"SELECT EXISTS(SELECT checkpoint FROM {_options.Schema}.{Constants.TableName} WHERE checkpoint_name = '{CheckpointName}')",
                    connection))
                (await command.ExecuteScalarAsync() as bool? ?? true).ShouldBeFalse();
        }
    }
}