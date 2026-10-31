
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

    // GET: api/monitors/{id}/performance
    [HttpGet("{id}/performance")]
    public async Task<ActionResult<IEnumerable<MonitorPerformancePointDto>>>
        GetMonitorPerformance(
            int id,
            [FromQuery] DateOnly? startDate,
            [FromQuery] DateOnly? endDate)
    {
        if (startDate == null || endDate == null)
        {
            return BadRequest(new
            {
                message = "Both startDate and endDate are required."
            });
        }

        if (endDate < startDate)
        {
            return BadRequest(new
            {
                message = "endDate cannot be earlier than startDate."
            });
        }

        var monitorExists = await _context.Monitors
            .AnyAsync(m => m.Id == id);

        if (!monitorExists)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        var startDateTime = DateTime.SpecifyKind(
            startDate.Value.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var endDateTime = DateTime.SpecifyKind(
            endDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var useHourlyGrouping =
            (endDateTime - startDateTime).TotalHours <= 48;

        var history = await _context.MonitorHistories
            .AsNoTracking()
            .Where(h =>
                h.MonitorId == id &&
                h.CheckedAt >= startDateTime &&
                h.CheckedAt < endDateTime)
            .Select(h => new
            {
                h.CheckedAt,
                h.IsSuccessful,
                h.ResponseTimeMs
            })
            .ToListAsync();

        var performance = history
            .GroupBy(h => useHourlyGrouping
                ? new DateTime(
                    h.CheckedAt.Year,
                    h.CheckedAt.Month,
                    h.CheckedAt.Day,
                    h.CheckedAt.Hour,
                    0,
                    0,
                    DateTimeKind.Utc)
                : new DateTime(
                    h.CheckedAt.Year,
                    h.CheckedAt.Month,
                    h.CheckedAt.Day,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var responseTimes = group
                    .Where(h => h.ResponseTimeMs.HasValue)
                    .Select(h => h.ResponseTimeMs!.Value)
                    .ToList();

                return new MonitorPerformancePointDto
                {
                    TimeBucket = group.Key,
                    AverageResponseTimeMs = responseTimes.Count == 0
                        ? 0
                        : Math.Round(responseTimes.Average(), 2),
                    TotalChecks = group.Count(),
                    SuccessfulChecks = group.Count(h => h.IsSuccessful),
                    FailedChecks = group.Count(h => !h.IsSuccessful)
                };
            })
            .ToList();

        return Ok(performance);
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

    // PUT: api/monitors/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<MonitorResponseDto>> UpdateMonitor(
        int id,
        UpdateMonitorDto request)
    {
        var monitor = await _context.Monitors.FindAsync(id);

        if (monitor == null)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        // Update only the monitor's name and URL.
        // Preserve its status, activation state, and monitoring history.
        monitor.Name = request.Name.Trim();
        monitor.Url = request.Url.Trim();

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

        return Ok(response);
    }

    // PATCH: api/monitors/{id}/deactivate
    [HttpPatch("{id}/deactivate")]
    public async Task<ActionResult<MonitorResponseDto>> DeactivateMonitor(int id)
    {
        var monitor = await _context.Monitors.FindAsync(id);

        if (monitor == null)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        if (!monitor.IsActive)
        {
            return BadRequest(new { message = "Monitor is already deactivated." });
        }

        monitor.IsActive = false;

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

        return Ok(response);
    }

    // PATCH: api/monitors/{id}/activate
    [HttpPatch("{id}/activate")]
    public async Task<ActionResult<MonitorResponseDto>> ActivateMonitor(int id)
    {
        var monitor = await _context.Monitors.FindAsync(id);

        if (monitor == null)
        {
            return NotFound(new { message = "Monitor not found." });
        }

        if (monitor.IsActive)
        {
            return BadRequest(new { message = "Monitor is already active." });
        }

        monitor.IsActive = true;

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

        return Ok(response);
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
