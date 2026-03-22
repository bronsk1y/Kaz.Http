using System.Diagnostics;

namespace Kaz.Http.Metrics
{
    /// <summary>
    /// Provides basic telemetry metrics for the HTTP client.
    /// </summary>
    public class Telemetry
    {
        /// <summary>
        /// Gets the total number of processed requests.
        /// </summary>
        public long TotalRequests { get; internal set; }

        /// <summary>
        /// Gets the total number of failed requests.
        /// </summary>
        public long Errors { get; internal set; }

        /// <summary>
        /// Gets the average request duration in milliseconds.
        /// </summary>
        public double Duration { get; internal set; }
    }

    internal static class TelemetryLogger
    {
        private static readonly Telemetry telemetry =
            new Telemetry();

        private static readonly object _lock = new object();

        public static Telemetry GetTelemetry => telemetry;

        public static void UpdateTotalRequests()
        {
            lock (_lock)
            {
                telemetry.TotalRequests++;
            }
        }          

        public static void UpdateErrors()
        {
            lock (_lock)
            {
                telemetry.Errors++;
            }
        }
            
        public static Stopwatch StartMeasurement()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            return stopwatch;
        }

        public static void StopMeasurement(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            telemetry.Duration = stopwatch.Elapsed.TotalMilliseconds;
        }
    }
}
