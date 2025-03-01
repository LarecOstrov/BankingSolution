using Banking.Application.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public AdminController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet("unconfirmed-users")]
        public async Task<IActionResult> GetUnconfirmedUsers()
        {
            return Ok(await _userRepository.GetUnconfirmedUsersAsync());
        }

        [HttpPost("confirm-user/{userId}")]
        public async Task<IActionResult> ConfirmUser(Guid userId)
        {
            await _userRepository.ConfirmUserAsync(userId);
            return Ok(new { message = "User confirmed successfully" });
        }
    }
}
