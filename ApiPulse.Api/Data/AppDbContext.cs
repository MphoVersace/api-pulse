
using ApiPulse.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using MonitorEntity = ApiPulse.Api.Models.Monitor;

namespace ApiPulse.Api.Data;

/// <summary>
/// Database context for API Pulse.
/// Manages Identity users alongside monitoring data.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Stores website monitoring configurations.
    public DbSet<MonitorEntity> Monitors => Set<MonitorEntity>();

    // Stores historical website monitoring results.
    public DbSet<MonitorHistory> MonitorHistories => Set<MonitorHistory>();
}
