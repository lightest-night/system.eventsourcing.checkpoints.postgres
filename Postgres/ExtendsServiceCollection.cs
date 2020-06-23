using System;
using LightestNight.System.Data.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LightestNight.System.EventSourcing.Checkpoints.Postgres
{
    public static class ExtendsServiceCollection
    {
        public static IServiceCollection AddPostgresCheckpointManagement(this IServiceCollection services,
            Action<PostgresCheckpointOptions>? optionsAccessor = null)
        {
            services.Configure(optionsAccessor);

            var serviceProvider = services.BuildServiceProvider();
            var postgresOptions = serviceProvider.GetRequiredService<IOptions<PostgresCheckpointOptions>>().Value;
            var postgresConnection = new PostgresConnection(Options.Create(postgresOptions));

            if (!(serviceProvider.GetService<ICheckpointManager>() is PostgresCheckpointManager))
                services.AddTransient<ICheckpointManager>(sp =>
                    new PostgresCheckpointManager(sp.GetRequiredService<IOptions<PostgresCheckpointOptions>>(),
                        sp.GetRequiredService<ILogger<PostgresCheckpointManager>>(), postgresConnection));

            return services;
        }
    }
}