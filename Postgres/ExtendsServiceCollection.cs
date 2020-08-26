using System;
using LightestNight.System.Data.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LightestNight.System.EventSourcing.Checkpoints.Postgres
{
    public static class ExtendsServiceCollection
    {
        public static IServiceCollection AddPostgresCheckpointManagement(this IServiceCollection services,
            Action<PostgresOptions>? options = null)
        {
            var postgresOptions = new PostgresOptions();
            options?.Invoke(postgresOptions);

            // ReSharper disable once RedundantAssignment
            services.AddPostgresData(options)
                .TryAddSingleton<ICheckpointManager>(sp =>
                    new PostgresCheckpointManager(postgresOptions, sp.GetRequiredService<IPostgresConnection>()));
            
            return services;
        }
    }
}