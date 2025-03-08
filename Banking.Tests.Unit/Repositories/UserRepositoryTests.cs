using Banking.Application.Repositories.Implementations;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class UserRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new UserRepository(_dbContext);
    }
    /// <summary>
    /// Check getting a user by email
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FullName = "Test User",
            PasswordHash = "hash",
            Confirmed = false,
            Role = new RoleEntity { Id = Guid.NewGuid(), Name = "User" }
        };

        _dbContext.Roles.Add(user.Role);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    /// <summary>
    /// Check receiving null when user does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetUserByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Check getting unconfirmed users
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetUnconfirmedUsersAsync_ShouldReturnUsers_WhenExists()
    {
        // Arrange
        var user1 = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "user1@example.com",
            FullName = "Test User 1",
            PasswordHash = "hash",
            Confirmed = false
        };
        var user2 = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "user2@example.com",
            FullName = "Test User 2",
            PasswordHash = "hash",
            Confirmed = false
        };
        _dbContext.Users.AddRange(user1, user2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetUnconfirmedUsersAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Check receiving an empty list when no unconfirmed users
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ConfirmUserAsync_ShouldConfirmUser_WhenUserExists()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FullName = "Test User",
            PasswordHash = "hash",
            Confirmed = false
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.ConfirmUserAsync(user.Id);

        // Assert
        result.Should().BeTrue();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.Confirmed.Should().BeTrue();
    }

    /// <summary>
    /// Check receiving false when user does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task ConfirmUserAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.ConfirmUserAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Check assigning a role to a user
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AssignRoleAsync_ShouldAssignRole_WhenUserAndRoleExist()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FullName = "Test User",
            PasswordHash = "hash",
            Confirmed = true
        };

        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.AssignRoleAsync(user.Id, role.Id);

        // Assert
        result.Should().BeTrue();
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser!.RoleId.Should().Be(role.Id);
    }

    /// <summary>
    /// Check receiving false when user does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AssignRoleAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.AssignRoleAsync(Guid.NewGuid(), role.Id);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Check receiving false when role does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetUserWithRoleById_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "User" };
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FullName = "Test User",
            PasswordHash = "hash",
            RoleId = role.Id,
            Role = role
        };

        _dbContext.Roles.Add(role);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserWithRoleById(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Role.Should().NotBeNull();
        result.Role!.Name.Should().Be("User");
    }

    /// <summary>
    /// Check receiving null when user does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetUserWithRoleById_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.GetUserWithRoleById(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }
}
