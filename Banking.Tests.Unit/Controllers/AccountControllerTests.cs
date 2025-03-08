using Banking.API.Controllers;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

public class AccountControllerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IRedisCacheService> _redisCacheServiceMock;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _authServiceMock = new Mock<IAuthService>();
        _redisCacheServiceMock = new Mock<IRedisCacheService>();

        _controller = new AccountController(
            _accountRepositoryMock.Object,
            _authServiceMock.Object,
            _redisCacheServiceMock.Object
        );
    }

    /// <summary>
    /// Check that the controller returns 401 when the user ID is not found in the token
    /// </summary>
    [Fact]
    public async Task GetAccountDetails_ShouldReturnUnauthorized_WhenUserIdIsNull()
    {
        // Arrange
        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        // Act
        var result = await _controller.GetAccountDetails(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("User ID not found in token.");
    }

    /// <summary>
    /// Check that the controller returns 404 when the account does not exist
    /// </summary>
    [Fact]
    public async Task GetAccountDetails_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _accountRepositoryMock.Setup(r => r.GetByAccountNumberAsync(accountId, userId))
            .ReturnsAsync((AccountEntity?)null);

        // Act
        var result = await _controller.GetAccountDetails(accountId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Account not found.");
    }

    /// <summary>
    /// Check that the controller returns 200 when the account exists
    /// </summary>
    [Fact]
    public async Task GetAccountDetails_ShouldReturnOk_WhenAccountExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new AccountEntity { Id = accountId, UserId = userId, AccountNumber = "UA2038080552523943628122", Balance = 1000 };

        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _accountRepositoryMock.Setup(r => r.GetByAccountNumberAsync(accountId, userId))
            .ReturnsAsync(account);

        // Act
        var result = await _controller.GetAccountDetails(accountId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(account);
    }

    /// <summary>
    /// Check that the controller returns 401 when the user ID is not found in the token
    /// </summary>
    [Fact]
    public async Task GetBalance_ShouldReturnUnauthorized_WhenUserIdIsNull()
    {
        // Arrange
        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        // Act
        var result = await _controller.GetBalance(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("User ID not found in token.");
    }

    /// <summary>
    /// Check that if the balance is in the cache, it is returned from the cache
    /// </summary>
    [Fact]
    public async Task GetBalance_ShouldReturnBalanceFromCache_WhenCacheExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var balance = 500m;

        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _redisCacheServiceMock.Setup(c => c.GetBalanceAsync(accountId))
            .ReturnsAsync(balance);

        // Act
        var result = await _controller.GetBalance(accountId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be($"Balance for account {accountId}: {balance}");
    }

    /// <summary>
    /// Check that if the balance is not in the cache, it is obtained from the repository
    /// </summary>
    [Fact]
    public async Task GetBalance_ShouldReturnBalanceFromRepository_WhenCacheIsEmpty()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new AccountEntity { Id = accountId, UserId = userId, AccountNumber = "UA2038080552523943628122", Balance = 1200 };

        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _redisCacheServiceMock.Setup(c => c.GetBalanceAsync(accountId))
            .ReturnsAsync((decimal?)null);

        _accountRepositoryMock.Setup(r => r.GetByAccountNumberAsync(accountId, userId))
            .ReturnsAsync(account);

        // Act
        var result = await _controller.GetBalance(accountId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be($"Balance for account {account.Id}: {account.Balance}");
    }

    /// <summary>
    /// Check that the controller returns 404 when the account does not exist
    /// </summary>
    [Fact]
    public async Task GetBalance_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _authServiceMock.Setup(s => s.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _redisCacheServiceMock.Setup(c => c.GetBalanceAsync(accountId))
            .ReturnsAsync((decimal?)null);

        _accountRepositoryMock.Setup(r => r.GetByAccountNumberAsync(accountId, userId))
            .ReturnsAsync((AccountEntity?)null);

        // Act
        var result = await _controller.GetBalance(accountId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Account not found.");
    }
}
