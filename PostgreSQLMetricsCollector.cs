using App.Metrics;
using App.Metrics.Gauge;
using App.Metrics.Timer;

using Microsoft.Extensions.Logging;

using Npgsql;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aragas.QServer.Metrics
{
    public sealed class PostgreSQLMetricsCollector : BaseMetricsCollector
    {
        private readonly TimerOptions postgresql_response_milliseconds;
        private readonly GaugeOptions postgresql_last_response_milliseconds;

        private readonly string _connectionString;
        private readonly ILogger _logger;

        public PostgreSQLMetricsCollector(IMetrics metrics, string connectionName, string connectionString, ILogger<PostgreSQLMetricsCollector> logger) : base(metrics)
        {
            _connectionString = connectionString;
            _logger = logger;

            postgresql_response_milliseconds = new TimerOptions()
            {
                Name = $"postgresql_{connectionName}_response_milliseconds",
                Context = "service",
                MeasurementUnit = Unit.Custom("Milliseconds")
            };
            postgresql_last_response_milliseconds = new GaugeOptions()
            {
                Name = $"postgresql_{connectionName}_last_response_milliseconds",
                Context = "service",
                MeasurementUnit = Unit.Custom("Milliseconds")
            };

        }
        public override async ValueTask UpdateAsync(CancellationToken stoppingToken)
        {
            var timer = Metrics.Provider.Timer.Instance(postgresql_response_milliseconds);
            var gauge = Metrics.Provider.Gauge.Instance(postgresql_last_response_milliseconds);
            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(2000);
                var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cancellationTokenSource.Token);

                var start = timer.StartRecording();
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cts.Token);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                await command.ExecuteScalarAsync(cts.Token);
                var stop = timer.EndRecording();

                var value = stop - start;
                timer.Record(value, TimeUnit.Nanoseconds);
                gauge.SetValue(value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception caught!", GetType().Name);
                //timer.Record(0, TimeUnit.Nanoseconds);
                gauge.SetValue(double.NaN);
            }
        }

        public override void Dispose() { }
    }
}