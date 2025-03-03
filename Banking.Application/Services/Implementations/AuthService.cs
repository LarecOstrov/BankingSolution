using Azure.Core;
using Banking.Application.Repositories.Implementations;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
    private readonly SolutionOptions _solutionOptions;

    public AuthService(IUserRepository userRepository,
        IAccountService accountService,
        IAccountRepository accountRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<SolutionOptions> solutionOptions)
    {
        _userRepository = userRepository;
        _accountService = accountService;
        _accountRepository = accountRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _solutionOptions = solutionOptions.Value;
        _jwtSecretKey = _solutionOptions.Jwt.SecretKey;
        _issuer = _solutionOptions.Jwt.Issuer;
        _audience = _solutionOptions.Jwt.Audience;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetUserByEmailAsync(request.Email);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            return null;

        if (!user.Confirmed)
            return null;

        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };

        var result = await _refreshTokenRepository.AddAsync(refreshToken);
        if (result == null) return null;

        var accessToken = GenerateJwtToken(user);

        return new LoginResponse(accessToken, refreshToken.Token);        
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {

        if (await _userRepository.GetUserByEmailAsync(request.Email) != null)
            return false;

        using (var transaction = await _userRepository.BeginTransactionAsync())
        {
            try
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
                    transaction.Rollback();
                    return false;
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
                    return false;
                }

                var account = new AccountEntity
                {
                    Id = Guid.NewGuid(),
                    AccountNumber = await _accountService.GenerateUniqueIBANAsync(),
                    UserId = result.Id,
                };

                var resultAccount = await _accountRepository.AddAsync(account);
                if (resultAccount == null)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                return false;
            }
        }
    }

    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity == null || tokenEntity.ExpiryDate < DateTime.UtcNow)
            return null;

        if (tokenEntity.User == null || tokenEntity.User.Role == null) return null;

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


    #region Private
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
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
    #endregion
}
