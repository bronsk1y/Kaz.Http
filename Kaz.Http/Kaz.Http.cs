using Kaz.Http.Caching;
using Kaz.Http.Core;
using Kaz.Http.Metrics;
using Kaz.Http.Security;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Kaz.Http
{
    /// <summary>
    /// Represents a typed HTTP API response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Gets a value indicating whether the request was successful.
        /// </summary>
        public bool IsSuccess { get; internal set; }

        /// <summary>
        /// Gets the HTTP status code returned by the server.
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// Gets the error message if the request failed.
        /// </summary>
        public string? ErrorMessage { get; internal set; }

        /// <summary>
        /// Gets the deserialized response data.
        /// </summary>
        public T? Data { get; internal set; }

        /// <summary>
        /// Gets the response headers returned by the server.
        /// </summary>
        public HttpResponseHeaders? Headers { get; internal set; }
    }

    /// <summary>
    /// Provides a HTTP client with retry, caching, rate limiting,
    /// circuit breaking, fallback routing, bulkhead isolation, telemetry
    /// and request signing.
    /// </summary>
    public static class Client
    {
        #region Cofigurations

        /// <summary>
        /// Gets or sets the number of retry attempts for failed requests.
        /// </summary>
        public static int RetryCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial retry delay in milliseconds.
        /// </summary>
        public static int RetryDelay { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the circuit breaker failure threshold.
        /// </summary>
        public static int CircuitBreakerFailureThreshold
        {
            get => CircuitBreaker.FailureThreshold;
            set => CircuitBreaker.FailureThreshold = value;
        }

        /// <summary>
        /// Gets or sets the circuit breaker recovery timeout.
        /// </summary>
        public static TimeSpan CircuitBreakerRecoveryTimeout
        {
            get => CircuitBreaker.RecoveryTimeout;
            set => CircuitBreaker.RecoveryTimeout = value;
        }

        /// <summary>
        /// Gets or sets the maximum number of allowed requests per rate‑limiting period.
        /// </summary>
        public static int RateLimiterMaxRequests { get; set; } = 100;

        /// <summary>
        /// Gets or sets the rate‑limiting time window.
        /// </summary>
        public static TimeSpan RateLimiterPeriod { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Gets or sets the signing key used for request signatures.
        /// </summary>
        public static string? SigningKey { get; set; }

        /// <summary>
        /// Adds a default header to all outgoing HTTP requests.
        /// </summary>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value.</param>
        public static void AddDefaultHeader(string name, string value) =>
            httpClient.DefaultRequestHeaders.Add(name, value);

        /// <summary>
        /// Gets the telemetry instance used for metrics collection.
        /// </summary>
        public static Telemetry Telemetry => TelemetryLogger.GetTelemetry;

        private static readonly HttpClient httpClient = new HttpClient();

        private static readonly JsonSerializerOptions jsonSerializerOptions =
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private static readonly ConcurrentDictionary<string, Task<ApiInternalResponse>> activeRequests =
            new ConcurrentDictionary<string, Task<ApiInternalResponse>>();

        private static readonly ConcurrentDictionary<string, string> fallbackUrls =
            new ConcurrentDictionary<string, string>();

        private static readonly ConcurrentDictionary<Type, Delegate> contracts =
            new ConcurrentDictionary<Type, Delegate>();

        private static readonly ConcurrentDictionary<string, TimeSpan> timeouts =
            new ConcurrentDictionary<string, TimeSpan>();

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> bulkheads =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        #endregion

        #region GET

        /// <summary>
        /// Sends a GET request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <returns>A typed API response containing the deserialized data.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<T>> GetAsync<T>(string url) where T : class
        {
            try
            {
                return await GetWithCacheAsync<T>(url);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Sends an authenticated GET request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <returns>A typed API response containing the deserialized data.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<T>> GetAsync<T>(string url, string apiKey) where T : class
        {
            try
            {
                return await GetWithCacheAsync<T>(url, apiKey);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region POST

        /// <summary>
        /// Sends a POST request with a JSON body to the specified URL.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="data">The request payload.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, TRequest data)
            where TResponse : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Content = JsonContent.Create(data);
                    return request;
                }, url, HttpMethod.Post);

                return BuildApiResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Sends an authenticated POST request with a JSON body to the specified URL.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <param name="data">The request payload.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string url, string apiKey, TRequest data)
            where TResponse : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = JsonContent.Create(data);
                    return request;
                }, url, HttpMethod.Post);

                return BuildApiResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region PUT

        /// <summary>
        /// Sends a PUT request with a JSON body to the specified URL.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="data">The request payload.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, TRequest data)
            where TResponse : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, url);
                    request.Content = JsonContent.Create(data);
                    return request;
                }, url, HttpMethod.Put);

                return BuildApiResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Sends an authenticated PUT request with a JSON body to the specified URL.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <param name="data">The request payload.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string url, string apiKey, TRequest data)
            where TResponse : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Put, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = JsonContent.Create(data);
                    return request;
                }, url, HttpMethod.Put);

                return BuildApiResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region PATCH

        /// <summary>
        /// Sends a PATCH request with a JSON body to the specified URL.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="data">The request payload.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string url, TRequest data)
            where TResponse : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Patch, url);
                    request.Content = JsonContent.Create(data);
                    return request;
                }, url, HttpMethod.Patch);

                return BuildApiResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Sends an authenticated PATCH request with a JSON body to the specified URL.
        /// </summary>
        /// <typeparam name="TRequest">The request body type.</typeparam>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <param name="data">The request payload.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string url, string apiKey, TRequest data)
            where TResponse : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Patch, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = JsonContent.Create(data);
                    return request;
                }, url, HttpMethod.Patch);

                return BuildApiResponse<TResponse>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<TResponse>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region DELETE

        /// <summary>
        /// Sends a DELETE request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<T>> DeleteAsync<T>(string url) where T : class
        {
            try
            {
                var response = await SendWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Delete, url),
                    url,
                    HttpMethod.Delete);

                return BuildApiResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Sends an authenticated DELETE request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<T>> DeleteAsync<T>(string url, string apiKey) where T : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    return request;
                }, url, HttpMethod.Delete);

                return BuildApiResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region HEAD

        /// <summary>
        /// Sends a HEAD request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<T>> HeadAsync<T>(string url) where T : class
        {
            try
            {
                var response = await SendWithRetryAsync(
                    () => new HttpRequestMessage(HttpMethod.Head, url),
                    url,
                    HttpMethod.Head,
                    HttpCompletionOption.ResponseHeadersRead);

                return BuildApiResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Sends an authenticated HEAD request to the specified URL.
        /// </summary>
        /// <typeparam name="T">The expected response type.</typeparam>
        /// <param name="url">The request URL.</param>
        /// <param name="apiKey">The API key used for authentication.</param>
        /// <returns>A typed API response.</returns>
        /// <exception cref="HttpRequestException">Thrown when the request fails after all retry attempts.</exception>
        public static async Task<ApiResponse<T>> HeadAsync<T>(string url, string apiKey) where T : class
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    return request;
                },
                    url,
                    HttpMethod.Head,
                    HttpCompletionOption.ResponseHeadersRead);

                return BuildApiResponse<T>(response);
            }
            catch (HttpRequestException ex)
            {
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    StatusCode = HttpStatusCode.InternalServerError,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        private static ApiResponse<T> BuildApiResponse<T>(ApiInternalResponse response) where T : class
        {
            var result = new ApiResponse<T>
            {
                StatusCode = response.StatusCode,
                IsSuccess = response.IsSuccess,
                Headers = response.Headers
            };

            if (response.IsSuccess)
            {
                try
                {
                    result.Data = JsonSerializer.Deserialize<T>(response.Content, jsonSerializerOptions);

                    if (result.Data != null)
                    {
                        string? validationResult = ValidateContract(result.Data);

                        if (validationResult != null)
                        {
                            result.ErrorMessage = validationResult;
                            result.IsSuccess = false;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    result.ErrorMessage = ex.Message;
                }
            }
            else
            {
                result.ErrorMessage = response.Content;
            }

            return result;
        }

        private static async Task<ApiInternalResponse> PerformRequest(Func<HttpRequestMessage> factory, string key, bool isGet, string url, HttpCompletionOption options = HttpCompletionOption.ResponseContentRead)
        {
            var stopwatch = TelemetryLogger.StartMeasurement();

            try
            {
                var host = new Uri(url).Host;
                int delay = RetryDelay;
                var circuitBreaker = CircuitBreaker.GetCircuitBreaker(url);
                var rateLimiter = RateLimiter.GetRateLimiter(url);
                GetTimeout(host, out TimeSpan timeout);
                GetBulkhead(host, out SemaphoreSlim? semaphore);

                if (!circuitBreaker.CanRequest())
                {
                    return new ApiInternalResponse
                    {
                        IsSuccess = false,
                        StatusCode = HttpStatusCode.ServiceUnavailable,
                        ErrorMessage = "Circuit breaker is open, all requests are blocked."
                    };
                }

                using var timeoutToken = new CancellationTokenSource(timeout);
                using var linkedTokens = CancellationTokenSource.CreateLinkedTokenSource(circuitBreaker.CancellationToken, timeoutToken.Token);
                var cancellationToken = linkedTokens.Token;

                for (int i = 0; i < RetryCount; i++)
                {
                    TelemetryLogger.UpdateTotalRequests();

                    await rateLimiter.WaitIfNeededAsync(cancellationToken);

                    using var request = factory();
                    ApplySignature(request, url);

                    if (semaphore != null)
                        await semaphore.WaitAsync();

                    try
                    {
                        using var response = await httpClient.SendAsync(request, options, cancellationToken);
                        string content = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            circuitBreaker.RecordSuccess();
                            return new ApiInternalResponse
                            {
                                StatusCode = response.StatusCode,
                                Content = content,
                                IsSuccess = true,
                                Headers = response.Headers
                            };
                        }

                        switch ((int)response.StatusCode)
                        {
                            case >= 500:

                                TelemetryLogger.UpdateErrors();

                                circuitBreaker.RecordFailure();
                                if (i < RetryCount - 1)
                                {
                                    await Task.Delay(delay, cancellationToken);
                                    delay *= 2;
                                    continue;
                                }

                                if (GetFallbackUrl(url, out string? fallbackUrl) && fallbackUrl != null)
                                {
                                    string fallbackUrlKey = CacheService.SHA256Algorithm(fallbackUrl);
                                    return await PerformRequest(() =>
                                    {
                                        using var fallbackRequest = factory();
                                        return new HttpRequestMessage(fallbackRequest.Method, fallbackUrl);
                                    }, fallbackUrlKey, isGet, fallbackUrl, options);
                                }

                                break;

                            case 429:

                                TelemetryLogger.UpdateErrors();

                                var retryConditionHeaderValue = response.Headers.RetryAfter;

                                if (retryConditionHeaderValue != null && i < RetryCount - 1)
                                {
                                    if (retryConditionHeaderValue.Delta != null)
                                    {
                                        var retryConditionDelay = retryConditionHeaderValue.Delta;
                                        if (retryConditionDelay < TimeSpan.Zero)
                                        {
                                            retryConditionDelay = TimeSpan.Zero;
                                        }

                                        await Task.Delay(retryConditionDelay.Value, cancellationToken);
                                    }
                                    else if (retryConditionHeaderValue.Date != null)
                                    {
                                        var retryConditionDelay = retryConditionHeaderValue.Date - DateTimeOffset.UtcNow;
                                        if (retryConditionDelay < TimeSpan.Zero)
                                        {
                                            retryConditionDelay = TimeSpan.Zero;
                                        }

                                        await Task.Delay(retryConditionDelay.Value, cancellationToken);
                                    }
                                    else
                                    {
                                        await Task.Delay(RetryDelay, cancellationToken);
                                    }
                                }
                                else
                                {
                                    break;
                                }

                                continue;
                        }

                        if (isGet && i == RetryCount - 1 && CacheProvider.TryGetCache(key, out string? cached) && cached != null)
                        {
                            return new ApiInternalResponse
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = cached,
                                IsSuccess = true
                            };
                        }

                        return new ApiInternalResponse
                        {
                            StatusCode = response.StatusCode,
                            Content = content,
                            IsSuccess = false,
                            Headers = response.Headers
                        };
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        TelemetryLogger.UpdateErrors();

                        circuitBreaker.RecordFailure();
                        if (isGet && i == RetryCount - 1 && CacheProvider.TryGetCache(key, out string? cached) && cached != null)
                        {
                            return new ApiInternalResponse
                            {
                                StatusCode = HttpStatusCode.OK,
                                Content = cached,
                                IsSuccess = true,
                                ErrorMessage = ex.Message
                            };
                        }

                        if (i == RetryCount - 1)
                        {
                            if (GetFallbackUrl(url, out string? fallbackUrl) && fallbackUrl != null)
                            {
                                string fallbackUrlKey = CacheService.SHA256Algorithm(fallbackUrl);
                                return await PerformRequest(() =>
                                {
                                    using var fallbackRequest = factory();
                                    return new HttpRequestMessage(fallbackRequest.Method, fallbackUrl);
                                }, fallbackUrlKey, isGet, fallbackUrl, options);
                            }
                            throw;
                        }

                        await Task.Delay(delay, cancellationToken);
                        delay *= 2;
                    }
                    finally
                    {
                        if (semaphore != null)
                            semaphore.Release();
                    }
                }

                TelemetryLogger.UpdateErrors();

                throw new HttpRequestException("Unknown error occurred.");
            }
            finally
            {
                TelemetryLogger.StopMeasurement(stopwatch);
            }
        }

        private static async Task<ApiInternalResponse> SendWithRetryAsync(Func<HttpRequestMessage> factory, string url, HttpMethod method,
            HttpCompletionOption options = HttpCompletionOption.ResponseContentRead, string? suffix = null)
        {
            string key = CacheService.SHA256Algorithm(url + suffix);
            bool isGet = method == HttpMethod.Get;

            if (!isGet)
                return await PerformRequest(factory, key, false, url, options);

            return await activeRequests.GetOrAdd(key,
                _ => ExecuteAndClear(factory, key, url, options));
        }

        private static async Task<ApiInternalResponse> ExecuteAndClear(Func<HttpRequestMessage> factory, string key, string url,
            HttpCompletionOption options = HttpCompletionOption.ResponseContentRead)
        {
            try
            {
                return await PerformRequest(factory, key, true, url, options); ;
            }
            finally
            {
                activeRequests.TryRemove(key, out _);
            }
        }

        private static async Task<ApiResponse<T>> GetWithCacheAsync<T>(string url) where T : class
        {
            string key = CacheService.SHA256Algorithm(url);

            if (!CacheProvider.TryGetETag(key, out string? _))
            {
                if (CacheProvider.TryGetCache(key, out string? cached) && cached != null)
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<T>(cached, jsonSerializerOptions);
                        if (data != null)
                            return new ApiResponse<T>
                            {
                                IsSuccess = true,
                                StatusCode = HttpStatusCode.OK,
                                Data = data
                            };
                    }
                    catch (JsonException) { }
                }
            }

            var response = await SendWithRetryAsync(() =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, url);

                    if (CacheProvider.TryGetETag(key, out string? etag) && etag != null)
                    {
                        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
                    }

                    return request;

                }, url,
                HttpMethod.Get);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                if (CacheProvider.TryGetCache(key, out string? cached) && cached != null)
                {
                    return BuildApiResponse<T>(new ApiInternalResponse
                    {
                        IsSuccess = true,
                        StatusCode = HttpStatusCode.OK,
                        Content = cached
                    });
                }
            }

            if (response.IsSuccess)
            {
                CacheProvider.SetCache(key, response.Content);

                if (response.Headers?.ETag != null)
                {
                    CacheProvider.SetETag(key, response.Headers.ETag.Tag);
                }
            }

            return BuildApiResponse<T>(response);
        }

        private static async Task<ApiResponse<T>> GetWithCacheAsync<T>(string url, string apiKey) where T : class
        {
            string key = CacheService.SHA256Algorithm(url + apiKey);

            if (!CacheProvider.TryGetETag(key, out string? _))
            {
                if (CacheProvider.TryGetCache(key, out string? cached) && cached != null)
                {
                    try
                    {
                        var data = JsonSerializer.Deserialize<T>(cached, jsonSerializerOptions);
                        if (data != null)
                            return new ApiResponse<T>
                            {
                                IsSuccess = true,
                                StatusCode = HttpStatusCode.OK,
                                Data = data
                            };
                    }
                    catch (JsonException) { }
                }
            }


            var response = await SendWithRetryAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);

                if (CacheProvider.TryGetETag(key, out string? etag) && etag != null)
                {
                    request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(etag));
                }

                return request;
            },
            url,
            HttpMethod.Get, HttpCompletionOption.ResponseContentRead, apiKey);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                if (CacheProvider.TryGetCache(key, out string? cached) && cached != null)
                {
                    return BuildApiResponse<T>(new ApiInternalResponse
                    {
                        IsSuccess = true,
                        StatusCode = HttpStatusCode.OK,
                        Content = cached
                    });
                }
            }

            if (response.IsSuccess)
            {
                CacheProvider.SetCache(key, response.Content);

                if (response.Headers?.ETag != null)
                {
                    CacheProvider.SetETag(key, response.Headers.ETag.Tag);
                }
            }

            return BuildApiResponse<T>(response);
        }

        /// <summary>
        /// Registers a fallback URL for the specified primary endpoint.
        /// </summary>
        /// <param name="primaryUrl">The primary URL.</param>
        /// <param name="fallbackUrl">The fallback URL used when the primary endpoint fails.</param>
        public static void RegisterFallbackUrl(string primaryUrl, string fallbackUrl)
        {
            fallbackUrls.AddOrUpdate(primaryUrl, fallbackUrl, (key, oldUrl) => fallbackUrl);
        }

        private static bool GetFallbackUrl(string url, out string? fallbackUrl)
        {
            return fallbackUrls.TryGetValue(url, out fallbackUrl);
        }

        /// <summary>
        /// Adds a validation contract for the specified response type.
        /// </summary>
        /// <typeparam name="T">The response type.</typeparam>
        /// <param name="contract">A delegate that validates the response and returns an error message or null.</param>
        public static void AddContract<T>(Func<T, string?> contract) where T : class
        {
            contracts[typeof(T)] = contract;
        }

        private static string? ValidateContract<T>(T data) where T : class
        {
            if (contracts.TryGetValue(typeof(T), out Delegate? contract))
            {
                var typedContract = contract as Func<T, string?>;
                return typedContract?.Invoke(data);
            }
            return null;
        }

        /// <summary>
        /// Sets a custom timeout for requests targeting the specified URL.
        /// </summary>
        /// <param name="url">The target URL.</param>
        /// <param name="timeout">The timeout value.</param>
        public static void SetTimeout(string url, TimeSpan timeout)
        {
            var host = new Uri(url).Host;
            timeouts[host] = timeout;
        }

        private static bool GetTimeout(string url, out TimeSpan timeout)
        {
            if (timeouts.TryGetValue(url, out timeout))
            {
                return true;
            }

            timeout = TimeSpan.FromSeconds(100);
            return false;
        }

        /// <summary>
        /// Sets a bulkhead limit for the specified URL, restricting concurrent requests.
        /// </summary>
        /// <param name="url">The target URL.</param>
        /// <param name="maxConcurrent">The maximum number of concurrent requests.</param>
        public static void SetBulkhead(string url, int maxConcurrent)
        {
            var host = new Uri(url).Host;
            bulkheads[host] = new SemaphoreSlim(maxConcurrent);
        }

        private static bool GetBulkhead(string url, out SemaphoreSlim? semaphore)
        {
            if (bulkheads.TryGetValue(url, out semaphore))
            {
                return true;
            }

            semaphore = null;
            return false;
        }

        private static void ApplySignature(HttpRequestMessage request, string url)
        {
            if (SigningKey == null) return;

            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            string dataToSign = url + timestamp;
            string signature = HmacSha256.ComputeSignature(dataToSign, SigningKey);

            request.Headers.Add("X-Timestamp", timestamp.ToString());
            request.Headers.Add("X-Signature", signature);
        }
    }
}