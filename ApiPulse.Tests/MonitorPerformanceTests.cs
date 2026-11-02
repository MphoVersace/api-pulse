
using System.Net;
using System.Net.Http.Json;
using ApiPulse.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using MonitorHistoryEntity = ApiPulse.Api.Models.MonitorHistory;
using MonitorEntity = ApiPulse.Api.Models.Monitor;

namespace ApiPulse.Tests;

public class MonitorPerformanceTests
{
    [Fact]
    public async Task GetMonitorPerformance_WithHistory_ReturnsHourlyPerformance()
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

            // Seed checks across two hours to verify hourly grouping.
            database.MonitorHistories.AddRange(
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    CheckedAt = new DateTime(2026, 10, 31, 10, 15, 0, DateTimeKind.Utc),
                    IsSuccessful = true,
                    ResponseTimeMs = 100
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    CheckedAt = new DateTime(2026, 10, 31, 10, 45, 0, DateTimeKind.Utc),
                    IsSuccessful = false,
                    ResponseTimeMs = 300,
                    ErrorMessage = "Server error"
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    CheckedAt = new DateTime(2026, 10, 31, 11, 15, 0, DateTimeKind.Utc),
                    IsSuccessful = true,
                    ResponseTimeMs = 200
                });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/performance?startDate=2026-10-31&endDate=2026-10-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var performance = await response.Content
            .ReadFromJsonAsync<List<PerformanceResponse>>();

        Assert.NotNull(performance);
        Assert.Equal(2, performance.Count);

        Assert.Equal(
            new DateTime(2026, 10, 31, 10, 0, 0, DateTimeKind.Utc),
            performance[0].TimeBucket);

        Assert.Equal(200, performance[0].AverageResponseTimeMs);
        Assert.Equal(2, performance[0].TotalChecks);
        Assert.Equal(1, performance[0].SuccessfulChecks);
        Assert.Equal(1, performance[0].FailedChecks);

        Assert.Equal(
            new DateTime(2026, 10, 31, 11, 0, 0, DateTimeKind.Utc),
            performance[1].TimeBucket);

        Assert.Equal(200, performance[1].AverageResponseTimeMs);
        Assert.Equal(1, performance[1].TotalChecks);
        Assert.Equal(1, performance[1].SuccessfulChecks);
        Assert.Equal(0, performance[1].FailedChecks);
    }

    [Fact]
    public async Task GetMonitorPerformance_WithNoHistory_ReturnsEmptyArray()
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
            $"/api/monitors/{monitorId}/performance?startDate=2026-10-31&endDate=2026-10-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var performance = await response.Content
            .ReadFromJsonAsync<List<PerformanceResponse>>();

        Assert.NotNull(performance);
        Assert.Empty(performance);
    }

    [Fact]
    public async Task GetMonitorPerformance_WithNonExistingMonitor_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/monitors/999/performance?startDate=2026-10-31&endDate=2026-10-31");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitorPerformance_WithoutStartDate_ReturnsBadRequest()
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
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/performance?endDate=2026-10-31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitorPerformance_WithoutEndDate_ReturnsBadRequest()
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
                Name = "OpenAI",
                Url = "https://openai.com",
                IsActive = true
            };

            database.Monitors.Add(monitor);
            await database.SaveChangesAsync();

            monitorId = monitor.Id;
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/performance?startDate=2026-10-31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitorPerformance_WithEndDateBeforeStartDate_ReturnsBadRequest()
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
                Name = "Amazon",
                Url = "https://www.amazon.com",
                IsActive = true
            };

            database.Monitors.Add(monitor);
            await database.SaveChangesAsync();

            monitorId = monitor.Id;
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/performance?startDate=2026-10-31&endDate=2026-10-30");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitorPerformance_WithLongDateRange_ReturnsDailyPerformance()
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

            // Seed checks on two different days to verify daily grouping.
            database.MonitorHistories.AddRange(
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    CheckedAt = new DateTime(2026, 10, 1, 10, 30, 0, DateTimeKind.Utc),
                    IsSuccessful = true,
                    ResponseTimeMs = 100
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitorId,
                    CheckedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc),
                    IsSuccessful = false,
                    ResponseTimeMs = 300,
                    ErrorMessage = "Server error"
                });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/performance?startDate=2026-10-01&endDate=2026-10-03");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var performance = await response.Content
            .ReadFromJsonAsync<List<PerformanceResponse>>();

        Assert.NotNull(performance);
        Assert.Equal(2, performance.Count);

        Assert.Equal(
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            performance[0].TimeBucket);

        Assert.Equal(100, performance[0].AverageResponseTimeMs);
        Assert.Equal(1, performance[0].TotalChecks);
        Assert.Equal(1, performance[0].SuccessfulChecks);
        Assert.Equal(0, performance[0].FailedChecks);

        Assert.Equal(
            new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            performance[1].TimeBucket);

        Assert.Equal(300, performance[1].AverageResponseTimeMs);
        Assert.Equal(1, performance[1].TotalChecks);
        Assert.Equal(0, performance[1].SuccessfulChecks);
        Assert.Equal(1, performance[1].FailedChecks);
    }

    // Represents the JSON response returned by the performance endpoint.
    private class PerformanceResponse
    {
        public DateTime TimeBucket { get; set; }

        public double AverageResponseTimeMs { get; set; }

        public int TotalChecks { get; set; }

        public int SuccessfulChecks { get; set; }

        public int FailedChecks { get; set; }
    }
}
