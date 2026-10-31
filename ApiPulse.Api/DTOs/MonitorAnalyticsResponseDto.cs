namespace ApiPulse.Api.DTOs;

public class MonitorAnalyticsResponseDto
{
    public int MonitorId { get; set; }

    public string MonitorName { get; set; } = string.Empty;

    public double UptimePercentage { get; set; }

    public double DowntimePercentage { get; set; }

    public double AverageResponseTimeMs { get; set; }

    public int TotalChecks { get; set; }

    public int SuccessfulChecks { get; set; }

    public int FailedChecks { get; set; }
}