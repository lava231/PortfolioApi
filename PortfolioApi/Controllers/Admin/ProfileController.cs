using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Models.Dtos;
using PortfolioApi.Repositories;

namespace PortfolioApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IAdminRepository _admins;
        private readonly IPasswordHasher<PortfolioApi.Models.Entities.Admin> _passwordHasher;

        public ProfileController(IAdminRepository admins, IPasswordHasher<PortfolioApi.Models.Entities.Admin> passwordHasher)
        {
            _admins = admins;
            _passwordHasher = passwordHasher;
        }

        private Guid? CurrentAdminId()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(id, out var g)) return g;
            return null;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var id = CurrentAdminId();
            if (id == null) return Unauthorized();
            var admin = await _admins.GetByIdAsync(id.Value);
            if (admin == null) return Unauthorized();
            return Ok(new { id = admin.Id, name = admin.Name, email = admin.Email, role = admin.Role, isActive = admin.IsActive, lastLogin = admin.LastLogin });
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDto dto)
        {
            var id = CurrentAdminId();
            if (id == null) return Unauthorized();
            var admin = await _admins.GetByIdAsync(id.Value);
            if (admin == null) return Unauthorized();

            admin.Name = dto.Name;
            admin.UpdatedAt = DateTime.UtcNow;
            var updated = await _admins.UpdateAsync(admin);
            return Ok(new { id = updated?.Id, name = updated?.Name, email = updated?.Email });
        }

        [HttpPost("password")]
        public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeDto dto)
        {
            var id = CurrentAdminId();
            if (id == null) return Unauthorized();
            var admin = await _admins.GetByIdAsync(id.Value);
            if (admin == null) return Unauthorized();

            var verify = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, dto.CurrentPassword);
            if (verify == PasswordVerificationResult.Failed) return BadRequest(new { message = "Current password is incorrect" });

            admin.PasswordHash = _passwordHasher.HashPassword(admin, dto.NewPassword);
            admin.UpdatedAt = DateTime.UtcNow;
            await _admins.UpdateAsync(admin);
            return Ok(new { message = "Password changed" });
        }
    }
}
