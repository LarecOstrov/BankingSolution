using Banking.Application.Repositories.Implementations;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class RefreshTokenRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RefreshTokenRepository _repository;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new RefreshTokenRepository(_dbContext);
    }

    /// <summary>
    /// Check receiving a token
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetByTokenAsync_ShouldReturnToken_WhenExists()
    {
        // Arrange
        var role = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = "Admin"
        };

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = "hash",
            RoleId = role.Id,
            Role = role
        };

        var tokenEntity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid_token",
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            User = user
        };

        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _dbContext.RefreshTokens.Add(tokenEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTokenAsync("valid_token");

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().Be("valid_token");
        result.User.Should().NotBeNull();
        result.User.FullName.Should().Be("Test User");
        result.User.Role.Should().NotBeNull();
        result.User.Role.Name.Should().Be("Admin");
    }


    /// <summary>
    /// Check receiving a non-existent token
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetByTokenAsync_ShouldReturnNull_WhenTokenDoesNotExist()
    {
        // Arrange
        // Database is empty

        // Act
        var result = await _repository.GetByTokenAsync("non_existent_token");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Check adding a token
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveToken_WhenExists()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "Test User",
            Email = "test@test.com",
            PasswordHash = "hash"
        };

        var tokenEntity = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid_token",
            ExpiryDate = DateTime.UtcNow.AddDays(1),
            User = user
        };

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(tokenEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync("valid_token");
        var result = await _repository.GetByTokenAsync("valid_token");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Check deleting a non-existent token
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task DeleteAsync_ShouldDoNothing_WhenTokenDoesNotExist()
    {
        // Arrange
        // Empty database, no tokens

        // Act
        await _repository.DeleteAsync("non_existent_token");

        // Assert
        var tokenCount = await _dbContext.RefreshTokens.CountAsync();
        tokenCount.Should().Be(0);
    }
}
