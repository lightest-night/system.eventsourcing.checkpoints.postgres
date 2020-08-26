using System;
using System.Threading;
using System.Threading.Tasks;
using LightestNight.System.Data.Postgres;
using Npgsql;

namespace LightestNight.System.EventSourcing.Checkpoints.Postgres
{
    public class PostgresCheckpointManager : ICheckpointManager
    {
        private readonly Func<NpgsqlConnection> _createConnection;
        private readonly PostgresOptions _options;
        private readonly Scripts.Scripts _scripts;

        public PostgresCheckpointManager(PostgresOptions options, IPostgresConnection connection)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scripts = new Scripts.Scripts(_options.Schema);

            _createConnection = connection.Build;

            CreateSchemaIfNotExists();
        }

        public async Task SetCheckpoint(string checkpointName, long checkpoint, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = _createConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            await using var command = new NpgsqlCommand(_scripts.SetCheckpoint, connection);
            command.Parameters.AddWithValue("@CheckpointName", checkpointName);
            command.Parameters.AddWithValue("@Checkpoint", checkpoint);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<long?> GetCheckpoint(string checkpointName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = _createConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_scripts.GetCheckpoint, connection);
            command.Parameters.AddWithValue("@CheckpointName", checkpointName);

            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as long?;
        }

        public async Task ClearCheckpoint(string checkpointName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var connection = _createConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = new NpgsqlCommand(_scripts.DeleteCheckpoint, connection);
            command.Parameters.AddWithValue("@CheckpointName", checkpointName);

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        private void CreateSchemaIfNotExists()
        {
            using var connection = _createConnection();
            connection.Open();
            
            using var transaction = connection.BeginTransaction();
            
            using (var command = new NpgsqlCommand($"CREATE SCHEMA IF NOT EXISTS {_options.Schema}",
                transaction.Connection, transaction))
                command.ExecuteNonQuery();

            using (var command = new NpgsqlCommand(_scripts.CreateSchema, transaction.Connection, transaction))
                command.ExecuteNonQuery();

            transaction.Commit();
        }
    }
}