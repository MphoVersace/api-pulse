
using System.Net;
using System.Net.Http.Json;
using ApiPulse.Api.Data;
using MonitorHistoryEntity = ApiPulse.Api.Models.MonitorHistory;
using MonitorEntity = ApiPulse.Api.Models.Monitor;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPulse.Tests;

public class MonitorAnalyticsTests
{
    [Fact]
    public async Task GetMonitorAnalytics_WithHistory_ReturnsCorrectAnalytics()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        int monitorId;

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var monitor = new MonitorEntity
            {
                Name = "Google",
                Url = "https://www.google.com",
                IsActive = true
            };

            database.Monitors.Add(monitor);
            await database.SaveChangesAsync();

            monitorId = monitor.Id;

            database.MonitorHistories.AddRange(
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = true,
                    ResponseTimeMs = 100,
                    CheckedAt = DateTime.UtcNow.AddMinutes(-3)
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = true,
                    ResponseTimeMs = 200,
                    CheckedAt = DateTime.UtcNow.AddMinutes(-2)
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = false,
                    ResponseTimeMs = 300,
                    ErrorMessage = "Server error",
                    CheckedAt = DateTime.UtcNow.AddMinutes(-1)
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = false,
                    ResponseTimeMs = 400,
                    ErrorMessage = "Connection failed",
                    CheckedAt = DateTime.UtcNow
                });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/analytics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var analytics = await response.Content
            .ReadFromJsonAsync<AnalyticsResponse>();

        Assert.NotNull(analytics);
        Assert.Equal(monitorId, analytics.MonitorId);
        Assert.Equal("Google", analytics.MonitorName);
        Assert.Equal(50, analytics.UptimePercentage);
        Assert.Equal(50, analytics.DowntimePercentage);
        Assert.Equal(250, analytics.AverageResponseTimeMs);
        Assert.Equal(4, analytics.TotalChecks);
        Assert.Equal(2, analytics.SuccessfulChecks);
        Assert.Equal(2, analytics.FailedChecks);
    }

    [Fact]
    public async Task GetMonitorAnalytics_WithNoHistory_ReturnsZeroValues()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        int monitorId;

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var monitor = new MonitorEntity
            {
                Name = "GitHub",
                Url = "https://github.com",
                IsActive = true
            };

            database.Monitors.Add(monitor);
            await database.SaveChangesAsync();

            monitorId = monitor.Id;
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/analytics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var analytics = await response.Content
            .ReadFromJsonAsync<AnalyticsResponse>();

        Assert.NotNull(analytics);
        Assert.Equal(monitorId, analytics.MonitorId);
        Assert.Equal("GitHub", analytics.MonitorName);
        Assert.Equal(0, analytics.UptimePercentage);
        Assert.Equal(0, analytics.DowntimePercentage);
        Assert.Equal(0, analytics.AverageResponseTimeMs);
        Assert.Equal(0, analytics.TotalChecks);
        Assert.Equal(0, analytics.SuccessfulChecks);
        Assert.Equal(0, analytics.FailedChecks);
    }

    [Fact]
    public async Task GetMonitorAnalytics_WithNonExistingMonitor_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/monitors/999/analytics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitorAnalytics_ExcludesNullResponseTimesFromAverage()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        int monitorId;

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var monitor = new MonitorEntity
            {
                Name = "Microsoft",
                Url = "https://www.microsoft.com",
                IsActive = true
            };

            database.Monitors.Add(monitor);
            await database.SaveChangesAsync();

            monitorId = monitor.Id;

            database.MonitorHistories.AddRange(
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = true,
                    ResponseTimeMs = 100,
                    CheckedAt = DateTime.UtcNow.AddMinutes(-2)
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = false,
                    ResponseTimeMs = null,
                    ErrorMessage = "Connection timeout",
                    CheckedAt = DateTime.UtcNow.AddMinutes(-1)
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    IsSuccessful = true,
                    ResponseTimeMs = 300,
                    CheckedAt = DateTime.UtcNow
                });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/analytics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var analytics = await response.Content
            .ReadFromJsonAsync<AnalyticsResponse>();

        Assert.NotNull(analytics);
        Assert.Equal(3, analytics.TotalChecks);
        Assert.Equal(2, analytics.SuccessfulChecks);
        Assert.Equal(1, analytics.FailedChecks);
        Assert.Equal(66.67, analytics.UptimePercentage);
        Assert.Equal(33.33, analytics.DowntimePercentage);
        Assert.Equal(200, analytics.AverageResponseTimeMs);
    }

    // Represents the JSON response returned by the analytics endpoint.
    private class AnalyticsResponse
    {
        public int MonitorId { get; set; }

        public string MonitorName { get; set; } = string.Empty;

        public double UptimePercentage { get; set; }

        public double DowntimePercentage { get; set; }

        public double AverageResponseTimeMs { get; set; }

        public int TotalChecks { get; set; }

        public int SuccessfulChecks { get; set; }

        public int FailedChecks { get; set; }
    }
}
