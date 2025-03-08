using Banking.API.GraphQL;
using Banking.Application.Repositories.Interfaces;
using Banking.Domain.Entities;
using Banking.Domain.Enums;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using HotChocolate;
using Moq;


public class QueryTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IFailedTransactionRepository> _failedTransactionRepositoryMock;
    private readonly Mock<IBalanceHistoryRepository> _balanceHistoryRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Query _query;

    public QueryTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _failedTransactionRepositoryMock = new Mock<IFailedTransactionRepository>();
        _balanceHistoryRepositoryMock = new Mock<IBalanceHistoryRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();

        _accountRepositoryMock.Setup(r => r.GetAll())
            .Returns(Enumerable.Empty<AccountEntity>().AsQueryable());
        _transactionRepositoryMock.Setup(r => r.GetAll())
            .Returns(Enumerable.Empty<TransactionEntity>().AsQueryable());
        _failedTransactionRepositoryMock.Setup(r => r.GetAll())
            .Returns(Enumerable.Empty<FailedTransactionEntity>().AsQueryable());
        _balanceHistoryRepositoryMock.Setup(r => r.GetAll())
            .Returns(Enumerable.Empty<BalanceHistoryEntity>().AsQueryable());
        _userRepositoryMock.Setup(r => r.GetAll())
            .Returns(Enumerable.Empty<UserEntity>().AsQueryable());
        _roleRepositoryMock.Setup(r => r.GetAll())
            .Returns(Enumerable.Empty<RoleEntity>().AsQueryable());

        _query = new Query(
            _accountRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _failedTransactionRepositoryMock.Object,
            _balanceHistoryRepositoryMock.Object,
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object
        );
    }

    /// <summary>
    /// Check if the query returns accounts
    /// </summary>
    [Fact]
    public void GetAccounts_ShouldReturnAccounts_WhenRepositorySucceeds()
    {
        // Arrange
        var accounts = new List<AccountEntity>
        {
            new AccountEntity
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AccountNumber = "UA2038080552523943628122",
                Balance = 1000

            },
            new AccountEntity
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AccountNumber = "UA2038080552523943628123",
                Balance = 2000

            }
        }.AsQueryable();

        _accountRepositoryMock.Setup(r => r.GetAll()).Returns(accounts);

        // Act
        var result = _query.GetAccounts();

        // Assert
        result.Should().BeEquivalentTo(accounts);
    }

    /// <summary>
    /// Check if the query throws an exception when the repository fails
    /// </summary>
    [Fact]
    public void GetAccounts_ShouldThrowGraphQLException_WhenRepositoryThrows()
    {
        // Arrange
        var exceptionMessage = "Database error occurred.";
        _accountRepositoryMock.Setup(r => r.GetAll())
            .Throws(new Exception(exceptionMessage));

        // Act
        Action act = () => _query.GetAccounts();

        // Assert
        act.Should().Throw<GraphQLException>()
            .WithMessage($"Failed to get Accounts: {exceptionMessage}");
    }

    /// <summary>
    /// Check if the query returns transactions
    /// </summary>
    [Fact]
    public void GetTransactions_ShouldReturnTransactions_WhenRepositorySucceeds()
    {
        // Arrange
        var transactions = new List<TransactionEntity>
        {
            new TransactionEntity
            {
                Id = Guid.NewGuid(),
                Amount = 100,
                FromAccountId = Guid.NewGuid(),
                ToAccountId = Guid.NewGuid(),
                Status = TransactionStatus.Pending
            },
            new TransactionEntity
            {
                Id = Guid.NewGuid(),
                Amount = 200,
                FromAccountId = Guid.NewGuid(),
                ToAccountId = Guid.NewGuid(),
                Status = TransactionStatus.Pending
            }
        }.AsQueryable();
        _transactionRepositoryMock.Setup(r => r.GetAll()).Returns(transactions);

        // Act
        var result = _query.GetTransactions();

        // Assert
        result.Should().BeEquivalentTo(transactions);
    }

    /// <summary>
    /// Check if the query throws an exception when the repository fails
    /// </summary>
    [Fact]
    public void GetUsers_ShouldReturnUsers_WhenRepositorySucceeds()
    {
        // Arrange
        var users = new List<UserEntity>
        {
            new UserEntity
            {
                Id = Guid.NewGuid(),
                FullName = "John Doe",
                Email = "example@test.com",
                PasswordHash = "hash"
            },
            new UserEntity {
                Id = Guid.NewGuid(),
                FullName = "Jane Doe",
                Email = "example2@test.com",
                PasswordHash = "hash"
            }
        }.AsQueryable();
        _userRepositoryMock.Setup(r => r.GetAll()).Returns(users);

        // Act
        var result = _query.GetUsers();

        // Assert
        result.Should().BeEquivalentTo(users);
    }

    /// <summary>
    /// Check if the query throws an exception when the repository fails
    /// </summary>
    [Fact]
    public void GetFailedTransactions_ShouldReturnFailedTransactions_WhenRepositorySucceeds()
    {
        // Arrange
        var failedTransactions = new List<FailedTransactionEntity>
            {
                new FailedTransactionEntity { TransactionMessage = "Message1", Reason = "Reason1" },
                new FailedTransactionEntity { TransactionMessage = "Message2", Reason = "Reason2" }
            }.AsQueryable();

        _failedTransactionRepositoryMock.Setup(r => r.GetAll())
            .Returns(failedTransactions);

        // Act
        var result = _query.GetFailedTransactions();

        // Assert
        result.Should().BeEquivalentTo(failedTransactions);
    }

    /// <summary>
    /// Check if the query throws an exception when the repository fails
    /// </summary>
    [Fact]
    public void GetFailedTransactions_ShouldThrowGraphQLException_WhenRepositoryThrows()
    {
        // Arrange
        var exceptionMessage = "Database error occurred.";
        _failedTransactionRepositoryMock.Setup(r => r.GetAll())
            .Throws(new Exception(exceptionMessage));

        // Act
        Action act = () => _query.GetFailedTransactions();

        // Assert
        act.Should().Throw<GraphQLException>()
            .WithMessage($"Failed to get Failed Transactions: {exceptionMessage}");
    }

    /// <summary>
    /// Check if the query returns balance histories
    /// </summary>
    [Fact]
    public void GetBalanceHistories_ShouldReturnBalanceHistories_WhenRepositorySucceeds()
    {
        // Arrange
        var histories = new List<BalanceHistoryEntity>
            {
                new BalanceHistoryEntity
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    NewBalance = 500,
                    CreatedAt = DateTime.UtcNow
                },
                new BalanceHistoryEntity
                {
                    Id = Guid.NewGuid(),
                    AccountId = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    NewBalance = 600,
                    CreatedAt = DateTime.UtcNow
                }
            }.AsQueryable();

        _balanceHistoryRepositoryMock.Setup(r => r.GetAll())
            .Returns(histories);

        // Act
        var result = _query.GetBalanceHistories();

        // Assert
        result.Should().BeEquivalentTo(histories);
    }

    /// <summary>
    /// Check if the query throws an exception when the repository fails
    /// </summary>
    [Fact]
    public void GetBalanceHistories_ShouldThrowGraphQLException_WhenRepositoryThrows()
    {
        // Arrange
        var exceptionMessage = "Database error occurred.";
        _balanceHistoryRepositoryMock.Setup(r => r.GetAll())
            .Throws(new Exception(exceptionMessage));

        // Act
        Action act = () => _query.GetBalanceHistories();

        // Assert
        act.Should().Throw<GraphQLException>()
            .WithMessage($"Failed to get Balance Histories: {exceptionMessage}");
    }

    /// <summary>
    /// Check if the query returns roles
    /// </summary>
    [Fact]
    public void GetRoles_ShouldReturnRoles_WhenRepositorySucceeds()
    {
        // Arrange
        var roles = new List<RoleEntity>
            {
                new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "Admin"
                },
                new RoleEntity
                {
                    Id = Guid.NewGuid(),
                    Name = "User"
                }
            }.AsQueryable();

        _roleRepositoryMock.Setup(r => r.GetAll())
            .Returns(roles);

        // Act
        var result = _query.GetRoles();

        // Assert
        result.Should().BeEquivalentTo(roles);
    }

    /// <summary>
    /// Check if the query throws an exception when the repository fails
    /// </summary>
    [Fact]
    public void GetRoles_ShouldThrowGraphQLException_WhenRepositoryThrows()
    {
        // Arrange
        var exceptionMessage = "Database error occurred.";
        _roleRepositoryMock.Setup(r => r.GetAll())
            .Throws(new Exception(exceptionMessage));

        // Act
        Action act = () => _query.GetRoles();

        // Assert
        act.Should().Throw<GraphQLException>()
            .WithMessage($"Failed to get Roles: {exceptionMessage}");
    }
}
