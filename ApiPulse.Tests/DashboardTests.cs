
using System.Net;
using System.Net.Http.Json;
using ApiPulse.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using MonitorHistoryEntity = ApiPulse.Api.Models.MonitorHistory;
using MonitorEntity = ApiPulse.Api.Models.Monitor;

namespace ApiPulse.Tests;

public class DashboardTests
{
    [Fact]
    public async Task GetDashboard_WithMonitoringData_ReturnsCorrectMetrics()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var monitors = new List<MonitorEntity>
            {
                new()
                {
                    Name = "Google",
                    Url = "https://www.google.com",
                    Status = "Up",
                    IsActive = true
                },
                new()
                {
                    Name = "GitHub",
                    Url = "https://github.com",
                    Status = "Down",
                    IsActive = true
                },
                new()
                {
                    Name = "Microsoft",
                    Url = "https://www.microsoft.com",
                    Status = "Unknown",
                    IsActive = true
                },
                new()
                {
                    Name = "Amazon",
                    Url = "https://www.amazon.com",
                    Status = "Up",
                    IsActive = false
                }
            };

            database.Monitors.AddRange(monitors);
            await database.SaveChangesAsync();

            // Seed successful and failed checks with different response times.
            database.MonitorHistories.AddRange(
                new MonitorHistoryEntity
                {
                    MonitorId = monitors[0].Id,
                    CheckedAt = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                    IsSuccessful = true,
                    ResponseTimeMs = 100
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitors[0].Id,
                    CheckedAt = new DateTime(2026, 11, 1, 10, 1, 0, DateTimeKind.Utc),
                    IsSuccessful = true,
                    ResponseTimeMs = 200
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitors[1].Id,
                    CheckedAt = new DateTime(2026, 11, 1, 10, 2, 0, DateTimeKind.Utc),
                    IsSuccessful = false,
                    ResponseTimeMs = 300,
                    ErrorMessage = "Server error"
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitors[2].Id,
                    CheckedAt = new DateTime(2026, 11, 1, 10, 3, 0, DateTimeKind.Utc),
                    IsSuccessful = true,
                    ResponseTimeMs = 400
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitors[3].Id,
                    CheckedAt = new DateTime(2026, 11, 1, 10, 4, 0, DateTimeKind.Utc),
                    IsSuccessful = false,
                    ResponseTimeMs = null,
                    ErrorMessage = "Connection failed"
                });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content
            .ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(dashboard);

        Assert.Equal(4, dashboard.TotalMonitors);
        Assert.Equal(2, dashboard.UpMonitors);
        Assert.Equal(1, dashboard.DownMonitors);
        Assert.Equal(1, dashboard.UnknownMonitors);
        Assert.Equal(1, dashboard.InactiveMonitors);

        Assert.Equal(5, dashboard.TotalChecks);
        Assert.Equal(3, dashboard.SuccessfulChecks);
        Assert.Equal(2, dashboard.FailedChecks);

        Assert.Equal(250, dashboard.AverageResponseTimeMs);
        Assert.Equal(60, dashboard.UptimePercentage);
    }

    [Fact]
    public async Task GetDashboard_WithNoData_ReturnsZeroValues()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content
            .ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(dashboard);

        Assert.Equal(0, dashboard.TotalMonitors);
        Assert.Equal(0, dashboard.UpMonitors);
        Assert.Equal(0, dashboard.DownMonitors);
        Assert.Equal(0, dashboard.UnknownMonitors);
        Assert.Equal(0, dashboard.InactiveMonitors);

        Assert.Equal(0, dashboard.TotalChecks);
        Assert.Equal(0, dashboard.SuccessfulChecks);
        Assert.Equal(0, dashboard.FailedChecks);

        Assert.Equal(0, dashboard.AverageResponseTimeMs);
        Assert.Equal(0, dashboard.UptimePercentage);
    }

    [Fact]
    public async Task GetDashboard_ExcludesNullResponseTimesFromAverage()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var monitor = new MonitorEntity
            {
                Name = "Google",
                Url = "https://www.google.com",
                Status = "Up",
                IsActive = true
            };

            database.Monitors.Add(monitor);
            await database.SaveChangesAsync();

            database.MonitorHistories.AddRange(
                new MonitorHistoryEntity
                {
                    MonitorId = monitor.Id,
                    IsSuccessful = true,
                    ResponseTimeMs = 100
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitor.Id,
                    IsSuccessful = true,
                    ResponseTimeMs = null
                },
                new MonitorHistoryEntity
                {
                    MonitorId = monitor.Id,
                    IsSuccessful = false,
                    ResponseTimeMs = 300
                });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content
            .ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(dashboard);

        // Only the two non-null response times contribute to the average.
        Assert.Equal(200, dashboard.AverageResponseTimeMs);

        // All three checks still contribute to the uptime calculation.
        Assert.Equal(66.67, dashboard.UptimePercentage);
        Assert.Equal(3, dashboard.TotalChecks);
    }

    [Fact]
    public async Task GetDashboard_WithNoChecks_ReturnsZeroPerformanceMetrics()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            database.Monitors.Add(new MonitorEntity
            {
                Name = "GitHub",
                Url = "https://github.com",
                Status = "Up",
                IsActive = true
            });

            await database.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dashboard = await response.Content
            .ReadFromJsonAsync<DashboardResponse>();

        Assert.NotNull(dashboard);

        Assert.Equal(1, dashboard.TotalMonitors);
        Assert.Equal(1, dashboard.UpMonitors);

        Assert.Equal(0, dashboard.TotalChecks);
        Assert.Equal(0, dashboard.SuccessfulChecks);
        Assert.Equal(0, dashboard.FailedChecks);
        Assert.Equal(0, dashboard.AverageResponseTimeMs);
        Assert.Equal(0, dashboard.UptimePercentage);
    }

    // Represents the JSON response returned by the dashboard endpoint.
    private class DashboardResponse
    {
        public int TotalMonitors { get; set; }

        public int UpMonitors { get; set; }

        public int DownMonitors { get; set; }

        public int UnknownMonitors { get; set; }

        public int InactiveMonitors { get; set; }

        public double AverageResponseTimeMs { get; set; }

        public double UptimePercentage { get; set; }

        public int TotalChecks { get; set; }

        public int SuccessfulChecks { get; set; }

        public int FailedChecks { get; set; }
    }
}
