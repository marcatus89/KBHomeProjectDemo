using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DoAnTotNghiep.Models.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace DoAnTotNghiep.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TokenController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _config;
        private readonly ILogger<TokenController> _logger;

        public TokenController(UserManager<IdentityUser> userManager, IConfiguration config, ILogger<TokenController> logger)
        {
            _userManager = userManager;
            _config = config;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // small delay to mitigate user enumeration timing attacks
                await Task.Delay(200);
                return Unauthorized();
            }

            // Check lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                return Forbid(); // 403 (or 423 Locked - but Forbid is generic)
            }

            // Optional: require confirmed email
            if (_userManager.Options.SignIn.RequireConfirmedEmail && !await _userManager.IsEmailConfirmedAsync(user))
            {
                return BadRequest(new { error = "EmailNotConfirmed" });
            }

            var ok = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!ok)
            {
                // Increment failed count
                await _userManager.AccessFailedAsync(user);
                return Unauthorized();
            }

            // Successful login -> reset access failed count
            await _userManager.ResetAccessFailedCountAsync(user);

            // Check for mustChangePassword claim (e.g. created by seeding)
            var claimsInStore = await _userManager.GetClaimsAsync(user);
            if (claimsInStore.Any(c => c.Type == "mustChangePassword" && c.Value == "true"))
            {
                return Ok(new { mustChangePassword = true, message = "Password change required." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            var jwtSection = _config.GetSection("Jwt");
            var keyString = jwtSection["Key"];
            if (string.IsNullOrWhiteSpace(keyString))
            {
                _logger.LogError("Jwt:Key is not configured.");
                throw new InvalidOperationException("Jwt:Key is not configured.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(keyString);
            var key = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            double expiryMinutes = 60;
            if (double.TryParse(jwtSection["ExpiryMinutes"], out var parsed)) expiryMinutes = parsed;

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        /// <summary>
        /// Change password for authenticated user (requires current password).
        /// Also removes mustChangePassword claim if present.
        /// </summary>
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
            }

            var claim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "mustChangePassword");
            if (claim != null)
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            return Ok(new { success = true });
        }
    }
}
