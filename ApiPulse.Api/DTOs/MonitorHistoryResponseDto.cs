namespace ApiPulse.Api.DTOs;

public class MonitorHistoryResponseDto
{
    public int Id { get; set; }

    public int MonitorId { get; set; }

    public DateTime CheckedAt { get; set; }

    public bool IsSuccessful { get; set; }

    public int? ResponseTimeMs { get; set; }

    public string? ErrorMessage { get; set; }
}
