<p align="center">
  <img width="300" alt="KazHttpIcon" src="kaz-http-icon.png" />
</p>

<h1 align="center">Kaz.Http</h1>

<p align="center">
  A resilient HTTP client for .NET 6+ with retry, caching, rate limiting,<br/>
  circuit breaker, bulkhead isolation, fallback routing, telemetry, and request signing.
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Kaz.Http"><img src="https://img.shields.io/nuget/dt/Kaz.Http" alt="NuGet Version"></a>
  <img src="https://img.shields.io/badge/.NET-6%2B-purple" alt=".NET 6+">
  <img src="https://img.shields.io/badge/language-C%23-239120?logo=csharp" alt="Language">
</p>

<p align="center">
  <a href="https://github.com/bronsk1y/Kaz.Http/releases/latest"><img src="https://img.shields.io/github/v/release/bronsk1y/Kaz.Http?label=latest%20release&color=brightgreen" alt="Latest Release"></a>
  <a href="https://github.com/bronsk1y/Kaz.Http/releases"><img src="https://img.shields.io/github/release-date/bronsk1y/Kaz.Http?color=blue" alt="Release Date"></a>
  <a href="https://github.com/bronsk1y/Kaz.Http/releases"><img src="https://img.shields.io/github/downloads/bronsk1y/Kaz.Http/total?color=orange" alt="GitHub Downloads"></a>
  <a href="https://github.com/bronsk1y/Kaz.Http/releases"><img src="https://img.shields.io/github/commits-since/bronsk1y/Kaz.Http/latest?color=yellow" alt="Commits since release"></a>
</p>

---

## Installation

```bash
dotnet add package Kaz.Http
```

```powershell
Install-Package Kaz.Http
```

---

## What's Inside

| Feature | Description |
|---|---|
| Retry | Automatic retry with exponential backoff and 429 Retry-After support |
| Caching | ETag-based response caching with deduplication of concurrent GET requests |
| Rate Limiting | Per-host request rate limiting with configurable window |
| Circuit Breaker | Prevents cascading failures with configurable threshold and recovery timeout |
| Bulkhead | Limits concurrent requests per host via semaphore isolation |
| Fallback Routing | Redirects to a backup URL when the primary fails after all retries |
| Request Signing | Adds HMAC-SHA256 `X-Timestamp` and `X-Signature` headers |
| Contracts | Post-deserialization response validation with custom rules |
| Telemetry | Tracks total requests, errors, and last request duration |

## How to use

<details>
<summary><b>GET</b></summary>

```csharp
var response = await Client.GetAsync<WeatherData>("https://api.example.com/weather");

if (response.IsSuccess)
    Console.WriteLine(response.Data);

// Authenticated
var response = await Client.GetAsync<WeatherData>("https://api.example.com/weather", apiKey);
```
</details>

<details>
<summary><b>POST</b></summary>

```csharp
var response = await Client.PostAsync<CreateUserRequest, UserResponse>(
    "https://api.example.com/users",
    new CreateUserRequest { Name = "Alice" });

// Authenticated
var response = await Client.PostAsync<CreateUserRequest, UserResponse>(
    "https://api.example.com/users", apiKey,
    new CreateUserRequest { Name = "Alice" });
```
</details>

<details>
<summary><b>PUT</b></summary>

```csharp
var response = await Client.PutAsync<UpdateUserRequest, UserResponse>(
    "https://api.example.com/users/1",
    new UpdateUserRequest { Name = "Bob" });
```
</details>

<details>
<summary><b>PATCH</b></summary>

```csharp
var response = await Client.PatchAsync<PatchRequest, UserResponse>(
    "https://api.example.com/users/1",
    new PatchRequest { Name = "Bob" });
```
</details>

<details>
<summary><b>DELETE</b></summary>

```csharp
var response = await Client.DeleteAsync<DeleteResponse>("https://api.example.com/users/1");
```
</details>

<details>
<summary><b>HEAD</b></summary>

```csharp
// Returns headers only — no body is read
var response = await Client.HeadAsync<object>("https://api.example.com/users");

if (response.IsSuccess)
    Console.WriteLine(response.Headers);
```
</details>

---

## Configurations

<details>
<summary><b>Retry</b></summary>

```csharp
Client.RetryCount = 3;     // default: 3
Client.RetryDelay = 1000;  // initial delay in ms, doubles on each retry (default: 1000)
```
</details>

<details>
<summary><b>Circuit Breaker</b></summary>

```csharp
Client.CircuitBreakerFailureThreshold = 3;                        // default: 3
Client.CircuitBreakerRecoveryTimeout = TimeSpan.FromSeconds(30);  // default: 30s
```
</details>

<details>
<summary><b>Rate Limiting</b></summary>

```csharp
Client.RateLimiterMaxRequests = 100;                 // default: 100
Client.RateLimiterPeriod = TimeSpan.FromSeconds(1);  // default: 1s
```
</details>

<details>
<summary><b>Timeout</b></summary>

```csharp
Client.SetTimeout("https://api.example.com", TimeSpan.FromSeconds(10));
```
</details>

<details>
<summary><b>Bulkhead</b></summary>

```csharp
Client.SetBulkhead("https://api.example.com", maxConcurrent: 5);
```
</details>

<details>
<summary><b>Fallback URL</b></summary>

```csharp
Client.RegisterFallbackUrl(
    "https://api.example.com/data",
    "https://backup.example.com/data");
```
</details>

<details>
<summary><b>Request Signing</b></summary>

```csharp
Client.SigningKey = "your-secret-key";
```
</details>

<details>
<summary><b>Response Contracts</b></summary>

```csharp
Client.AddContract<UserResponse>(user =>
    user.Id > 0 ? null : "Invalid user ID.");
```
</details>

<details>
<summary><b>Default Headers</b></summary>

```csharp
Client.AddDefaultHeader("X-App-Version", "2.0.0");
```
</details>

<details>
<summary><b>Telemetry</b></summary>

```csharp
Console.WriteLine(Client.Telemetry.TotalRequests);  // total requests sent
Console.WriteLine(Client.Telemetry.Errors);         // total failed requests
Console.WriteLine(Client.Telemetry.Duration);       // last request duration in ms
```
</details>

---

## License

This project is distributed under the MIT License — free for personal and commercial use.

---

## Contact

If you want to contact me about the NuGet package, collaboration, or any development-related questions, feel free to reach out through the links below:

[![NuGet](https://img.shields.io/badge/NuGet-Kaz.Http-blue?logo=nuget)](https://www.nuget.org/packages/Kaz.Http)
