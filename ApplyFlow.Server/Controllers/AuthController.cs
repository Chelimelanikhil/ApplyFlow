
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

}
