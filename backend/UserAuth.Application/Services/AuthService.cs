using Microsoft.Extensions.Logging;
using UserAuth.Application.DTOs;
using UserAuth.Application.Interfaces;
using UserAuth.Domain.Entities;
using UserAuth.Domain.Exceptions;
using UserAuth.Domain.Interfaces.Repositories;
using UserAuth.Domain.Interfaces.Services;

namespace UserAuth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IEmailService emailService,
        IPasswordHasher passwordHasher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.ToLower();
        _logger.LogInformation("Registration attempt for {Email}", email);

        var existing = await _userRepository.GetByEmailAsync(email);
        if (existing != null)
        {
            _logger.LogWarning("Registration rejected — email already in use: {Email}", email);
            throw new UserAlreadyExistsException(dto.Email);
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        _logger.LogInformation("User registered successfully — Id: {UserId}, Email: {Email}", user.Id, email);

        // Fire-and-forget welcome email
        _ = _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.ToLower();
        _logger.LogInformation("Login attempt for {Email}", email);

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed — invalid credentials for {Email}", email);
            throw new InvalidCredentialsException();
        }

        _logger.LogInformation("Login successful — Id: {UserId}, Email: {Email}", user.Id, email);
        return BuildAuthResponse(user);
    }

    private AuthResponseDto BuildAuthResponse(User user)
    {
        var result = _tokenService.GenerateToken(user);
        return new AuthResponseDto
        {
            Token = result.Token,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ExpiresAt = result.ExpiresAt
        };
    }
}
