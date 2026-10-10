using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Models.Dtos;
using PortfolioApi.Models.Entities;
using PortfolioApi.Repositories;

namespace PortfolioApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class AdminsController : ControllerBase
    {
        private readonly IAdminRepository _admins;
        private readonly IPasswordHasher<PortfolioApi.Models.Entities.Admin> _passwordHasher;

        public AdminsController(IAdminRepository admins, IPasswordHasher<PortfolioApi.Models.Entities.Admin> passwordHasher)
        {
            _admins = admins;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _admins.GetAllAsync();
            return Ok(list.Select(a => new { a.Id, a.Email, a.Name, a.Role, a.IsActive, a.LastLogin, a.CreatedAt }));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrEmpty(dto.Password) || dto.Password.Length < 8) return BadRequest(new { message = "Password must be at least 8 characters" });

            var existing = await _admins.FindByEmailAsync(dto.Email);
            if (existing != null) return BadRequest(new { message = "An admin with that email already exists" });

            var admin = new PortfolioApi.Models.Entities.Admin { Id = Guid.NewGuid(), Email = dto.Email, Name = dto.Name, Role = dto.Role ?? "admin", IsActive = true, CreatedAt = DateTime.UtcNow };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, dto.Password);
            var created = await _admins.CreateAsync(admin);
            return CreatedAtAction(null, new { id = created.Id }, new { id = created.Id, email = created.Email, name = created.Name, role = created.Role });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] AdminUpdateDto dto)
        {
            var admin = await _admins.GetByIdAsync(id);
            if (admin == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Name)) admin.Name = dto.Name;
            if (!string.IsNullOrEmpty(dto.Role)) admin.Role = dto.Role;
            if (dto.IsActive.HasValue) admin.IsActive = dto.IsActive.Value;
            if (!string.IsNullOrEmpty(dto.Password))
            {
                if (dto.Password.Length < 8) return BadRequest(new { message = "Password must be at least 8 characters" });
                admin.PasswordHash = _passwordHasher.HashPassword(admin, dto.Password);
            }

            admin.UpdatedAt = DateTime.UtcNow;
            var updated = await _admins.UpdateAsync(admin);
            return Ok(new { id = updated?.Id, email = updated?.Email, name = updated?.Name, role = updated?.Role, isActive = updated?.IsActive });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _admins.DeleteAsync(id);
            if (!ok) return NotFound();
            return Ok(new { message = "Admin deleted" });
        }
    }
}
