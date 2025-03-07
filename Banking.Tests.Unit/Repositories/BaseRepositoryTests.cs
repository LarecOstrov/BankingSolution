using Banking.Application.Repositories.Implementations;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class BaseRepositoryTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly BaseRepository<RoleEntity> _repository;

    public BaseRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _repository = new RoleRepository(_dbContext);
    }

    /// <summary>
    /// Check adding an entity
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var entity = new RoleEntity { Id = Guid.NewGuid(), Name = "Test Entity" };

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(entity);
    }

    /// <summary>
    /// Check receiving an entity by Id
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        // Arrange
        var entity = new RoleEntity { Id = Guid.NewGuid(), Name = "Test Entity" };
        await _repository.AddAsync(entity);

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(entity);
    }

    /// <summary>
    /// Check updating an entity
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var entity = new RoleEntity { Id = Guid.NewGuid(), Name = "Old Name" };
        await _repository.AddAsync(entity);
        entity.Name = "Updated Name";

        // Act
        var result = await _repository.UpdateAsync(entity);

        // Assert
        result.Should().BeTrue();
        var updatedEntity = await _repository.GetByIdAsync(entity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity.Name.Should().Be("Updated Name");
    }


    /// <summary>
    /// Check deleting an entity
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        // Arrange
        var entity = new RoleEntity { Id = Guid.NewGuid(), Name = "Test Entity" };
        await _repository.AddAsync(entity);

        // Act
        var result = await _repository.DeleteAsync(entity.Id);

        // Assert
        result.Should().BeTrue();
        var deletedEntity = await _repository.GetByIdAsync(entity.Id);
        deletedEntity.Should().BeNull();
    }

    /// <summary>
    /// Check getting all entities
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task GetAll_ShouldReturnAllEntities()
    {
        // Arrange
        var entity1 = new RoleEntity { Id = Guid.NewGuid(), Name = "Entity 1" };
        var entity2 = new RoleEntity { Id = Guid.NewGuid(), Name = "Entity 2" };
        await _repository.AddAsync(entity1);
        await _repository.AddAsync(entity2);

        // Act
        var result = _repository.GetAll().ToList();

        // Assert
        result.Should().HaveCount(2);
    }
}
