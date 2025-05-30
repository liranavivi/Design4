using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace EntitiesManager.Api.HealthChecks;

public class OpenTelemetryHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenTelemetryHealthCheck> _logger;
    private readonly IHostEnvironment _environment;
    private static readonly ActivitySource ActivitySource = new("EntitiesManager.HealthCheck.OpenTelemetry");

    public OpenTelemetryHealthCheck(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<OpenTelemetryHealthCheck> logger,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
        _environment = environment;

        // Configure HttpClient with reasonable defaults
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "EntitiesManager-HealthCheck/1.0");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("OpenTelemetryHealthCheck");
        var healthData = new Dictionary<string, object>();

        _logger.LogInformation("Starting OpenTelemetry health check. Environment: {Environment}", _environment.EnvironmentName);

        try
        {
            // Validate configuration
            var configValidation = ValidateConfiguration();
            healthData.Add("configuration", configValidation);

            if (!configValidation.IsValid)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Configuration validation failed");
                _logger.LogWarning("OpenTelemetry configuration validation failed. Issues: {Issues}",
                    string.Join("; ", configValidation.Issues));

                return HealthCheckResult.Degraded(
                    $"OpenTelemetry configuration issues: {string.Join("; ", configValidation.Issues)}",
                    data: healthData);
            }

            var endpoint = _configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";
            var uri = new Uri(endpoint);

            activity?.SetTag("otel.endpoint", endpoint);
            activity?.SetTag("otel.host", uri.Host);
            activity?.SetTag("otel.port", uri.Port);
            activity?.SetTag("environment", _environment.EnvironmentName);

            // Perform comprehensive connectivity checks
            var connectivityResults = await PerformConnectivityChecksAsync(uri, cancellationToken);
            healthData.Add("connectivity", connectivityResults);

            activity?.SetTag("health_check.status", connectivityResults.OverallStatus);

            // Determine final health status
            var finalStatus = DetermineHealthStatus(connectivityResults, configValidation);
            var statusMessage = BuildStatusMessage(connectivityResults, configValidation);

            healthData.Add("timestamp", DateTimeOffset.UtcNow);
            healthData.Add("environment", _environment.EnvironmentName);

            _logger.LogInformation("OpenTelemetry health check completed. Status: {Status}, Endpoint: {Endpoint}",
                finalStatus, endpoint);

            return new HealthCheckResult(finalStatus, statusMessage, data: healthData);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(ex, "OpenTelemetry health check failed with exception");

            healthData.Add("error", ex.Message);
            healthData.Add("timestamp", DateTimeOffset.UtcNow);

            return HealthCheckResult.Unhealthy($"OpenTelemetry health check failed: {ex.Message}", ex, healthData);
        }
    }

    private ConfigurationValidationResult ValidateConfiguration()
    {
        var issues = new List<string>();
        var warnings = new List<string>();

        // Check endpoint configuration
        var endpoint = _configuration["OpenTelemetry:Endpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            issues.Add("OpenTelemetry:Endpoint is not configured");
        }
        else
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            {
                issues.Add($"OpenTelemetry:Endpoint '{endpoint}' is not a valid URI");
            }
            else
            {
                if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    issues.Add($"OpenTelemetry:Endpoint scheme '{uri.Scheme}' is not supported (use http or https)");
                }

                if (uri.Port <= 0 || uri.Port > 65535)
                {
                    issues.Add($"OpenTelemetry:Endpoint port '{uri.Port}' is invalid");
                }

                // Check for common misconfigurations
                if (uri.Port == 4318 && _environment.IsProduction())
                {
                    warnings.Add("Using HTTP endpoint (4318) in production - consider gRPC (4317) for better performance");
                }
            }
        }

        // Check service configuration
        var serviceName = _configuration["OpenTelemetry:ServiceName"];
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            warnings.Add("OpenTelemetry:ServiceName is not configured, using default");
        }

        // Environment-specific validations
        if (_environment.IsProduction())
        {
            var useConsole = _configuration.GetValue<bool>("OpenTelemetry:UseConsoleInDevelopment", false);
            if (useConsole)
            {
                warnings.Add("Console exporter is enabled in production environment");
            }
        }

        return new ConfigurationValidationResult
        {
            IsValid = issues.Count == 0,
            Issues = issues,
            Warnings = warnings,
            Endpoint = endpoint ?? "not configured"
        };
    }

    private async Task<ConnectivityCheckResult> PerformConnectivityChecksAsync(Uri uri, CancellationToken cancellationToken)
    {
        var results = new List<EndpointCheckResult>();

        _logger.LogDebug("Starting connectivity checks for OpenTelemetry collector. Host: {Host}, Port: {Port}",
            uri.Host, uri.Port);

        // 1. Network connectivity check (ping)
        var pingResult = await CheckNetworkConnectivityAsync(uri.Host, cancellationToken);
        results.Add(pingResult);

        // 2. Port connectivity check (TCP)
        var tcpResult = await CheckTcpConnectivityAsync(uri.Host, uri.Port, cancellationToken);
        results.Add(tcpResult);

        // 3. HTTP endpoint check (if applicable)
        if (uri.Port == 4318 || uri.Scheme == "http" || uri.Scheme == "https")
        {
            var httpResult = await CheckHttpEndpointAsync(uri, cancellationToken);
            results.Add(httpResult);
        }

        // 4. gRPC endpoint check (if applicable)
        if (uri.Port == 4317)
        {
            var grpcResult = await CheckGrpcEndpointAsync(uri, cancellationToken);
            results.Add(grpcResult);
        }

        var successfulChecks = results.Count(r => r.IsSuccessful);
        var totalChecks = results.Count;
        var overallStatus = DetermineConnectivityStatus(results);

        _logger.LogDebug("Connectivity checks completed. Successful: {Successful}/{Total}, Status: {Status}",
            successfulChecks, totalChecks, overallStatus);

        return new ConnectivityCheckResult
        {
            OverallStatus = overallStatus,
            TotalDurationMs = 0, // Remove duration tracking
            EndpointChecks = results,
            SuccessfulChecks = successfulChecks,
            TotalChecks = totalChecks,
            Host = uri.Host,
            Port = uri.Port
        };
    }

    private async Task<EndpointCheckResult> CheckNetworkConnectivityAsync(string host, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 5000);

            var isSuccessful = reply.Status == IPStatus.Success;
            var message = isSuccessful
                ? $"Ping successful ({reply.RoundtripTime}ms)"
                : $"Ping failed: {reply.Status}";

            _logger.LogDebug("Network connectivity check for {Host}: {Status}",
                host, reply.Status);

            return new EndpointCheckResult
            {
                CheckType = "Network (Ping)",
                IsSuccessful = isSuccessful,
                DurationMs = 0, // Remove duration tracking
                Message = message,
                Details = new { Status = reply.Status.ToString(), RoundtripTime = reply.RoundtripTime }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Network connectivity check failed for {Host}",
                host);

            return new EndpointCheckResult
            {
                CheckType = "Network (Ping)",
                IsSuccessful = false,
                DurationMs = 0, // Remove duration tracking
                Message = $"Ping failed: {ex.Message}",
                Details = new { Error = ex.Message }
            };
        }
    }

    private async Task<EndpointCheckResult> CheckTcpConnectivityAsync(string host, int port, CancellationToken cancellationToken)
    {
        try
        {
            using var tcpClient = new System.Net.Sockets.TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await tcpClient.ConnectAsync(host, port, cts.Token);

            _logger.LogDebug("TCP connectivity check for {Host}:{Port}: Success",
                host, port);

            return new EndpointCheckResult
            {
                CheckType = "TCP",
                IsSuccessful = true,
                DurationMs = 0, // Remove duration tracking
                Message = $"TCP connection successful to {host}:{port}",
                Details = new { Host = host, Port = port, Connected = true }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TCP connectivity check failed for {Host}:{Port}",
                host, port);

            return new EndpointCheckResult
            {
                CheckType = "TCP",
                IsSuccessful = false,
                DurationMs = 0, // Remove duration tracking
                Message = $"TCP connection failed to {host}:{port}: {ex.Message}",
                Details = new { Host = host, Port = port, Error = ex.Message }
            };
        }
    }

    private async Task<EndpointCheckResult> CheckHttpEndpointAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var response = await _httpClient.GetAsync(uri, cts.Token);

            // For OpenTelemetry collector, any response (even 404) indicates the service is running
            var isSuccessful = true;
            var message = $"HTTP endpoint responded with {response.StatusCode}";

            _logger.LogDebug("HTTP endpoint check for {Uri}: {StatusCode}",
                uri, response.StatusCode);

            return new EndpointCheckResult
            {
                CheckType = "HTTP",
                IsSuccessful = isSuccessful,
                DurationMs = 0, // Remove duration tracking
                Message = message,
                Details = new
                {
                    Uri = uri.ToString(),
                    StatusCode = (int)response.StatusCode,
                    StatusDescription = response.StatusCode.ToString(),
                    Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTTP endpoint check failed for {Uri}",
                uri);

            return new EndpointCheckResult
            {
                CheckType = "HTTP",
                IsSuccessful = false,
                DurationMs = 0, // Remove duration tracking
                Message = $"HTTP endpoint check failed: {ex.Message}",
                Details = new { Uri = uri.ToString(), Error = ex.Message }
            };
        }
    }

    private async Task<EndpointCheckResult> CheckGrpcEndpointAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            // For gRPC health check, we'll try to connect to the TCP port
            // and look for gRPC-specific responses or just verify connectivity
            var host = uri.Host;
            var port = uri.Port;

            using var tcpClient = new System.Net.Sockets.TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await tcpClient.ConnectAsync(host, port, cts.Token);

            // If we can connect, assume gRPC service is available
            // In a more sophisticated implementation, we could send a gRPC health check request
            _logger.LogDebug("gRPC endpoint check for {Host}:{Port}: Connection successful",
                host, port);

            return new EndpointCheckResult
            {
                CheckType = "gRPC",
                IsSuccessful = true,
                DurationMs = 0, // Remove duration tracking
                Message = $"gRPC endpoint appears accessible at {host}:{port}",
                Details = new { Host = host, Port = port, Protocol = "gRPC", Connected = true }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "gRPC endpoint check failed for {Uri}",
                uri);

            return new EndpointCheckResult
            {
                CheckType = "gRPC",
                IsSuccessful = false,
                DurationMs = 0, // Remove duration tracking
                Message = $"gRPC endpoint check failed: {ex.Message}",
                Details = new { Uri = uri.ToString(), Protocol = "gRPC", Error = ex.Message }
            };
        }
    }

    private string DetermineConnectivityStatus(List<EndpointCheckResult> results)
    {
        var successCount = results.Count(r => r.IsSuccessful);
        var totalCount = results.Count;

        if (successCount == totalCount) return "Healthy";
        if (successCount > 0) return "Degraded";
        return "Unhealthy";
    }

    private HealthStatus DetermineHealthStatus(ConnectivityCheckResult connectivity, ConfigurationValidationResult config)
    {
        if (!config.IsValid) return HealthStatus.Degraded;

        return connectivity.OverallStatus switch
        {
            "Healthy" => HealthStatus.Healthy,
            "Degraded" => HealthStatus.Degraded,
            _ => HealthStatus.Unhealthy
        };
    }

    private string BuildStatusMessage(ConnectivityCheckResult connectivity, ConfigurationValidationResult config)
    {
        var parts = new List<string>();

        if (config.IsValid)
        {
            parts.Add($"OpenTelemetry collector connectivity: {connectivity.OverallStatus}");
            parts.Add($"({connectivity.SuccessfulChecks}/{connectivity.TotalChecks} checks passed)");
        }
        else
        {
            parts.Add("Configuration issues detected");
        }

        return string.Join(" | ", parts);
    }
}

// Data classes
public class ConfigurationValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string Endpoint { get; set; } = string.Empty;
}

public class ConnectivityCheckResult
{
    public string OverallStatus { get; set; } = string.Empty;
    public long TotalDurationMs { get; set; }
    public List<EndpointCheckResult> EndpointChecks { get; set; } = new();
    public int SuccessfulChecks { get; set; }
    public int TotalChecks { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}

public class EndpointCheckResult
{
    public string CheckType { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public long DurationMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}
