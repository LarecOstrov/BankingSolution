using Banking.API.Controllers;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

public class TransactionControllerTests
{
    private readonly Mock<IPublishService> _publishServiceMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly TransactionController _controller;

    public TransactionControllerTests()
    {
        _publishServiceMock = new Mock<IPublishService>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _authServiceMock = new Mock<IAuthService>();
        _accountServiceMock = new Mock<IAccountService>();

        _controller = new TransactionController(
            _transactionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _publishServiceMock.Object,
            _authServiceMock.Object,
            _accountServiceMock.Object
        );
    }

    /// <summary>
    /// Test that Deposit returns Accepted with transaction details
    /// </summary>
    [Fact]
    public async Task Deposit_ShouldReturnAccepted_WhenValidRequest()
    {
        // Arrange
        var request = new DepositRequest(Guid.NewGuid(), 500);
        var transaction = new Transaction(Guid.NewGuid(), null, request.ToAccountId, request.Amount, DateTime.UtcNow, TransactionStatus.Pending);

        _publishServiceMock.Setup(service => service.PublishTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Deposit(request);

        // Assert
        result.Should().BeOfType<AcceptedResult>()
            .Which.Value.Should().Be(transaction);
    }

    /// <summary>
    /// Test that Withdraw returns Accepted with transaction details
    /// </summary>
    [Fact]
    public async Task Withdraw_ShouldReturnAccepted_WhenValidRequest()
    {
        // Arrange
        var request = new WithdrawRequest(Guid.NewGuid(), 300);
        var transaction = new Transaction(Guid.NewGuid(), request.FromAccountId, null, request.Amount, DateTime.UtcNow, TransactionStatus.Pending);

        _publishServiceMock.Setup(service => service.PublishTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Withdraw(request);

        // Assert
        result.Should().BeOfType<AcceptedResult>()
            .Which.Value.Should().Be(transaction);
    }

    /// <summary>
    /// Test that Transfer returns Unauthorized when user ID is not found
    /// </summary>
    [Fact]
    public async Task Transfer_ShouldReturnUnauthorized_WhenUserIdNotFound()
    {
        // Arrange
        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        var request = new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), 150);

        // Act
        var result = await _controller.Transfer(request);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("User ID not found in token.");
    }

    /// <summary>
    /// Test that Transfer returns NotFound when user does not exist
    /// </summary>
    [Fact]
    public async Task Transfer_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _userRepositoryMock.Setup(repo => repo.GetUserWithRoleById(userId))
            .ReturnsAsync((UserEntity?)null);

        var request = new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), 150);

        // Act
        var result = await _controller.Transfer(request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("User not found.");
    }

    /// <summary>
    /// Test that Transfer returns Forbid when user is not Admin and does not own the account
    /// </summary>
    [Fact]
    public async Task Transfer_ShouldReturnForbid_WhenUserIsNotAdminAndNotOwner()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, Role = new RoleEntity { Name = "Client" }, Email = "test@example.com", PasswordHash = "hash", FullName = "John Doe" };
        var request = new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), 200);

        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _userRepositoryMock.Setup(repo => repo.GetUserWithRoleById(userId))
            .ReturnsAsync(user);

        _accountServiceMock.Setup(service => service.IsAccountOwnerAsync(request.FromAccountId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Transfer(request);

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    /// <summary>
    /// Test that Transfer returns Accepted with transaction details when valid
    /// </summary>
    [Fact]
    public async Task Transfer_ShouldReturnAccepted_WhenValidRequest()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserEntity { Id = userId, FullName = "John Doe", Email = "test@example.com", PasswordHash = "hash", Role = new RoleEntity { Name = "Client" } };
        var request = new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), 200);
        var transaction = new Transaction(Guid.NewGuid(), request.FromAccountId, request.ToAccountId, request.Amount, DateTime.UtcNow, TransactionStatus.Pending);

        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _userRepositoryMock.Setup(repo => repo.GetUserWithRoleById(userId))
            .ReturnsAsync(user);

        _accountServiceMock.Setup(service => service.IsAccountOwnerAsync(request.FromAccountId, userId))
            .ReturnsAsync(true);

        _publishServiceMock.Setup(service => service.PublishTransactionAsync(It.IsAny<Transaction>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _controller.Transfer(request);

        // Assert
        result.Should().BeOfType<AcceptedResult>()
            .Which.Value.Should().Be(transaction);
    }

    /// <summary>
    /// Test that GetTransaction returns Unauthorized when user ID is not found
    /// </summary>
    [Fact]
    public async Task GetTransaction_ShouldReturnUnauthorized_WhenUserIdNotFound()
    {
        // Arrange
        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns((Guid?)null);

        // Act
        var result = await _controller.GetTransaction(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().Be("User ID not found in token.");
    }

    /// <summary>
    /// Test that GetTransaction returns NotFound when transaction does not exist
    /// </summary>
    [Fact]
    public async Task GetTransaction_ShouldReturnNotFound_WhenTransactionDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authServiceMock.Setup(service => service.GetUserIdFromToken(It.IsAny<ClaimsPrincipal>()))
            .Returns(userId);

        _transactionRepositoryMock.Setup(repo => repo.GetByTransactionIdAsync(It.IsAny<Guid>(), userId))
            .ReturnsAsync((TransactionEntity?)null);

        // Act
        var result = await _controller.GetTransaction(Guid.NewGuid());

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().Be("Transaction not found.");
    }
}
