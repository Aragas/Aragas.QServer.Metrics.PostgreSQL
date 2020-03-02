using App.Metrics;
using Aragas.QServer.Metrics;
using Aragas.QServer.Metrics.BackgroundServices;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNpgSqlMetrics(this IServiceCollection services, string connectionName, string connectionString)
        {
            return services.AddSingleton<IHostedService>(sp => new PostgreSQLMetricsService(
                sp.GetRequiredService<IMetrics>(),
                connectionName,
                connectionString,
                sp.GetRequiredService<ILogger<PostgreSQLMetricsService>>()));
        }

        public static IServiceCollection AddNpgSqlMetricsNew(this IServiceCollection services, string connectionName, string connectionString, int delay = 5000)
        {
            services.AddSingleton<IHostedService, MetricsCollectorService>(sp => new MetricsCollectorService(
                sp.GetRequiredService<IMetrics>(),
                sp.GetRequiredService<ILogger<MetricsCollectorService>>(),
                new List<IMetricsCollector>()
                {
                    ActivatorUtilities.CreateInstance<PostgreSQLMetricsCollector>(sp, connectionName, connectionString)
                },
                delay));

            return services;
        }
    }
}