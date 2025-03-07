using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services;
using Banking.Application.Services.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAccountService> _accountServiceMock;
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<IOptions<JwtOptions>> _optionsMock;
    private readonly AuthService _authService;
    private readonly JwtOptions _jwtOptions;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _accountServiceMock = new Mock<IAccountService>();
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _jwtOptions = new JwtOptions
        {
            SecretKey = Guid.NewGuid().ToString(),
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 30
        };

        _optionsMock = new Mock<IOptions<JwtOptions>>();
        _optionsMock.Setup(o => o.Value).Returns(_jwtOptions);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _accountServiceMock.Object,
            _accountRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _optionsMock.Object
        );
    }

    /// <summary>
    /// Test successful login
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var password = "securepassword";
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "test@test.com",
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password))),
            Confirmed = true,
            Role = new RoleEntity { Name = "Client" }
        };

        _userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);
        _refreshTokenRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<RefreshTokenEntity>()))
            .ReturnsAsync(new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = "valid-refresh-token",
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            });

        // Act
        var result = await _authService.LoginAsync(new LoginRequest(user.Email, password));

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test login failure when password is incorrect
    /// </summary>
    [Fact]
    public async Task LoginAsync_ShouldThrowInvalidOperationException_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "test@test.com",
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("correctpassword"))),
            Confirmed = true
        };

        _userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(user.Email)).ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _authService.LoginAsync(new LoginRequest(user.Email, "wrongpassword"));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid email or password");
    }

    /// <summary>
    /// Test successful registration
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldReturnTrue_WhenRegistrationIsSuccessful()
    {
        // Arrange
        var request = new RegisterRequest("John Doe", "test@test.com", "password123", "Client");
        var role = new RoleEntity { Id = Guid.NewGuid(), Name = "Client" };
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password))),
            RoleId = role.Id,
            Confirmed = false
        };
        var transactionMock = new Mock<IDbContextTransaction>();

        _userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(request.Email)).ReturnsAsync((UserEntity?)null);
        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(request.Role)).ReturnsAsync(role);
        _userRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<UserEntity>())).ReturnsAsync((UserEntity u) => u);
        _accountServiceMock.Setup(service => service.GenerateUniqueIBANAsync()).ReturnsAsync("UA2038080552523943628122");
        _accountRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<AccountEntity>())).ReturnsAsync((AccountEntity acc) => acc);
        _userRepositoryMock.Setup(repo => repo.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        result.Should().BeTrue();
    }


    /// <summary>
    /// Test failed registration when email already exists
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest("John Doe", "test@test.com", "password123!", "Client");
        _userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(request.Email))
            .ReturnsAsync(new UserEntity
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password))),
                Role = new RoleEntity { Name = "Client" }
            }
            );

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User with this email already exists.");
    }

    /// <summary>
    /// Test failed registration when role not found
    /// </summary>
    [Fact]
    public async Task RegisterAsync_ShouldThrowInvalidOperationException_WhenRoleNotFound()
    {
        // Arrange
        var request = new RegisterRequest("John Doe", "test@test.com", "password123", "UnknownRole");
        var transactionMock = new Mock<IDbContextTransaction>();

        _userRepositoryMock.Setup(repo => repo.GetUserByEmailAsync(request.Email)).ReturnsAsync((UserEntity?)null);
        _roleRepositoryMock.Setup(repo => repo.GetRoleByNameAsync(request.Role)).ReturnsAsync((RoleEntity?)null);
        _userRepositoryMock.Setup(repo => repo.BeginTransactionAsync()).ReturnsAsync(transactionMock.Object);

        // Act
        Func<Task> act = async () => await _authService.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Role {request.Role} not found.");
    }

    /// <summary>
    /// Test successful token refresh
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewToken_WhenValidRefreshTokenProvided()
    {
        // Arrange
        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            FullName = "John Doe",
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes("password123!"))),
            Role = new RoleEntity { Name = "Client" }
        };
        var refreshToken = new RefreshTokenEntity
        {
            User = user,
            UserId = user.Id,
            Token = "valid-refresh-token",
            ExpiryDate = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenRepositoryMock.Setup(repo => repo.GetByTokenAsync(refreshToken.Token)).ReturnsAsync(refreshToken);

        // Act
        var result = await _authService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Test failed token refresh when refresh token is invalid
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowInvalidOperationException_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        _refreshTokenRepositoryMock.Setup(repo => repo.GetByTokenAsync("invalid-refresh-token")).ReturnsAsync((RefreshTokenEntity?)null);

        // Act
        Func<Task> act = async () => await _authService.RefreshTokenAsync("invalid-refresh-token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid refresh token");
    }

    /// <summary>
    /// Test failed token refresh when user or role is missing
    /// </summary>
    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowInvalidOperationException_WhenUserOrRoleIsMissing()
    {
        // Arrange
        var refreshToken = new RefreshTokenEntity
        {
            UserId = Guid.Empty,
            User = null!, // User is missing
            Token = "valid-refresh-token",
            ExpiryDate = DateTime.UtcNow.AddDays(1)
        };

        _refreshTokenRepositoryMock.Setup(repo => repo.GetByTokenAsync(refreshToken.Token)).ReturnsAsync(refreshToken);

        // Act
        Func<Task> act = async () => await _authService.RefreshTokenAsync(refreshToken.Token);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User or role not found");
    }

    /// <summary>
    /// Test getting user ID from token
    /// </summary>
    [Fact]
    public void GetUserIdFromToken_ShouldReturnUserId_WhenTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        // Act
        var result = _authService.GetUserIdFromToken(claims);

        // Assert
        result.Should().Be(userId);
    }

    /// <summary>
    /// Test JWT validation
    /// </summary>
    [Fact]
    public void ValidateJwtToken_ShouldReturnClaimsPrincipal_WhenTokenIsValid()
    {
        // Arrange
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            new[] { new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) },
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
                SecurityAlgorithms.HmacSha256)
        ));

        // Act
        var result = _authService.ValidateJwtToken(token);

        // Assert
        result.Should().NotBeNull();
    }
}
