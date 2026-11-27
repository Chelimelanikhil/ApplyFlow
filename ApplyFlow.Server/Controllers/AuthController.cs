
using ApplyFlow.Server.DTOs;
using ApplyFlow.Server.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApplyFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var userId = await _authService.RegisterAsync(request);

        if (userId is null)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            userId,
            message = "Account created successfully."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshAsync(
            request.RefreshToken);

        if (result is null)
        {
            return Unauthorized(new
            {
                message = "Invalid or expired refresh token."
            });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request)
    {
        await _authService.LogoutAsync(request.RefreshToken);

        return Ok(new
        {
            message = "Logged out successfully."
        });
    }


    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new
            {
                message = "Verification token is required."
            });
        }

        var verified = await _authService.VerifyEmailAsync(token);

        if (!verified)
        {
            return BadRequest(new
            {
                message = "Invalid, expired, or already used verification token."
            });
        }

        return Ok(new
        {
            message = "Email verified successfully."
        });
    }

}
