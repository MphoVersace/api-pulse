namespace ApiPulse.Api.DTOs;

public class DashboardResponseDto
{
    public int TotalMonitors { get; set; }

    public int UpMonitors { get; set; }

    public int DownMonitors { get; set; }

    public int UnknownMonitors { get; set; }

    public int InactiveMonitors { get; set; }

    public double AverageResponseTimeMs { get; set; }

    public double UptimePercentage { get; set; }

    public int TotalChecks { get; set; }

    public int SuccessfulChecks { get; set; }

    public int FailedChecks { get; set; }
}