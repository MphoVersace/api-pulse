namespace ApiPulse.Api.DTOs;

public class MonitorResponseDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    public string Status { get; set; } = string.Empty;
}
