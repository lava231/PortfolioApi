using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PortfolioApi.Models.Dtos;
using PortfolioApi.Models.Entities;
using PortfolioApi.Repositories;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IContactRepository _contacts;
        private readonly IConfiguration _config;
        private readonly PortfolioApi.Repositories.IPortfolioRepository _portfolioRepo;

        public AdminController(IContactRepository contacts, IConfiguration config, PortfolioApi.Repositories.IPortfolioRepository portfolioRepo)
        {
            _contacts = contacts;
            _config = config;
            _portfolioRepo = portfolioRepo;
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var contacts = await _contacts.GetAllAsync();
            var portfolio = await _portfolioRepo.GetPublicAsync();

            var exportData = new
            {
                portfolio = portfolio,
                contacts = contacts.Select(c => new {
                    id = c.Id,
                    name = c.Name,
                    email = c.Email,
                    message = c.Message,
                    status = c.Status,
                    createdAt = c.CreatedAt
                }),
                exportedAt = DateTime.UtcNow,
                totalContacts = contacts.Count()
            };

            var bytes = JsonSerializer.SerializeToUtf8Bytes(exportData, new JsonSerializerOptions { WriteIndented = true });
            var fileName = $"portfolio-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json";

            return File(bytes, "application/json", fileName);
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] ImportRequestDto dto)
        {
            if (dto == null) return BadRequest(new { error = "Invalid payload" });

            if (dto.Contacts == null) return BadRequest(new { error = "Contacts array is required" });

            var contactsList = dto.Contacts.ToList();
            if (contactsList.Count == 0) return BadRequest(new { error = "Contacts array is empty" });
            if (contactsList.Count > 1000) return BadRequest(new { error = "Import exceeds maximum of 1000 contacts" });

            // Validate all items first
            foreach (var c in contactsList)
            {
                if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Length < 2 || c.Name.Length > 50)
                    return BadRequest(new { error = "Contact name invalid" });
                if (string.IsNullOrWhiteSpace(c.Email)) return BadRequest(new { error = "Contact email invalid" });
                if (string.IsNullOrWhiteSpace(c.Message) || c.Message.Length < 10 || c.Message.Length > 1000)
                    return BadRequest(new { error = "Contact message invalid" });
                var status = string.IsNullOrEmpty(c.Status) ? ContactStatus.Unread : c.Status;
                if (status != ContactStatus.Unread && status != ContactStatus.Read && status != ContactStatus.Replied)
                    return BadRequest(new { error = "Unsupported contact status" });
            }

            // Transform to entities
            var contactEntities = contactsList.Select(c => new Contact
            {
                Name = c.Name,
                Email = c.Email,
                Message = c.Message,
                Status = string.IsNullOrEmpty(c.Status) ? ContactStatus.Unread : c.Status,
                CreatedAt = c.CreatedAt ?? DateTime.UtcNow
            }).ToList();

            if (dto.ReplaceContacts)
            {
                await _contacts.ReplaceContactsAsync(contactEntities);
            }
            else
            {
                // Batch insert where possible
                foreach (var c in contactEntities)
                {
                    await _contacts.AddAsync(c);
                }
            }

            return Ok(new { message = "Import completed", imported = contactEntities.Count });
        }

        [HttpPost("backup")]
        public async Task<IActionResult> Backup()
        {
            var contacts = await _contacts.GetAllAsync();
            var portfolio = await _portfolioRepo.GetPublicAsync();

            var exportData = new
            {
                portfolio = portfolio,
                contacts = contacts.Select(c => new {
                    id = c.Id,
                    name = c.Name,
                    email = c.Email,
                    message = c.Message,
                    status = c.Status,
                    createdAt = c.CreatedAt
                }),
                exportedAt = DateTime.UtcNow,
                totalContacts = contacts.Count()
            };

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });

            var backupsDir = _config["Backups:Directory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "backups");
            if (!Directory.Exists(backupsDir)) Directory.CreateDirectory(backupsDir);

            var filename = $"backup-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            var path = Path.Combine(backupsDir, filename);

            // Atomic write: write to temp file then move
            var tempPath = path + ".tmp";
            await System.IO.File.WriteAllTextAsync(tempPath, json);
            System.IO.File.Move(tempPath, path);
            var fi = new FileInfo(path);

            return Ok(new {
                message = "Backup created",
                filename = filename,
                size = fi.Length,
                createdAt = fi.LastWriteTimeUtc
            });
        }
    }
}
