namespace ApiPulse.Api.DTOs;

public class MonitorPerformancePointDto
{
    public DateTime TimeBucket { get; set; }

    public double AverageResponseTimeMs { get; set; }

    public int TotalChecks { get; set; }

    public int SuccessfulChecks { get; set; }

    public int FailedChecks { get; set; }
}