using Banking.Application.Repositories.Implementations;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class RoleRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RoleRepository _repository;

    public RoleRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new RoleRepository(_dbContext);
    }
    /// <summary>
    /// Check getting a role by name
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetRoleByNameAsync_ShouldReturnRole_WhenExists()
    {
        // Arrange
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };
        _dbContext.Roles.Add(role);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetRoleByNameAsync("Admin");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Admin");
    }

    /// <summary>
    /// Check receiving null when role does not exist
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetRoleByNameAsync_ShouldReturnNull_WhenRoleDoesNotExist()
    {
        // Act
        var result = await _repository.GetRoleByNameAsync("NonExistingRole");

        // Assert
        result.Should().BeNull();
    }
}
