using Banking.Application.Implementations;
using Banking.Application.Repositories.Interfaces;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

public class AccountServiceTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly AccountService _accountService;
    private readonly Mock<IOptions<BankInfo>> _bankInfoMock;

    public AccountServiceTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();

        _bankInfoMock = new Mock<IOptions<BankInfo>>();
        _bankInfoMock.Setup(o => o.Value).Returns(new BankInfo
        {
            Name = "TestBank",
            Country = "UA",
            Code = "380805",
            AccountLength = 19
        });

        _accountService = new AccountService(
            _accountRepositoryMock.Object,
            _bankInfoMock.Object
        );
    }

    /// <summary>
    /// Check if the user is the owner of the account
    /// </summary>
    [Fact]
    public async Task IsAccountOwnerAsync_ShouldReturnTrue_WhenUserIsOwner()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var account = new AccountEntity
        {
            Id = accountId,
            UserId = userId,
            AccountNumber = "UA2038080552523943628122",
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        _accountRepositoryMock.Setup(repo => repo.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _accountService.IsAccountOwnerAsync(accountId, userId);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Check if the user is not the owner of the account
    /// </summary>
    [Fact]
    public async Task IsAccountOwnerAsync_ShouldReturnFalse_WhenUserIsNotOwner()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var account = new AccountEntity
        {
            Id = accountId,
            UserId = anotherUserId,
            AccountNumber = "UA2038080552523943628122",
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        _accountRepositoryMock.Setup(repo => repo.GetByIdAsync(accountId))
            .ReturnsAsync(account);

        // Act
        var result = await _accountService.IsAccountOwnerAsync(accountId, userId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Check if the account does not exist
    /// </summary>
    [Fact]
    public async Task IsAccountOwnerAsync_ShouldReturnFalse_WhenAccountNotFound()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _accountRepositoryMock.Setup(repo => repo.GetByIdAsync(accountId))
            .ReturnsAsync((AccountEntity?)null);

        // Act
        var result = await _accountService.IsAccountOwnerAsync(accountId, userId);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Generate unique IBAN when account does not exist
    /// </summary>
    [Fact]
    public async Task GenerateUniqueIBANAsync_ShouldReturnValidIBAN()
    {
        // Arrange
        _accountRepositoryMock.Setup(repo => repo.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var iban = await _accountService.GenerateUniqueIBANAsync();

        // Assert
        iban.Should().StartWith("UA");
        iban.Should().Contain("380805");
        iban.Length.Should().Be(29);
    }

    /// <summary>
    /// Check error when invalid country code
    /// </summary>
    [Fact]
    public void GenerateUniqueIBANAsync_ShouldThrowException_WhenInvalidCountryCode()
    {
        // Arrange
        var invalidBankInfoMock = new Mock<IOptions<BankInfo>>();
        invalidBankInfoMock.Setup(o => o.Value).Returns(new BankInfo
        {
            Name = "TestBank",
            AccountLength = 24,
            Country = "U1", // Invalid country code
            Code = "380805"
        });


        var accountService = new AccountService(_accountRepositoryMock.Object, invalidBankInfoMock.Object);

        // Act & Assert
        Func<Task> act = async () => await accountService.GenerateUniqueIBANAsync();
        act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid country code. Must be 2 letters.");
    }

    /// <summary>
    /// Check error when invalid bank code
    /// </summary>
    [Fact]
    public void GenerateUniqueIBANAsync_ShouldThrowException_WhenInvalidBankCode()
    {
        // Arrange
        var invalidBankInfoMock = new Mock<IOptions<BankInfo>>();
        invalidBankInfoMock.Setup(o => o.Value).Returns(new BankInfo
        {
            Name = "TestBank",
            AccountLength = 24,
            Country = "UA",
            Code = "ABC" //Invalid bank code
        });

        var accountService = new AccountService(_accountRepositoryMock.Object, invalidBankInfoMock.Object);

        // Act & Assert
        Func<Task> act = async () => await accountService.GenerateUniqueIBANAsync();
        act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid bank code. Must be numeric.");
    }
}
