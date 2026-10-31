
using ApiPulse.Api.Data;
using ApiPulse.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiPulse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonitorsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MonitorsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/monitors
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonitorResponseDto>>> GetMonitors()
    {
        var monitors = await _context.Monitors
            .AsNoTracking()
            .Select(monitor => new MonitorResponseDto
            {
                Id = monitor.Id,
                Name = monitor.Name,
                Url = monitor.Url,
                IsActive = monitor.IsActive,
                CreatedAt = monitor.CreatedAt,
                LastCheckedAt = monitor.LastCheckedAt,
                Status = monitor.Status
            })
            .ToListAsync();

        return Ok(monitors);
    }

    // GET: api/monitors/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<MonitorResponseDto>> GetMonitor(int id)
    {
        var monitor = await _context.Monitors
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MonitorResponseDto
            {
                Id = m.Id,
                Name = m.Name,
                Url = m.Url,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt,
                LastCheckedAt = m.LastCheckedAt,
                Status = m.Status
            })
            .FirstOrDefaultAsync();

        if (monitor == null)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        return Ok(monitor);
    }

    // GET: api/monitors/{id}/history
    [HttpGet("{id}/history")]
    public async Task<ActionResult<IEnumerable<MonitorHistoryResponseDto>>>
        GetMonitorHistory(int id)
    {
        var monitorExists = await _context.Monitors
            .AnyAsync(m => m.Id == id);

        if (!monitorExists)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        var history = await _context.MonitorHistories
            .AsNoTracking()
            .Where(h => h.MonitorId == id)
            .OrderByDescending(h => h.CheckedAt)
            .Select(h => new MonitorHistoryResponseDto
            {
                Id = h.Id,
                MonitorId = h.MonitorId,
                CheckedAt = h.CheckedAt,
                IsSuccessful = h.IsSuccessful,
                ResponseTimeMs = h.ResponseTimeMs,
                ErrorMessage = h.ErrorMessage
            })
            .ToListAsync();

        return Ok(history);
    }

    // GET: api/monitors/{id}/analytics
    [HttpGet("{id}/analytics")]
    public async Task<ActionResult<MonitorAnalyticsResponseDto>>
        GetMonitorAnalytics(int id)
    {
        var monitor = await _context.Monitors
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (monitor == null)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        var history = await _context.MonitorHistories
            .AsNoTracking()
            .Where(h => h.MonitorId == id)
            .Select(h => new
            {
                h.IsSuccessful,
                h.ResponseTimeMs
            })
            .ToListAsync();

        var totalChecks = history.Count;
        var successfulChecks = history.Count(h => h.IsSuccessful);
        var failedChecks = totalChecks - successfulChecks;

        var uptimePercentage = totalChecks == 0
            ? 0
            : (double)successfulChecks / totalChecks * 100;

        var downtimePercentage = totalChecks == 0
            ? 0
            : (double)failedChecks / totalChecks * 100;

        var responseTimes = history
            .Where(h => h.ResponseTimeMs.HasValue)
            .Select(h => h.ResponseTimeMs!.Value)
            .ToList();

        var averageResponseTime = responseTimes.Count == 0
            ? 0
            : responseTimes.Average();

        var analytics = new MonitorAnalyticsResponseDto
        {
            MonitorId = monitor.Id,
            MonitorName = monitor.Name,
            UptimePercentage = Math.Round(uptimePercentage, 2),
            DowntimePercentage = Math.Round(downtimePercentage, 2),
            AverageResponseTimeMs = Math.Round(averageResponseTime, 2),
            TotalChecks = totalChecks,
            SuccessfulChecks = successfulChecks,
            FailedChecks = failedChecks
        };

        return Ok(analytics);
    }

    // POST: api/monitors
    [HttpPost]
    public async Task<ActionResult<MonitorResponseDto>> CreateMonitor(
        CreateMonitorDto request)
    {
        var monitor = new ApiPulse.Api.Models.Monitor
        {
            Name = request.Name.Trim(),
            Url = request.Url.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Status = "Unknown"
        };

        _context.Monitors.Add(monitor);
        await _context.SaveChangesAsync();

        var response = new MonitorResponseDto
        {
            Id = monitor.Id,
            Name = monitor.Name,
            Url = monitor.Url,
            IsActive = monitor.IsActive,
            CreatedAt = monitor.CreatedAt,
            LastCheckedAt = monitor.LastCheckedAt,
            Status = monitor.Status
        };

        return CreatedAtAction(
            nameof(GetMonitor),
            new { id = monitor.Id },
            response);
    }

    // DELETE: api/monitors/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMonitor(int id)
    {
        var monitor = await _context.Monitors.FindAsync(id);

        if (monitor == null)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        _context.Monitors.Remove(monitor);

        await _context.SaveChangesAsync();

        return NoContent();
    }
}
