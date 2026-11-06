
using System.ComponentModel.DataAnnotations;

namespace ApiPulse.Api.DTOs;

/// <summary>
/// Represents the information required to register a new API Pulse account.
/// </summary>
public class RegisterDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}
