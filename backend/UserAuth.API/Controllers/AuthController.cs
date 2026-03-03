using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using UserAuth.Application.DTOs;
using UserAuth.Application.Interfaces;

namespace UserAuth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Register request rejected — validation failed for {Email}", registerDto.Email);
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(registerDto);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Login an existing user</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login request rejected — validation failed for {Email}", loginDto.Email);
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(loginDto);
        return Ok(result);
    }
}
