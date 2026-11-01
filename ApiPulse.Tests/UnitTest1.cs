
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiPulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPulse.Tests;

public class MonitorsControllerTests
{
    [Fact]
    public async Task GetMonitors_ReturnsOkAndJsonArray()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var jsonDocument = JsonDocument.Parse(responseBody);

        Assert.Equal(JsonValueKind.Array, jsonDocument.RootElement.ValueKind);
    }

    [Fact]
    public async Task CreateMonitor_WithValidData_ReturnsCreatedAndSavesMonitor()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var request = new
        {
            Name = "Google",
            Url = "https://www.google.com"
        };

        var response = await client.PostAsJsonAsync("/api/monitors", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdMonitor = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Google", createdMonitor.GetProperty("name").GetString());
        Assert.Equal("https://www.google.com", createdMonitor.GetProperty("url").GetString());
        Assert.True(createdMonitor.GetProperty("isActive").GetBoolean());
        Assert.Equal("Unknown", createdMonitor.GetProperty("status").GetString());

        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        Assert.True(monitorId > 0);

        using var scope = factory.Services.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedMonitor = await database.Monitors
            .AsNoTracking()
            .FirstOrDefaultAsync(monitor => monitor.Id == monitorId);

        Assert.NotNull(savedMonitor);
        Assert.Equal("Google", savedMonitor.Name);
        Assert.Equal("https://www.google.com", savedMonitor.Url);
    }

    [Fact]
    public async Task CreateMonitor_WithInvalidUrl_ReturnsBadRequest()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var request = new
        {
            Name = "Invalid Website",
            Url = "not-a-valid-url"
        };

        var response = await client.PostAsJsonAsync("/api/monitors", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMonitor_WithExistingId_ReturnsCorrectMonitor()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/monitors",
            new { Name = "GitHub", Url = "https://github.com" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdMonitor = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/monitors/{monitorId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var retrievedMonitor = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(monitorId, retrievedMonitor.GetProperty("id").GetInt32());
        Assert.Equal("GitHub", retrievedMonitor.GetProperty("name").GetString());
        Assert.Equal("https://github.com", retrievedMonitor.GetProperty("url").GetString());
        Assert.True(retrievedMonitor.GetProperty("isActive").GetBoolean());
        Assert.Equal("Unknown", retrievedMonitor.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetMonitor_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/monitors/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMonitor_WithValidData_ReturnsUpdatedMonitor()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        // Register a monitor before updating it.
        var createResponse = await client.PostAsJsonAsync(
            "/api/monitors",
            new { Name = "Old Name", Url = "https://old.example.com" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdMonitor = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var monitorId = createdMonitor.GetProperty("id").GetInt32();

        // Update the monitor's name and URL.
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/monitors/{monitorId}",
            new
            {
                Name = "Updated Website",
                Url = "https://updated.example.com"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedMonitor = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(monitorId, updatedMonitor.GetProperty("id").GetInt32());
        Assert.Equal("Updated Website", updatedMonitor.GetProperty("name").GetString());
        Assert.Equal(
            "https://updated.example.com",
            updatedMonitor.GetProperty("url").GetString());

        // Verify that the updated values were persisted.
        using var scope = factory.Services.CreateScope();

        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var savedMonitor = await database.Monitors
            .AsNoTracking()
            .FirstOrDefaultAsync(monitor => monitor.Id == monitorId);

        Assert.NotNull(savedMonitor);
        Assert.Equal("Updated Website", savedMonitor.Name);
        Assert.Equal("https://updated.example.com", savedMonitor.Url);
    }

    [Fact]
    public async Task UpdateMonitor_WithNonExistingId_ReturnsNotFound()
    {
        using var factory = new ApiPulseWebApplicationFactory();
        await factory.InitializeDatabaseAsync();

        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            "/api/monitors/999",
            new
            {
                Name = "Updated Website",
                Url = "https://updated.example.com"
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
