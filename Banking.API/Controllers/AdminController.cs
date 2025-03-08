using Banking.Application.Repositories.Interfaces;
using Banking.Domain.ValueObjects;
using Banking.Infrastructure.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Banking.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public AdminController(IUserRepository userRepository,
            IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }
        /// <summary>
        /// Get all unconfirmed users
        /// </summary>
        /// <returns>IActionResult with List Unconfirmed User</returns>
        [HttpGet("unconfirmed-users")]
        public async Task<IActionResult> GetUnconfirmedUsers()
        {
            return Ok(await _userRepository.GetUnconfirmedUsersAsync());
        }

        /// <summary>
        /// Confirm user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>IActionResult</returns>

        [HttpPost("confirm-user/{userId}")]
        public async Task<IActionResult> ConfirmUser(Guid userId)
        {
            await _userRepository.ConfirmUserAsync(userId);
            return Ok(new { message = "User confirmed successfully" });
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        /// <returns>IActionResult with List all roles</returns>
        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _roleRepository.GetAll().ToListAsync();
            return Ok(roles);
        }

        /// <summary>
        /// Assign role to user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="request"></param>
        /// <returns>IActionResult with object</returns>
        [HttpPost("assign-role/{userId}")]
        public async Task<IActionResult> AssignRole(Guid userId, [FromBody] AssignRoleRequest request)
        {
            var role = await _roleRepository.GetRoleByNameAsync(request.RoleName);
            if (role == null)
                return BadRequest(new { message = "Role not found" });

            var success = await _userRepository.AssignRoleAsync(userId, role.Id);
            if (!success)
                return NotFound(new { message = "User not found or role already assigned" });

            return Ok(new { message = $"Role '{request.RoleName}' assigned successfully" });
        }

        /// <summary>
        /// Create role
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns> IActionResult with object</returns>
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var role = await _roleRepository.GetRoleByNameAsync(roleName);
            if (role != null)
                return BadRequest(new { message = "Role already exists" });
            var newRole = new RoleEntity
            {
                Name = roleName
            };
            await _roleRepository.AddAsync(newRole);
            return Ok(new { message = "Role created successfully" });
        }
    }
}
