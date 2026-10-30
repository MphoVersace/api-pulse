using ApiPulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiPulse.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApiPulse.Api.Models.Monitor> Monitors => Set<ApiPulse.Api.Models.Monitor>();

    public DbSet<MonitorHistory> MonitorHistories => Set<MonitorHistory>();
}