using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly IPublishService _publishService;
    private readonly IAccountService _accountService;
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ITransactionRepository _transactionRepository;

    public TransactionController(
        ITransactionRepository transactionRepository,
        IUserRepository userRepository,
        IPublishService publishService,
        IAuthService authService,
        IAccountService accountService)
    {
        _transactionRepository = transactionRepository;
        _publishService = publishService;
        _authService = authService;
        _accountService = accountService;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Deposit funds (only for authorized services)
    /// </summary>
    [HttpPost("deposit")]
    [Authorize(Roles = "PaymentService, Admin")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        return Accepted(await _publishService.PublishTransactionAsync(new Transaction(
            Guid.NewGuid(),
            null,
            request.ToAccountId,
            request.Amount,
            DateTime.UtcNow,
            TransactionStatus.Pending)));

    }

    /// <summary>
    /// Withdraw funds (only for authorized services)
    /// </summary>
    [HttpPost("withdraw")]
    [Authorize(Roles = "PaymentService, Admin")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
    {
        return Accepted(await _publishService.PublishTransactionAsync(new Transaction(
            Guid.NewGuid(),
            request.FromAccountId,
            null,
            request.Amount,
            DateTime.UtcNow,
            TransactionStatus.Pending)));
    }

    /// <summary>
    /// Transfer funds
    /// </summary>
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized("User ID not found in token.");
        }

        var user = await _userRepository.GetUserWithRoleById(userId.Value);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (user.Role?.Name != "Admin")
        {
            var isOwner = await _accountService.IsAccountOwnerAsync(request.FromAccountId, userId.Value);
            if (!isOwner)
            {
                return Forbid("You can only transfer funds from your own account.");
            }
        }

        return Accepted(await _publishService.PublishTransactionAsync(new Transaction(
            Guid.NewGuid(),
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            DateTime.UtcNow,
            TransactionStatus.Pending)));

    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        var userId = _authService.GetUserIdFromToken(User);
        if (userId == null)
        {
            return Unauthorized("User ID not found in token.");
        }

        var transaction = await _transactionRepository.GetByTransactionIdAsync(id, userId.Value);
        if (transaction == null)
        {
            return NotFound("Transaction not found.");
        }

        return Ok(transaction);
    }
}
