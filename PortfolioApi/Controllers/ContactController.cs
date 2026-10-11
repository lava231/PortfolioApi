using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("ContactPolicy")]
        public async Task<IActionResult> Create([FromBody] ContactCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Apply server-side limits to field lengths (defensive)
            if (dto.Name.Length > 50 || dto.Message.Length > 1000) return BadRequest(new { message = "Payload too large" });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.ContainsKey("User-Agent") ? Request.Headers["User-Agent"].ToString() : null;

            var contact = new Contact
            {
                Name = dto.Name,
                Email = dto.Email,
                Message = dto.Message,
                IpAddress = ip,
                UserAgent = ua
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
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? status = null)
        {
            if (page < 1) return BadRequest(new { message = "page must be >= 1" });
            if (limit < 1 || limit > 100) return BadRequest(new { message = "limit must be between 1 and 100" });

            var (items, total) = await _repo.GetPagedAsync(page, limit, status);
            items = items ?? System.Linq.Enumerable.Empty<Models.Entities.Contact>();

            var pages = (total + limit - 1) / limit; // integer-safe ceiling

            return Ok(new {
                data = items.Select(c => new { id = c.Id, name = c.Name, email = c.Email, message = c.Message, status = c.Status, createdAt = c.CreatedAt }),
                pagination = new {
                    current = page,
                    pages = pages,
                    total,
                    hasNext = page < pages,
                    hasPrev = page > 1
                }
            });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var contact = await _repo.GetByIdAsync(id);
            if (contact == null) return NotFound(new { message = "Contact submission not found" });
            return Ok(new { data = contact });
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] Models.Dtos.ContactStatusUpdateDto body)
        {
            if (body == null || string.IsNullOrEmpty(body.Status)) return BadRequest(new { message = "Status is required" });

            var status = body.Status.Trim().ToLowerInvariant();
            if (status != Models.Entities.ContactStatus.Unread && status != Models.Entities.ContactStatus.Read && status != Models.Entities.ContactStatus.Replied)
                return BadRequest(new { message = "Unsupported status" });

            var contact = await _repo.GetByIdAsync(id);
            if (contact == null) return NotFound(new { message = "Contact submission not found" });

            contact.Status = status;
            var updated = await _repo.UpdateAsync(contact);
            if (updated == null) return NotFound(new { message = "Contact submission not found" });

            return Ok(new { data = new { id = updated.Id, status = updated.Status, updatedAt = updated.UpdatedAt } });
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _repo.DeleteAsync(id);
            if (!ok) return NotFound(new { message = "Contact submission not found" });
            return Ok(new { message = "Contact submission deleted successfully" });
        }

        [HttpGet("stats")]
        [Authorize(Policy = "SuperAdminOnly")]
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
