using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;

namespace Kaz.Http.Core
{
    internal class ApiInternalResponse
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public HttpResponseHeaders? Headers { get; set; }
    }

    internal class CircuitBreaker
    {

        private CancellationTokenSource cancellationTokenSource =
            new CancellationTokenSource();

        public CancellationToken CancellationToken =>
            cancellationTokenSource.Token;

        private static readonly ConcurrentDictionary<string, CircuitBreaker> circuitBreakers =
            new ConcurrentDictionary<string, CircuitBreaker>();

        private int failureCount;

        public static int FailureThreshold { get; set; } = 3;

        private DateTimeOffset openedAt;
        private CircuitBreakerState state = CircuitBreakerState.Closed;

        public static TimeSpan RecoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);
        internal enum CircuitBreakerState
        {
            Closed,
            Open,
            HalfOpen
        }

        private readonly object _lock = new object();

        public void RecordFailure()
        {
            lock (_lock)
            {
                if (state == CircuitBreakerState.HalfOpen)
                {
                    state = CircuitBreakerState.Open;
                    openedAt = DateTimeOffset.UtcNow;
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                    return;
                }

                failureCount++;

                if (failureCount >= FailureThreshold)
                {
                    state = CircuitBreakerState.Open;
                    openedAt = DateTimeOffset.UtcNow;
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                }
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                state = CircuitBreakerState.Closed;
                failureCount = 0;
            }
        }

        public bool CanRequest()
        {
            lock (_lock)
            {
                switch (state)
                {
                    case CircuitBreakerState.Closed:
                        return true;

                    case CircuitBreakerState.HalfOpen:
                        return false;

                    case CircuitBreakerState.Open:
                        if (DateTimeOffset.UtcNow - openedAt >= RecoveryTimeout)
                        {
                            state = CircuitBreakerState.HalfOpen;
                            return true;
                        }
                        return false;
                }
                return false;
            }
        }


        public static CircuitBreaker GetCircuitBreaker(string url)
        {
            string host = new Uri(url).Host;
            return circuitBreakers.GetOrAdd(host, h => new CircuitBreaker(h));
        }

        public CircuitBreaker(string host) { }
    }

    internal class RateLimiter
    {
        private int _maxRequests;

        private TimeSpan _period;

        private int _currentRequests;

        private DateTimeOffset _periodStart;

        private object _lock = new object();

        private static readonly ConcurrentDictionary<string, RateLimiter> rateLimiters =
            new ConcurrentDictionary<string, RateLimiter>();

        public RateLimiter(int maxRequests, TimeSpan period)
        {
            _maxRequests = maxRequests;
            _period = period;
            _periodStart = DateTimeOffset.UtcNow;
        }

        public async Task WaitIfNeededAsync(CancellationToken cancellationToken = default)
        {
            TimeSpan delay = TimeSpan.Zero;

            lock (_lock)
            {
                TimeSpan elapsed = DateTimeOffset.UtcNow - _periodStart;

                if (elapsed >= _period)
                {
                    _currentRequests = 0;
                    _periodStart = DateTimeOffset.UtcNow;
                }

                if (_currentRequests >= _maxRequests)
                {
                    delay = _period - elapsed;
                }
                else
                {
                    _currentRequests++;
                }
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken);

                lock (_lock)
                {
                    _currentRequests = 0;
                    _periodStart = DateTimeOffset.UtcNow;
                    _currentRequests++;
                }
            }
        }

        public static RateLimiter GetRateLimiter(string url)
        {
            string host = new Uri(url).Host;
            return rateLimiters.GetOrAdd(host, r => new RateLimiter(Client.RateLimiterMaxRequests, Client.RateLimiterPeriod));
        }
    }
}
