using App.Metrics;

using Aragas.QServer.Metrics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNpgSqlMetrics(this IServiceCollection services, string connectionName, string connectionString)
        {
            services.AddSingleton<IHostedService>(sp => new PostgreSQLMetricsService(
                sp.GetRequiredService<IMetrics>(),
                connectionName,
                connectionString,
                sp.GetRequiredService<ILogger<PostgreSQLMetricsService>>()));

            return services;
        }
    }
}