using KarttaBackEnd2.Server.DTOs;
using KarttaBackEnd2.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KarttaBackEnd2.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UserController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;

            _signInManager = signInManager;
            _configuration = configuration;
        }

        // Käyttäjän rekisteröinti
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Email,
                Email = model.Email,
                IsApproved = false  // Ei hyväksytty oletuksena
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Lisää käyttäjä oletusrooliin
                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return BadRequest(ModelState);
                }

                // Luo sähköpostivahvistuslinkki
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "User", new { userId = user.Id, token = token }, Request.Scheme);

                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{callbackUrl}'>clicking here</a>.");

                // Lähetetään ilmoitus adminille
                var adminEmail = _configuration["Admin:Email"]; // Adminin sähköpostiosoite konfiguraatiosta
                await _emailSender.SendEmailAsync(adminEmail, "New User Registration",
                    $"A new user has registered with email: {model.Email}. Please review and approve their account.");

                return Ok("Registration successful. Please check your email to confirm.");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                return Unauthorized("Invalid credentials");
            }

            if (!user.EmailConfirmed)
            {
                return BadRequest("Email not confirmed");
            }

            if (!user.IsApproved)
            {
                return BadRequest("Admin has not approved your account yet");
            }


            // Generoi JWT-token
            var token = GenerateJwtToken(user);

            // Palauta token
            return Ok(new { token });
        }

        // GET: api/user/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDTO
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return Ok(userDto);
        }


        // Sähköpostin vahvistus
        [HttpGet("confirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return BadRequest("Invalid user");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Ok("Email confirmed. Please wait for admin approval.");
            }

            return BadRequest("Error confirming email");
        }

        [HttpGet("pendingApprovals")]
        public async Task<IActionResult> GetPendingApprovals()
        {
            var users = await _userManager.Users
                .Where(u => u.EmailConfirmed && !u.IsApproved)
                .ToListAsync();

            return Ok(users);
        }

        // Admin hyväksyy käyttäjän
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

        [Authorize]
        [HttpGet("getUserIdByEmail")]
        public async Task<IActionResult> GetUserIdByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(new { userId = user.Id });
        }
        // GET: api/user/roles
        [Authorize]
        [HttpGet("roles")]
        public async Task<IActionResult> GetUserRoles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User ID not found in token");
            }

            var user = await _userManager.FindByEmailAsync(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { roles });
        }
        // Generoi JWT token
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

