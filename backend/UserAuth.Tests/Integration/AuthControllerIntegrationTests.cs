using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UserAuth.Application.DTOs;
using UserAuth.Infrastructure.Data;
using Xunit;

namespace UserAuth.Tests.Integration;

public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // dbName captured here (once per test instance) so every HTTP request
        // within the same test shares the same InMemory store.
        var dbName = "IntegrationTestDb_" + Guid.NewGuid();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            // ConfigureTestServices runs AFTER all program services, guaranteeing
            // the InMemory registration wins over the PostgreSQL one.
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
            });
        });
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201WithToken()
    {
        // Arrange
        var client = CreateClient();
        var dto = new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = $"john.{Guid.NewGuid():N}@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var client = CreateClient();
        var email = $"dup.{Guid.NewGuid():N}@example.com";
        var dto = new RegisterDto { FirstName = "John", LastName = "Doe", Email = email, Password = "Password123!" };

        await client.PostAsJsonAsync("/api/auth/register", dto);

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithMissingFields_ShouldReturn400()
    {
        // Arrange
        var client = CreateClient();
        var dto = new { Email = "test@example.com" }; // missing required fields

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/register", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200WithToken()
    {
        // Arrange
        var client = CreateClient();
        var email = $"login.{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            Password = password
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = email, Password = password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginDto { Email = "nobody@example.com", Password = "WrongPass!" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturn401()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturn200WithUserDetails()
    {
        // Arrange
        var client = CreateClient();
        var email = $"me.{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = email,
            Password = "Password123!"
        });

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDetailsDto>();
        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
    }
}
