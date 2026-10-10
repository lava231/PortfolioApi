using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Models.Dtos;
using PortfolioApi.Models.Entities;
using PortfolioApi.Repositories;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IContactRepository _repo;

        public ContactController(IContactRepository repo)
        {
            _repo = repo;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ContactCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var contact = new Contact
            {
                Name = dto.Name,
                Email = dto.Email,
                Message = dto.Message,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            var created = await _repo.AddAsync(contact);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new {
                id = created.Id,
                name = created.Name,
                email = created.Email,
                submittedAt = created.CreatedAt
            });
        }
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? status = null)
        {
            var (items, total) = await _repo.GetPagedAsync(page, limit, status);

            return Ok(new {
                data = items,
                pagination = new {
                    current = page,
                    pages = (int)Math.Ceiling((double)total / limit),
                    total,
                    hasNext = page * limit < total,
                    hasPrev = page > 1
                }
            });
        }

        [HttpGet("{id:guid}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var contact = await _repo.GetByIdAsync(id);
            if (contact == null) return NotFound(new { message = "Contact submission not found" });
            return Ok(new { data = contact });
        }

        [HttpPut("{id:guid}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] dynamic body)
        {
            string? status = body?.status;
            if (string.IsNullOrEmpty(status)) return BadRequest(new { message = "Status is required" });

            var contact = await _repo.GetByIdAsync(id);
            if (contact == null) return NotFound(new { message = "Contact submission not found" });

            contact.Status = status;
            var updated = await _repo.UpdateAsync(contact);
            return Ok(new { data = updated });
        }

        [HttpDelete("{id:guid}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _repo.DeleteAsync(id);
            if (!ok) return NotFound(new { message = "Contact submission not found" });
            return Ok(new { message = "Contact submission deleted successfully" });
        }

        [HttpGet("stats")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Stats()
        {
            var stats = await _repo.GetStatsOverviewAsync();
            return Ok(new {
                total = stats.total,
                unread = stats.unread,
                read = stats.read,
                replied = stats.replied,
                recent7Days = stats.recent
            });
        }
    }
}
