using Banking.API.Controllers;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

public class BalanceHistoryControllerTests
{
    private readonly Mock<IBalanceHistoryRepository> _balanceHistoryRepositoryMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly BalanceHistoryController _controller;

    public BalanceHistoryControllerTests()
    {
        _balanceHistoryRepositoryMock = new Mock<IBalanceHistoryRepository>();
        _authServiceMock = new Mock<IAuthService>();
        _controller = new BalanceHistoryController(_balanceHistoryRepositoryMock.Object, _authServiceMock.Object);
    }

    /// <summary>
    /// Test that GetBalanceHistory returns Unauthorized when user ID is not found in the token
    /// </summary>
    [Fact]
    public async Task GetBalanceHistory_ShouldReturnUnauthorized_WhenUserIdNotFound()
    {
        // Arrange
        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        // Act
        var result = await _controller.GetBalanceHistory(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("User ID not found in token.");
    }

    /// <summary>
    /// Test that GetBalanceHistory returns NotFound when balance history does not exist
    /// </summary>
    [Fact]
    public async Task GetBalanceHistory_ShouldReturnNotFound_WhenBalanceHistoryDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _balanceHistoryRepositoryMock.Setup(repo => repo.GetBalanceHistoryByAccountIdAsync(accountId, userId))
            .ReturnsAsync((List<BalanceHistoryEntity>?)null!);

        // Act
        var result = await _controller.GetBalanceHistory(accountId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Balance history not found.");
    }

    /// <summary>
    /// Test that GetBalanceHistory returns Ok with balance history data when found
    /// </summary>
    [Fact]
    public async Task GetBalanceHistory_ShouldReturnOk_WhenBalanceHistoryExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var balanceHistory = new List<BalanceHistoryEntity>
        {
            new BalanceHistoryEntity { Id = Guid.NewGuid(), TransactionId= Guid.NewGuid(), AccountId = accountId, NewBalance = 500, CreatedAt = DateTime.UtcNow },
            new BalanceHistoryEntity { Id = Guid.NewGuid(), TransactionId= Guid.NewGuid(), AccountId = accountId, NewBalance = 1000, CreatedAt = DateTime.UtcNow }
        };

        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _balanceHistoryRepositoryMock.Setup(repo => repo.GetBalanceHistoryByAccountIdAsync(accountId, userId))
            .ReturnsAsync(balanceHistory);

        // Act
        var result = await _controller.GetBalanceHistory(accountId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(balanceHistory);
    }
}
