using FluentAssertions;
using Microsoft.Extensions.Options;
using UserAuth.Domain.Entities;
using UserAuth.Infrastructure.Services;
using UserAuth.Infrastructure.Settings;
using Xunit;

namespace UserAuth.Tests.Unit;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var settings = Options.Create(new JwtSettings
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32CharactersLongForTesting!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 60
        });

        _tokenService = new TokenService(settings);
    }

    [Fact]
    public void GenerateToken_WithValidUser_ShouldReturnWellFormedJwt()
    {
        // Arrange
        var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        // Act
        var result = _tokenService.GenerateToken(user);

        // Assert
        result.Token.Should().NotBeNullOrEmpty();
        result.Token.Split('.').Should().HaveCount(3, "JWT must have header.payload.signature");
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void ValidateTokenAndGetUserId_WithValidToken_ShouldReturnCorrectUserId()
    {
        // Arrange
        var user = new User { Id = 42, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var result = _tokenService.GenerateToken(user);

        // Act
        var userId = _tokenService.ValidateTokenAndGetUserId(result.Token);

        // Assert
        userId.Should().Be(42);
    }

    [Fact]
    public void ValidateTokenAndGetUserId_WithInvalidToken_ShouldReturnNull()
    {
        // Act
        var userId = _tokenService.ValidateTokenAndGetUserId("this.is.invalid");

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void ValidateTokenAndGetUserId_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var userId = _tokenService.ValidateTokenAndGetUserId(string.Empty);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GenerateToken_ForDifferentUsers_ShouldReturnDifferentTokens()
    {
        // Arrange
        var user1 = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        var user2 = new User { Id = 2, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        // Act
        var token1 = _tokenService.GenerateToken(user1);
        var token2 = _tokenService.GenerateToken(user2);

        // Assert
        token1.Token.Should().NotBe(token2.Token);
    }
}
