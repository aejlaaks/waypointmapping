using KarttaBackEnd2.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KarttaBackEnd2.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("approveUser")]
        public async Task<IActionResult> ApproveUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found");

            if (user.EmailConfirmed && !user.IsApproved)
            {
                user.IsApproved = true; // Hyväksy tunnus
                await _userManager.UpdateAsync(user);
                return Ok("User approved");
            }

            return BadRequest("User is either not confirmed or already approved");
        }

        [HttpGet("pendingApprovals")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var users = await _userManager.Users
                .Where(u => u.EmailConfirmed && !u.IsApproved)
                .ToListAsync();

            return Ok(users);
        }
    }

}
