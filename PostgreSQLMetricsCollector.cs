using App.Metrics;
using App.Metrics.Histogram;
using App.Metrics.Timer;

using Microsoft.Extensions.Logging;

using Npgsql;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aragas.QServer.Metrics
{
    public sealed class PostgreSQLMetricsCollector : IMetricsCollector
    {
        private readonly HistogramOptions postgresql_response_milliseconds;
        private readonly TimerOptions postgresql_last_response_milliseconds;

        private readonly string _connectionString;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationToken;
        private readonly Task _collector;
        private IMetrics _metrics;

        public PostgreSQLMetricsCollector(string connectionName, string connectionString, ILogger<PostgreSQLMetricsCollector> logger)
        {
            _connectionString = connectionString;
            _logger = logger;

            postgresql_response_milliseconds = new HistogramOptions()
            {
                Name = $"postgresql_{connectionName}_response_milliseconds",
                Context = "service",
                MeasurementUnit = Unit.Custom("Milliseconds")
            };
            postgresql_last_response_milliseconds = new TimerOptions()
            {
                Name = $"postgresql_{connectionName}_last_response_milliseconds",
                Context = "service",
                MeasurementUnit = Unit.Custom("Milliseconds")
            };

            _cancellationToken = new CancellationTokenSource();
            _collector = Task.Run(CheckMetrics);
        }

        private async Task CheckMetrics()
        {
            while (_cancellationToken?.IsCancellationRequested == false)
            {
                var timer = _metrics.Provider.Timer.Instance(postgresql_last_response_milliseconds);
                var start = timer.StartRecording();
                var exception = false;
                try
                {
                    await using var connection = new NpgsqlConnection(_connectionString);
                    await connection.OpenAsync(_cancellationToken.Token);

                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1;";
                    await command.ExecuteScalarAsync(_cancellationToken.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception caught!", GetType().Name);
                    exception = true;
                }
                var stop = timer.EndRecording();

                if (exception)
                {
                    timer.Record(long.MaxValue, TimeUnit.Nanoseconds);
                }
                else
                {
                    var value = stop - start;
                    timer.Record(value, TimeUnit.Nanoseconds);
                    _metrics.Provider.Histogram.Instance(postgresql_response_milliseconds).Update(value);
                }

                await Task.Delay(5000);
            }

        }

        public void UpdateMetrics(IMetrics metrics)
        {
            _metrics = metrics;
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
        }
    }
}