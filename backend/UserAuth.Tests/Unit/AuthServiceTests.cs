using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserAuth.Application.DTOs;
using UserAuth.Application.Services;
using UserAuth.Domain.Common;
using UserAuth.Domain.Entities;
using UserAuth.Domain.Exceptions;
using UserAuth.Domain.Interfaces.Repositories;
using UserAuth.Domain.Interfaces.Services;
using Xunit;

namespace UserAuth.Tests.Unit;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();

        _tokenServiceMock
            .Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns(new TokenResult("mock-jwt-token", DateTime.UtcNow.AddHours(1)));

        _passwordHasherMock
            .Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        _emailServiceMock
            .Setup(e => e.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object,
            _emailServiceMock.Object,
            _passwordHasherMock.Object,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("john.doe@example.com")).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("mock-jwt-token");
        result.Email.Should().Be("john.doe@example.com");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange
        var plainPassword = "Password123!";
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = plainPassword
        };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        _passwordHasherMock.Verify(p => p.Hash(plainPassword), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowUserAlreadyExistsException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Password = "Password123!"
        };
        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync("john.doe@example.com"))
            .ReturnsAsync(new User { Email = "john.doe@example.com" });

        // Act & Assert
        await _authService
            .Invoking(s => s.RegisterAsync(dto))
            .Should().ThrowAsync<UserAlreadyExistsException>();
    }

    [Fact]
    public async Task RegisterAsync_ShouldNormalizeEmailToLowercase()
    {
        // Arrange
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "JOHN.DOE@EXAMPLE.COM",
            Password = "Password123!"
        };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("john.doe@example.com")).ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var email = "john.doe@example.com";
        var password = "Password123!";
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = email, PasswordHash = "hashed-password" };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.Verify(password, "hashed-password")).Returns(true);

        // Act
        var result = await _authService.LoginAsync(new LoginDto { Email = email, Password = password });

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().Be("mock-jwt-token");
        result.Email.Should().Be(email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var user = new User { Id = 1, Email = "john.doe@example.com", PasswordHash = "hashed-password" };
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("john.doe@example.com")).ReturnsAsync(user);
        _passwordHasherMock.Setup(p => p.Verify("WrongPassword!", "hashed-password")).Returns(false);

        // Act & Assert
        await _authService
            .Invoking(s => s.LoginAsync(new LoginDto
            {
                Email = "john.doe@example.com",
                Password = "WrongPassword!"
            }))
            .Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentEmail_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByEmailAsync("nobody@example.com")).ReturnsAsync((User?)null);

        // Act & Assert
        await _authService
            .Invoking(s => s.LoginAsync(new LoginDto
            {
                Email = "nobody@example.com",
                Password = "Password123!"
            }))
            .Should().ThrowAsync<InvalidCredentialsException>();
    }
}
