using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Timer;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Npgsql;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aragas.QServer.Metrics
{
    public class PostgreSQLMetricsService : BackgroundService
    {
        private readonly IMetrics _metrics;
        private readonly ILogger _logger;
        private readonly int _delay;
        private readonly string _connectionString;

        private readonly HistogramOptions service_response_milliseconds;
        private readonly TimerOptions service_last_response_milliseconds;

        public PostgreSQLMetricsService(IMetrics metrics, string connectionName, string connectionString, ILogger<PostgreSQLMetricsService> logger, int delay = 3000)
        {
            _metrics = metrics;
            _connectionString = connectionString;
            _logger = logger;
            _delay = delay;

            service_response_milliseconds = new HistogramOptions()
            {
                Name = $"postgresql_{connectionName}_response_milliseconds",
                Context = "service",
                MeasurementUnit = Unit.Custom("Milliseconds")
            };
            service_last_response_milliseconds = new TimerOptions()
            {
                Name = $"postgresql_{connectionName}_last_response_milliseconds",
                Context = "service",
                MeasurementUnit = Unit.Custom("Milliseconds")
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting reporting. Delay:{Delay}", _delay);

            while (!stoppingToken.IsCancellationRequested)
            {
                var timer = _metrics.Provider.Timer.Instance(service_last_response_milliseconds);

                var start = timer.StartRecording();
                var exception = false;
                try
                {
                    await using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync(stoppingToken);

                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1;";
                    await command.ExecuteScalarAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception caught!");
                    exception = true;
                }
                var end = timer.EndRecording();


                if (exception)
                {
                    timer.Record(-1, TimeUnit.Nanoseconds);
                }
                else
                {
                    var value = end - start;
                    timer.Record(value, TimeUnit.Nanoseconds);
                    _metrics.Provider.Histogram.Instance(service_response_milliseconds).Update(value);
                }

                await Task.Delay(_delay, stoppingToken);
            }
        }
    }
}