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
        public static IServiceCollection AddNpgSqlMetrics(this IServiceCollection services, string connectionName, string connectionString, int delay = 5000)
        {
            services.AddSingleton<IHostedService, MetricsCollectorService>(sp => new MetricsCollectorService(
                sp.GetRequiredService<IMetrics>(),
                sp.GetRequiredService<ILogger<MetricsCollectorService>>(),
                new List<BaseMetricsCollector> { ActivatorUtilities.CreateInstance<PostgreSQLMetricsCollector>(sp, connectionName, connectionString) },
                delay));

            return services;
        }
    }
}