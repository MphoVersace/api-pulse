using ApiPulse.Api.Data;
using ApiPulse.Api.DTOs;
using ApiPulse.Api.Models;
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

    // POST: api/monitors
    [HttpPost]
    public async Task<ActionResult<MonitorResponseDto>> CreateMonitor(
        CreateMonitorDto request)
    {
        var monitor = new Monitor
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
}