
namespace ApiPulse.Api.Models;

public class Monitor
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastCheckedAt { get; set; }

    public string Status { get; set; } = "Unknown";

    public ICollection<MonitorHistory> History { get; set; }
        = new List<MonitorHistory>();
}
