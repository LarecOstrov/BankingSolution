using Banking.API.Controllers;
using Banking.Application.Services.Interfaces;
using Banking.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    /// <summary>
    /// Test successful user registration
    /// </summary>
    [Fact]
    public async Task Register_ShouldReturnOk_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var request = new RegisterRequest("John Doe", "test@example.com", "password123", "Client");
        _authServiceMock.Setup(service => service.RegisterAsync(request)).ReturnsAsync(true);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be("User registered. Wait account verification.");
    }

    /// <summary>
    /// Test failed user registration
    /// </summary>
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenRegistrationFails()
    {
        // Arrange
        var request = new RegisterRequest("John Doe", "test@example.com", "password123", "Client");
        _authServiceMock.Setup(service => service.RegisterAsync(request)).ReturnsAsync(false);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("Registration failed");
    }

    /// <summary>
    /// Test successful login
    /// </summary>
    [Fact]
    public async Task Login_ShouldReturnOk_WhenLoginIsSuccessful()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "password123");
        var response = new LoginResponse("access-token", "refresh-token");

        _authServiceMock.Setup(service => service.LoginAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(response);
    }

    /// <summary>
    /// Test failed login
    /// </summary>
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenLoginFails()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "wrongpassword");
        _authServiceMock.Setup(service => service.LoginAsync(request)).ReturnsAsync((LoginResponse?)null);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }

    /// <summary>
    /// Test successful token refresh
    /// </summary>
    [Fact]
    public async Task Refresh_ShouldReturnOk_WhenTokenIsValid()
    {
        // Arrange
        var refreshToken = "valid-refresh-token";
        var newToken = "new-access-token";

        _authServiceMock.Setup(service => service.RefreshTokenAsync(refreshToken)).ReturnsAsync(newToken);

        // Act
        var result = await _controller.Refresh(refreshToken);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { Token = newToken });
    }

    /// <summary>
    /// Test failed token refresh
    /// </summary>
    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsInvalid()
    {
        // Arrange
        var refreshToken = "invalid-refresh-token";
        _authServiceMock.Setup(service => service.RefreshTokenAsync(refreshToken)).ReturnsAsync((string?)null);

        // Act
        var result = await _controller.Refresh(refreshToken);

        // Assert
        result.Should().BeOfType<UnauthorizedResult>();
    }
}
