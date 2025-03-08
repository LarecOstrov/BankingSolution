using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [ApiController]
    [Route("api/balance-history")]
    [Authorize]
    public class BalanceHistoryController : ControllerBase
    {
        private readonly IBalanceHistoryRepository _balanceHistoryRepository;
        private readonly IAuthService _authService;
        public BalanceHistoryController(
            IBalanceHistoryRepository balanceHistoryRepository,
            IAuthService authService)
        {
            _balanceHistoryRepository = balanceHistoryRepository;
            _authService = authService;
        }
        /// <summary>
        /// Get balance history (only for authorized services)
        /// </summary>    
        [HttpGet("{accountId}")]
        public async Task<IActionResult> GetBalanceHistory(Guid accountId)
        {
            var userId = _authService.GetUserIdFromToken(User);
            if (userId == null)
            {
                return Unauthorized("User ID not found in token.");
            }
            var balanceHistory = await _balanceHistoryRepository.GetBalanceHistoryByAccountIdAsync(accountId, userId.Value);
            if (balanceHistory == null)
            {
                return NotFound("Balance history not found.");
            }
            return Ok(balanceHistory);
        }
    }
}
