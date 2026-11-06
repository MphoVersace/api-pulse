
using ApiPulse.Api.DTOs;
using ApiPulse.Api.Models;
using ApiPulse.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApiPulse.Api.Controllers;

/// <summary>
/// Handles user registration and authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Registers a new API Pulse user account.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);

        if (existingUser is not null)
        {
            return Conflict(new
            {
                message = "An account with this email address already exists."
            });
        }

        var user = new ApplicationUser
        {
            FullName = request.FullName.Trim(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Registration failed.",
                errors = result.Errors.Select(error => error.Description)
            });
        }

        var tokenResponse = _jwtTokenService.GenerateToken(user);

        return StatusCode(StatusCodes.Status201Created, tokenResponse);
    }

    /// <summary>
    /// Authenticates an existing user and returns a JWT access token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email address or password."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Unauthorized(new
            {
                message = "Your account is temporarily locked. Please try again later."
            });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email address or password."
            });
        }

        var tokenResponse = _jwtTokenService.GenerateToken(user);

        return Ok(tokenResponse);
    }
}
