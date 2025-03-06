using Banking.Application.Implementations;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthService _authService;
    public AccountController(
        IAccountRepository accountRepository,
        IAuthService authService)
    {
        _accountRepository = accountRepository;
        _authService = authService;
    }

    /// <summary>
    /// Get account details (only for authorized services)
    /// </summary>    
    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetAccountDetails(Guid accountId)
    {
        try
        {
            var userId = _authService.GetUserIdFromToken(User);
            if (userId == null)
            {
                return Unauthorized("User ID not found in token.");
            }

            var account = await _accountRepository.GetByAccountNumberAsync(accountId, userId.Value);
            if (account == null)
            {
                return NotFound("Account not found.");
            }

            return Ok(account);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }    
}
