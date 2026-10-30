using ApiPulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiPulse.Api.Services;

public class MonitorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitorBackgroundService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public MonitorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MonitorBackgroundService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("API Pulse background monitoring service started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckMonitorsAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An unexpected error occurred while checking monitors.");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task CheckMonitorsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var monitors = await context.Monitors
            .Where(monitor => monitor.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var monitor in monitors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            bool isSuccessful = false;
            int? responseTimeMs = null;
            string? errorMessage = null;

            try
            {
               var httpClient = _httpClientFactory.CreateClient("MonitorClient");
                using var response = await httpClient.GetAsync(
                    monitor.Url,
                    cancellationToken);

                stopwatch.Stop();

                responseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                isSuccessful = response.IsSuccessStatusCode;

                if (!isSuccessful)
                {
                    errorMessage = $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}";
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                errorMessage = exception.Message;
            }

            monitor.Status = isSuccessful ? "Up" : "Down";
            monitor.LastCheckedAt = DateTime.UtcNow;

            context.MonitorHistories.Add(new ApiPulse.Api.Models.MonitorHistory
            {
                MonitorId = monitor.Id,
                CheckedAt = DateTime.UtcNow,
                IsSuccessful = isSuccessful,
                ResponseTimeMs = responseTimeMs,
                ErrorMessage = errorMessage
            });

            _logger.LogInformation(
                "Monitor {MonitorName} ({MonitorUrl}) is {Status}.",
                monitor.Name,
                monitor.Url,
                monitor.Status);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}