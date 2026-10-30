namespace ApiPulse.Api.Models;

public class MonitorHistory
{
    public int Id { get; set; }

    public int MonitorId { get; set; }

    public Monitor Monitor { get; set; } = null!;

    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

    public bool IsSuccessful { get; set; }

    public int? ResponseTimeMs { get; set; }

    public string? ErrorMessage { get; set; }
}