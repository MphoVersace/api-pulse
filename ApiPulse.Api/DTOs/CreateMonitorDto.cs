using System.ComponentModel.DataAnnotations;

namespace ApiPulse.Api.DTOs;

public class CreateMonitorDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;
}