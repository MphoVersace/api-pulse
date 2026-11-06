
using ApiPulse.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApiPulse.Tests;

/// <summary>
/// Creates an in-memory test version of the API Pulse application.
/// </summary>
public class ApiPulseWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    // Preserve existing environment configuration.
    private readonly Dictionary<string, string?> _originalJwtSettings =
        new()
        {
            ["Jwt__SecretKey"] =
                Environment.GetEnvironmentVariable("Jwt__SecretKey"),
            ["Jwt__Issuer"] =
                Environment.GetEnvironmentVariable("Jwt__Issuer"),
            ["Jwt__Audience"] =
                Environment.GetEnvironmentVariable("Jwt__Audience"),
            ["Jwt__ExpirationInMinutes"] =
                Environment.GetEnvironmentVariable("Jwt__ExpirationInMinutes")
        };

    public ApiPulseWebApplicationFactory()
    {
        // Configure JWT settings before the test application starts.
        Environment.SetEnvironmentVariable(
            "Jwt__SecretKey",
            "ApiPulse-Test-Only-Signing-Key-2026-Do-Not-Use-In-Production");

        Environment.SetEnvironmentVariable(
            "Jwt__Issuer",
            "ApiPulse");

        Environment.SetEnvironmentVariable(
            "Jwt__Audience",
            "ApiPulseClient");

        Environment.SetEnvironmentVariable(
            "Jwt__ExpirationInMinutes",
            "60");

        _connection.Open();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the production database with in-memory SQLite.
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddSingleton(_connection);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Disable background services during integration tests.
            services.RemoveAll<IHostedService>();
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var database = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        await database.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            base.Dispose(disposing);

            _connection.Dispose();

            // Restore the environment variables to their original values.
            foreach (var setting in _originalJwtSettings)
            {
                Environment.SetEnvironmentVariable(
                    setting.Key,
                    setting.Value);
            }
        }
        else
        {
            base.Dispose(disposing);
        }
    }
}
