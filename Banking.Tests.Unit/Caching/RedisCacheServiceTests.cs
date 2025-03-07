using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Text.Json;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly IRedisCacheService _cacheService;
    private readonly IOptions<RedisOptions> _redisOptions;

    public RedisCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();

        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                  .Returns(_databaseMock.Object);

        var redisOptions = new RedisOptions
        {
            BalanceLifetimeMinutes = 1,
            Host = "localhost:6379",
            InstanceName = "TestInstance"
        };

        _redisOptions = Options.Create(redisOptions);

        _cacheService = new RedisCacheService(_redisMock.Object, _redisOptions);
    }

    /// <summary>
    /// Check getting balance from cache when key does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetBalanceAsync_ShouldReturnNull_WhenKeyNotExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var key = $"balance_{accountId}";
        _databaseMock.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
                     .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetBalanceAsync(accountId);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Check getting balance from cache
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetBalanceAsync_ShouldReturnDecimal_WhenKeyExists()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var key = $"balance_{accountId}";
        decimal balance = 123.45m;
        string balanceString = JsonSerializer.Serialize(balance);
        _databaseMock.Setup(db => db.StringGetAsync(key, It.IsAny<CommandFlags>()))
                     .ReturnsAsync(balanceString);

        // Act
        var result = await _cacheService.GetBalanceAsync(accountId);

        // Assert
        result.Should().Be(balance);
    }
    /// <summary>
    /// Check setting balance in cache
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateBalanceAsync_ShouldSetBalanceInCache()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var key = $"balance_{accountId}";
        decimal newBalance = 987.65m;
        string expectedValue = JsonSerializer.Serialize(newBalance);
        TimeSpan expectedExpiry = TimeSpan.FromMinutes(_redisOptions.Value.BalanceLifetimeMinutes);

        _databaseMock.Setup(db => db.StringSetAsync(
                key,
                expectedValue,
                expectedExpiry,
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _cacheService.UpdateBalanceAsync(accountId, newBalance);

        // Assert
        _databaseMock.Verify(db => db.StringSetAsync(
                key,
                expectedValue,
                expectedExpiry,
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()), Times.Once);
    }
}
