
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiPulse.Api.Data;
using MonitorEntity = ApiPulse.Api.Models.Monitor;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPulse.Tests;

public class MonitorFilteringTests
{
    // Creates a consistent set of monitors for filtering tests.
    private static async Task SeedMonitorsAsync(
        ApiPulseWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();

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
                IsActive = false
            },
            new()
            {
                Name = "OpenAI",
                Url = "https://openai.com",
                Status = "Up",
                IsActive = false
            }
        };

        database.Monitors.AddRange(monitors);

        await database.SaveChangesAsync();
    }

    [Fact]
    public async Task GetMonitors_WithUpStatus_ReturnsOnlyUpMonitors()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors?status=up");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var monitors =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(monitors);
        Assert.Equal(2, monitors.Count);

        Assert.All(monitors, monitor =>
            Assert.Equal("Up", monitor.GetProperty("status").GetString()));
    }

    [Fact]
    public async Task GetMonitors_WithDownStatus_ReturnsOnlyDownMonitors()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors?status=Down");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var monitors =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(monitors);
        Assert.Single(monitors);

        Assert.Equal("Down", monitors[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetMonitors_WithUnknownStatus_ReturnsOnlyUnknownMonitors()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors?status=Unknown");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var monitors =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(monitors);
        Assert.Single(monitors);

        Assert.Equal(
            "Unknown",
            monitors[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetMonitors_WithIsActiveTrue_ReturnsOnlyActiveMonitors()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors?isActive=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var monitors =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(monitors);
        Assert.Equal(2, monitors.Count);

        Assert.All(monitors, monitor =>
            Assert.True(monitor.GetProperty("isActive").GetBoolean()));
    }

    [Fact]
    public async Task GetMonitors_WithIsActiveFalse_ReturnsOnlyInactiveMonitors()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors?isActive=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var monitors =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(monitors);
        Assert.Equal(2, monitors.Count);

        Assert.All(monitors, monitor =>
            Assert.False(monitor.GetProperty("isActive").GetBoolean()));
    }

    [Fact]
    public async Task GetMonitors_WithCombinedFilters_ReturnsMatchingMonitors()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/monitors?status=Up&isActive=false");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var monitors =
            await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.NotNull(monitors);
        Assert.Single(monitors);

        Assert.Equal("OpenAI", monitors[0].GetProperty("name").GetString());
        Assert.Equal("Up", monitors[0].GetProperty("status").GetString());

        Assert.False(monitors[0].GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task GetMonitors_WithInvalidStatus_ReturnsBadRequest()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();
        await SeedMonitorsAsync(factory);

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/monitors?status=Maintenance");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
