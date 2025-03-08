using Banking.API.Controllers;
using Banking.Application.Repositories.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

public class AdminControllerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();

        _controller = new AdminController(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object
        );
    }

    /// <summary>
    /// Check that the controller returns a list of unconfirmed users
    /// </summary>
    [Fact]
    public async Task GetUnconfirmedUsers_ShouldReturnListOfUsers()
    {
        // Arrange
        var users = new List<UserEntity>
        {
            new UserEntity { Id = Guid.NewGuid(), FullName = "John Doe", Email = "john@example.com", Confirmed = false, PasswordHash = "hash" },
            new UserEntity { Id = Guid.NewGuid(), FullName = "Jane Smith", Email = "jane@example.com", Confirmed = false, PasswordHash = "hash" }
        };

        _userRepositoryMock.Setup(repo => repo.GetUnconfirmedUsersAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _controller.GetUnconfirmedUsers();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(users);
    }

    /// <summary>
    /// Check that the controller returns 200 when the user is confirmed
    /// </summary>
    [Fact]
    public async Task ConfirmUser_ShouldReturnOk_WhenUserConfirmed()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock.Setup(repo => repo.ConfirmUserAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ConfirmUser(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { message = "User confirmed successfully" });

        _userRepositoryMock.Verify(repo => repo.ConfirmUserAsync(userId), Times.Once);
    }

    /// <summary>
    /// Check receiving all roles
    /// </summary>
    [Fact]
    public async Task GetRoles_ShouldReturnAllRoles()
    {
        // Arrange
        var roles = new List<RoleEntity>
        {
            new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" },
            new RoleEntity { Id = Guid.NewGuid(), Name = "Client" }
        };

        var rolesMock = new Mock<DbSet<RoleEntity>>();
        rolesMock.As<IAsyncEnumerable<RoleEntity>>()
            .Setup(m => m.GetAsyncEnumerator(default))
            .Returns(new TestAsyncEnumerator<RoleEntity>(roles.GetEnumerator()));

        _roleRepositoryMock.Setup(repo => repo.GetAll()).Returns(rolesMock.Object);

        // Act
        var result = await _controller.GetRoles();

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(roles);
    }

    /// <summary>
    /// Check that the controller returns 400 when the role is not found
    /// </summary>
    [Fact]
    public async Task AssignRole_ShouldReturnBadRequest_WhenRoleNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AssignRoleRequest("Manager");

        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(request.RoleName))
            .ReturnsAsync((RoleEntity?)null);

        // Act
        var result = await _controller.AssignRole(userId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { message = "Role not found" });
    }

    /// <summary>
    /// Check that the controller returns 200 when the role is assigned
    /// </summary>
    [Fact]
    public async Task AssignRole_ShouldReturnOk_WhenRoleAssigned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AssignRoleRequest("Admin");
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };

        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(request.RoleName))
            .ReturnsAsync(role);

        _userRepositoryMock.Setup(repo => repo.AssignRoleAsync(userId, role.Id))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.AssignRole(userId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { message = $"Role '{request.RoleName}' assigned successfully" });
    }

    /// <summary>
    /// Check that the controller returns 404 when the user is not found or the role is already assigned
    /// </summary>
    [Fact]
    public async Task AssignRole_ShouldReturnNotFound_WhenUserNotFoundOrRoleAlreadyAssigned()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AssignRoleRequest("Admin");
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Admin" };

        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(request.RoleName))
            .ReturnsAsync(role);

        _userRepositoryMock.Setup(repo => repo.AssignRoleAsync(userId, role.Id))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.AssignRole(userId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { message = "User not found or role already assigned" });
    }

    /// <summary>
    /// Check that the controller returns 200 when the role is created
    /// </summary>
    [Fact]
    public async Task CreateRole_ShouldReturnOk_WhenRoleCreated()
    {
        // Arrange
        var roleName = "Manager";

        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(roleName))
            .ReturnsAsync((RoleEntity?)null);

        _roleRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<RoleEntity>()))
            .ReturnsAsync((RoleEntity entity) => entity);

        // Act
        var result = await _controller.CreateRole(roleName);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { message = "Role created successfully" });
    }

    /// <summary>
    /// Check that the controller returns 400 when the role already exists
    /// </summary>
    [Fact]
    public async Task CreateRole_ShouldReturnBadRequest_WhenRoleAlreadyExists()
    {
        // Arrange
        var roleName = "Admin";
        var existingRole = new RoleEntity { Id = Guid.NewGuid(), Name = roleName };

        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(roleName))
            .ReturnsAsync(existingRole);

        // Act
        var result = await _controller.CreateRole(roleName);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { message = "Role already exists" });
    }
}
