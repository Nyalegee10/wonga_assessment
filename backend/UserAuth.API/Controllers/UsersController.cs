using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using UserAuth.Application.DTOs;
using UserAuth.Application.Interfaces;

namespace UserAuth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>Get the currently authenticated user's details</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Profile request rejected — token missing or invalid sub claim");
            return Unauthorized(new { message = "Invalid token." });
        }

        _logger.LogInformation("Profile requested for UserId: {UserId}", userId);

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Profile not found for UserId: {UserId}", userId);
            return NotFound(new { message = "User not found." });
        }

        return Ok(user);
    }
}
