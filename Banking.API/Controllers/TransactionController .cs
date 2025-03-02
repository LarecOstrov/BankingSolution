using Banking.Application.Services.Interfaces;
using Banking.Domain.Enums;
using Banking.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    public class TransactionController : ControllerBase
    {
        private readonly IPublishService _publishService;
        private readonly IAccountService _accountService;
        private readonly IAuthService _authService;

        public TransactionController(
            IPublishService publishService,
            IAuthService authService,
            IAccountService accountService)
        {
            _publishService = publishService;
            _authService = authService;
            _accountService = accountService;
        }

        /// <summary>
        /// Deposit funds (only for authorized services)
        /// </summary>
        [HttpPost("deposit")]
        [Authorize(Roles = "PaymentService, Admin")] 
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            try
            {
                return Accepted(await _publishService.PublishTransactionAsync(new Transaction(
                    Guid.NewGuid(),
                    null,
                    request.ToAccountId,
                    request.Amount,
                    DateTime.UtcNow,
                    TransactionStatus.Pending)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        /// <summary>
        /// Withdraw funds (only for authorized services)
        /// </summary>
        [HttpPost("withdraw")]
        [Authorize(Roles = "PaymentService, Admin")]
        public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
        {
            try
            {
                return Accepted(await _publishService.PublishTransactionAsync(new Transaction(
                    Guid.NewGuid(),
                    request.FromAccountId,
                    null,
                    request.Amount,
                    DateTime.UtcNow,
                    TransactionStatus.Pending)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Transfer funds
        /// </summary>
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            try
            {
                var userId = _authService.GetUserIdFromToken(User);
                if (userId == null)
                {
                    return Unauthorized("User ID not found in token.");
                }

                var isOwner = await _accountService.IsAccountOwnerAsync(request.FromAccountId, userId.Value);
                if (!isOwner)
                {
                    return Forbid("You can only transfer funds from your own account.");
                }

                return Accepted(await _publishService.PublishTransactionAsync(new Transaction(
                    Guid.NewGuid(),
                    request.FromAccountId,
                    request.ToAccountId,
                    request.Amount,
                    DateTime.UtcNow,
                    TransactionStatus.Pending)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
