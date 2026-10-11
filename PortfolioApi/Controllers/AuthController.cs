using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PortfolioApi.Models.Dtos;
using PortfolioApi.Models.Entities;
using PortfolioApi.Repositories;
using PortfolioApi.Services;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAdminRepository _admins;
        private readonly IJwtService _jwt;
        private readonly IPasswordHasher<PortfolioApi.Models.Entities.Admin> _passwordHasher;

        public AuthController(IAdminRepository admins, IJwtService jwt, IPasswordHasher<PortfolioApi.Models.Entities.Admin> passwordHasher)
        {
            _admins = admins;
            _jwt = jwt;
            _passwordHasher = passwordHasher;
        }

        [HttpPost("login")]
        // No-op patch to trigger file update consistency.
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            // Basic server-side rate limiting using IMemoryCache to mitigate brute-force
            var cache = HttpContext.RequestServices.GetService<IMemoryCache>();
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var rateKey = $"login-attempt:{ip}";
            var attempts = 0;
            if (cache != null && cache.TryGetValue<int>(rateKey, out var existing)) attempts = existing;
            if (attempts >= 10) return StatusCode(429, new { message = "Too many login attempts. Try again later." });

            var admin = await _admins.FindByEmailAsync(dto.Email);
            var genericFail = Unauthorized(new { message = "Invalid credentials" });

            if (admin == null)
            {
                // increment attempts for unknown account too
                cache?.Set(rateKey, attempts + 1, TimeSpan.FromMinutes(5));
                return genericFail;
            }

            // Prevent inactive admins from authenticating but do not leak this to the client
            if (!admin.IsActive)
            {
                cache?.Set(rateKey, attempts + 1, TimeSpan.FromMinutes(5));
                return genericFail;
            }

            var verify = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                cache?.Set(rateKey, attempts + 1, TimeSpan.FromMinutes(5));
                return genericFail;
            }

            // Successful login: reset attempt counter
            cache?.Remove(rateKey);

            admin.LastLogin = DateTime.UtcNow;
            await _admins.UpdateAsync(admin);

            var token = _jwt.GenerateToken(admin.Id, admin.Role);

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var response = new LoginResponseDto
            {
                Token = token,
                Id = admin.Id,
                Name = admin.Name,
                Email = admin.Email,
                Role = admin.Role,
                LastLogin = admin.LastLogin
            };

            // Optionally include expiry if compatible with clients
            try { response.GetType().GetProperty("ExpiresAt")?.SetValue(response, jwtToken.ValidTo); } catch {}

            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim)) return Unauthorized();

            if (!Guid.TryParse(idClaim, out var id)) return Unauthorized();

            var admin = await _admins.GetByIdAsync(id);
            if (admin == null || !admin.IsActive) return Unauthorized();

            return Ok(new {
                id = admin.Id,
                name = admin.Name,
                email = admin.Email,
                role = admin.Role,
                lastLogin = admin.LastLogin,
                isActive = admin.IsActive,
                createdAt = admin.CreatedAt
            });
        }
    }
}
