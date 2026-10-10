using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var admin = await _admins.FindByEmailAsync(dto.Email);
            if (admin == null) return Unauthorized(new { message = "Invalid credentials" });
            if (!admin.IsActive) return Unauthorized(new { message = "Account is deactivated" });

            var verify = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.Password);
            if (verify == PasswordVerificationResult.Failed) return Unauthorized(new { message = "Invalid credentials" });

            admin.LastLogin = DateTime.UtcNow;
            await _admins.UpdateAsync(admin);

            var token = _jwt.GenerateToken(admin.Id, admin.Role);

            var response = new LoginResponseDto
            {
                Token = token,
                Id = admin.Id,
                Name = admin.Name,
                Email = admin.Email,
                Role = admin.Role,
                LastLogin = admin.LastLogin
            };

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
            if (admin == null) return Unauthorized();

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
