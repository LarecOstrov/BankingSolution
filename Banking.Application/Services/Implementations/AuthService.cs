using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Banking.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountService _accountService;
    private readonly IAccountRepository _accountRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly string _jwtSecretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;
    private readonly JwtOptions _jwtOptions;

    public AuthService(IUserRepository userRepository,
        IAccountService accountService,
        IAccountRepository accountRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtOptions> jwtOptions)
    {
        _userRepository = userRepository;
        _accountService = accountService;
        _accountRepository = accountRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtOptions = jwtOptions.Value;
        _jwtSecretKey = _jwtOptions.SecretKey;
        _issuer = _jwtOptions.Issuer;
        _audience = _jwtOptions.Audience;
        _expiryMinutes = _jwtOptions.ExpiryMinutes;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid email or password");

        if (!user.Confirmed)
            throw new InvalidOperationException("User not confirmed");

        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };

        var result = await _refreshTokenRepository.AddAsync(refreshToken);
        if (result == null)
            throw new Exception("Failed to generate refresh token");

        var accessToken = GenerateJwtToken(user);

        return new LoginResponse(accessToken, refreshToken.Token);
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {

        if (await _userRepository.GetUserByEmailAsync(request.Email) != null)
            throw new InvalidOperationException("User with this email already exists.");

        using (var transaction = await _userRepository.BeginTransactionAsync())
        {

            bool confirmed = false;

            if (request.Role.Trim().ToLower() == "admin")
            {
                if (await _userRepository.CountAllAsync() == 0)
                {
                    await _roleRepository.AddAsync(new RoleEntity
                    {
                        Id = Guid.NewGuid(),
                        Name = "Admin"
                    });

                    confirmed = true;
                }
            }

            var role = await _roleRepository.GetRoleByNameAsync(request.Role);
            if (role == null)
            {
                Log.Error($"Role {request.Role} not found during registration.");
                transaction.Rollback();
                throw new InvalidOperationException($"Role {request.Role} not found.");
            }

            var user = new UserEntity
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                RoleId = role.Id,
                Confirmed = confirmed
            };
            var result = await _userRepository.AddAsync(user);

            if (result == null)
            {
                transaction.Rollback();
                throw new Exception("Failed to create user.");
            }

            var accountNumber = await _accountService.GenerateUniqueIBANAsync();
            if (accountNumber == null)
            {
                transaction.Rollback();
                throw new Exception("Failed to generate account number.");
            }

            var account = new AccountEntity
            {
                Id = Guid.NewGuid(),
                AccountNumber = accountNumber,
                UserId = result.Id,
            };

            var resultAccount = await _accountRepository.AddAsync(account);
            if (resultAccount == null)
            {
                transaction.Rollback();
                throw new Exception("Failed to create acount.");
            }

            transaction.Commit();
            return true;
        }
    }

    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity == null || tokenEntity.ExpiryDate < DateTime.UtcNow)
            throw new InvalidOperationException($"Invalid refresh token");

        if (tokenEntity.User == null || tokenEntity.User.Role == null)
            throw new InvalidOperationException($"User or role not found");

        return GenerateJwtToken(tokenEntity.User);
    }

    /// <summary>
    /// Get user ID from JWT token
    /// </summary>
    public Guid? GetUserIdFromToken(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    public ClaimsPrincipal? ValidateJwtToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
            return null;

        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _issuer,

            ValidateAudience = true,
            ValidAudience = _audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParams, out var validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }


    #region Private
    /// <summary>
    /// Generate JWT token
    /// </summary>
    /// <param name="user"></param>
    /// <returns>string token</returns>
    /// <exception cref="Exception"></exception>
    private string GenerateJwtToken(UserEntity user)
    {
        if (user.Role == null) throw new Exception("User role not found");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Hash password
    /// </summary>
    /// <param name="password"></param>
    /// <returns>string hash</returns>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    /// <summary>
    /// Verify password
    /// </summary>
    /// <param name="password"></param>
    /// <param name="storedHash"></param>
    /// <returns>Boolean indicates if password is valid</returns>
    private static bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
    #endregion
}
