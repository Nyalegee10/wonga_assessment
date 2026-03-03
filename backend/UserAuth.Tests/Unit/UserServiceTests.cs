using FluentAssertions;
using Moq;
using UserAuth.Application.Services;
using UserAuth.Domain.Entities;
using UserAuth.Domain.Interfaces.Repositories;
using Xunit;

namespace UserAuth.Tests.Unit;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userService = new UserService(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithExistingId_ShouldReturnUserDetails()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow
        };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        _userRepositoryMock.Setup(r => r.GetByIdAsync(9999)).ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserByIdAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldNotExposePasswordHash()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = "super_secret_hash",
            CreatedAt = DateTime.UtcNow
        };
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        // UserDetailsDto has no PasswordHash property - verified by compilation
        var properties = result!.GetType().GetProperties();
        properties.Should().NotContain(p => p.Name == "PasswordHash");
    }
}
