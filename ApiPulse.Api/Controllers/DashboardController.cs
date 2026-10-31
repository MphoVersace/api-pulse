
using ApiPulse.Api.Data;
using ApiPulse.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/dashboard
    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboard()
    {
        var monitors = await _context.Monitors
            .AsNoTracking()
            .Select(m => new
            {
                m.Status,
                m.IsActive
            })
            .ToListAsync();

        var totalMonitors = monitors.Count;
        var upMonitors = monitors.Count(m => m.Status == "Up");
        var downMonitors = monitors.Count(m => m.Status == "Down");
        var unknownMonitors = monitors.Count(m => m.Status == "Unknown");
        var inactiveMonitors = monitors.Count(m => !m.IsActive);

        var totalChecks = await _context.MonitorHistories
            .CountAsync();

        var successfulChecks = await _context.MonitorHistories
            .CountAsync(h => h.IsSuccessful);

        var failedChecks = totalChecks - successfulChecks;

        var averageResponseTime = await _context.MonitorHistories
            .Where(h => h.ResponseTimeMs.HasValue)
            .AverageAsync(h => (double?)h.ResponseTimeMs) ?? 0;

        var uptimePercentage = totalChecks == 0
            ? 0
            : (double)successfulChecks / totalChecks * 100;

        var dashboard = new DashboardResponseDto
        {
            TotalMonitors = totalMonitors,
            UpMonitors = upMonitors,
            DownMonitors = downMonitors,
            UnknownMonitors = unknownMonitors,
            InactiveMonitors = inactiveMonitors,
            AverageResponseTimeMs = Math.Round(averageResponseTime, 2),
            UptimePercentage = Math.Round(uptimePercentage, 2),
            TotalChecks = totalChecks,
            SuccessfulChecks = successfulChecks,
            FailedChecks = failedChecks
        };

        return Ok(dashboard);
    }
}
