
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ApiPulse.Tests;

public class MonitorActivationTests
{
    [Fact]
    public async Task DeactivateMonitor_WhenActive_ReturnsOkAndSetsInactive()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        // Register a monitor. New monitors are active by default.
        var createResponse = await client.PostAsJsonAsync(
            "/api/monitors",
            new { Name = "Google", Url = "https://www.google.com" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdMonitor =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        // Deactivate the newly registered monitor.
        var response = await client.PatchAsync(
            $"/api/monitors/{monitorId}/deactivate",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedMonitor =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(updatedMonitor.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task ActivateMonitor_WhenInactive_ReturnsOkAndSetsActive()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        // Register a monitor.
        var createResponse = await client.PostAsJsonAsync(
            "/api/monitors",
            new { Name = "GitHub", Url = "https://github.com" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdMonitor =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        // Deactivate it first so that it can be activated.
        var deactivateResponse = await client.PatchAsync(
            $"/api/monitors/{monitorId}/deactivate",
            content: null);

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        // Activate the inactive monitor.
        var response = await client.PatchAsync(
            $"/api/monitors/{monitorId}/activate",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedMonitor =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(updatedMonitor.GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task DeactivateMonitor_WhenAlreadyInactive_ReturnsBadRequest()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monitors",
            new { Name = "Google", Url = "https://www.google.com" });

        var createdMonitor =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        // Deactivate the monitor once.
        await client.PatchAsync(
            $"/api/monitors/{monitorId}/deactivate",
            content: null);

        // Attempt to deactivate it again.
        var response = await client.PatchAsync(
            $"/api/monitors/{monitorId}/deactivate",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateMonitor_WhenAlreadyActive_ReturnsBadRequest()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monitors",
            new { Name = "GitHub", Url = "https://github.com" });

        var createdMonitor =
            await createResponse.Content.ReadFromJsonAsync<JsonElement>();

        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        // A newly registered monitor is already active.
        var response = await client.PatchAsync(
            $"/api/monitors/{monitorId}/activate",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ActivateMonitor_WhenMonitorDoesNotExist_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PatchAsync(
            "/api/monitors/999/activate",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateMonitor_WhenMonitorDoesNotExist_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PatchAsync(
            "/api/monitors/999/deactivate",
            content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
