
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiPulse.Api.Data;
using MonitorHistoryEntity = ApiPulse.Api.Models.MonitorHistory;
using MonitorEntity = ApiPulse.Api.Models.Monitor;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPulse.Tests;

public class MonitorHistoryTests
{
    // Creates a monitor and its history records for isolated tests.
    private static async Task<(int MonitorId, int OtherMonitorId)>
        SeedMonitorHistoryAsync(ApiPulseWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var monitor = new MonitorEntity
        {
            Name = "Google",
            Url = "https://www.google.com",
            Status = "Up",
            IsActive = true
        };

        var otherMonitor = new MonitorEntity
        {
            Name = "GitHub",
            Url = "https://github.com",
            Status = "Down",
            IsActive = true
        };

        database.Monitors.AddRange(monitor, otherMonitor);
        await database.SaveChangesAsync();

        var historyRecords = new List<MonitorHistoryEntity>
        {
            new()
            {
                MonitorId = monitor.Id,
                CheckedAt = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc),
                IsSuccessful = true,
                ResponseTimeMs = 120
            },
            new()
            {
                MonitorId = monitor.Id,
                CheckedAt = new DateTime(2026, 11, 1, 10, 1, 0, DateTimeKind.Utc),
                IsSuccessful = false,
                ResponseTimeMs = 500,
                ErrorMessage = "Request timed out."
            },
            new()
            {
                MonitorId = monitor.Id,
                CheckedAt = new DateTime(2026, 11, 1, 10, 2, 0, DateTimeKind.Utc),
                IsSuccessful = true,
                ResponseTimeMs = 95
            },
            new()
            {
                MonitorId = otherMonitor.Id,
                CheckedAt = new DateTime(2026, 11, 1, 10, 3, 0, DateTimeKind.Utc),
                IsSuccessful = false,
                ResponseTimeMs = null,
                ErrorMessage = "Connection refused."
            }
        };

        database.MonitorHistories.AddRange(historyRecords);
        await database.SaveChangesAsync();

        return (monitor.Id, otherMonitor.Id);
    }

    [Fact]
    public async Task GetMonitorHistory_WithExistingHistory_ReturnsHistoryRecords()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var (monitorId, _) = await SeedMonitorHistoryAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content
            .ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(history);
        Assert.Equal(3, history.Count);
    }

    [Fact]
    public async Task GetMonitorHistory_WithNoHistory_ReturnsEmptyArray()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var scope = factory.Services.CreateScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var monitor = new MonitorEntity
        {
            Name = "Google",
            Url = "https://www.google.com",
            Status = "Unknown",
            IsActive = true
        };

        database.Monitors.Add(monitor);
        await database.SaveChangesAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitor.Id}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content
            .ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(history);
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetMonitorHistory_WithNonExistingMonitor_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/monitors/9999/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitorHistory_ReturnsNewestRecordsFirst()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var (monitorId, _) = await SeedMonitorHistoryAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/history");

        var history = await response.Content
            .ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(history);
        Assert.Equal(3, history.Count);

        var timestamps = history
            .Select(record => record.GetProperty("checkedAt").GetDateTime())
            .ToList();

        Assert.Equal(
            timestamps.OrderByDescending(date => date),
            timestamps);
    }

    [Fact]
    public async Task GetMonitorHistory_ExcludesOtherMonitorsHistory()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var (monitorId, otherMonitorId) =
            await SeedMonitorHistoryAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/history");

        var history = await response.Content
            .ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(history);
        Assert.Equal(3, history.Count);

        Assert.All(history, record =>
            Assert.Equal(
                monitorId,
                record.GetProperty("monitorId").GetInt32()));

        Assert.DoesNotContain(history, record =>
            record.GetProperty("monitorId").GetInt32() == otherMonitorId);
    }

    [Fact]
    public async Task GetMonitorHistory_ReturnsAllHistoryFields()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        var (monitorId, _) = await SeedMonitorHistoryAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/monitors/{monitorId}/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content
            .ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(history);

        var failedRecord = Assert.Single(
            history,
            record => !record.GetProperty("isSuccessful").GetBoolean());

        Assert.True(failedRecord.GetProperty("id").GetInt32() > 0);

        Assert.Equal(
            monitorId,
            failedRecord.GetProperty("monitorId").GetInt32());

        Assert.Equal(
            500,
            failedRecord.GetProperty("responseTimeMs").GetInt32());

        Assert.Equal(
            "Request timed out.",
            failedRecord.GetProperty("errorMessage").GetString());

        Assert.True(
            failedRecord.GetProperty("checkedAt").GetDateTime() != default);
    }
}
