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
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IContactRepository _contacts;
        private readonly IConfiguration _config;

        public AdminController(IContactRepository contacts, IConfiguration config)
        {
            _contacts = contacts;
            _config = config;
        }

        [HttpGet("export")]
        public async Task<IActionResult> Export()
        {
            var contacts = await _contacts.GetAllAsync();

            // Portfolio not yet implemented in SQL; return null placeholder
            var exportData = new
            {
                portfolio = (object?)null,
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

            // If contacts provided
            if (dto.Contacts != null)
            {
                var contactEntities = dto.Contacts.Select(c => new Contact
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
                    // Append: add each contact
                    foreach (var c in contactEntities)
                    {
                        await _contacts.AddAsync(c);
                    }
                }
            }

            // Portfolio import not supported yet in SQL migration; ignore dto.Portfolio

            return Ok(new { message = "Import completed" });
        }

        [HttpPost("backup")]
        public async Task<IActionResult> Backup()
        {
            var contacts = await _contacts.GetAllAsync();
            var exportData = new
            {
                portfolio = (object?)null,
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

            await System.IO.File.WriteAllTextAsync(path, json);
            var fi = new FileInfo(path);

            return Ok(new {
                message = "Backup created",
                filename = filename,
                size = fi.Length,
                createdAt = fi.CreationTimeUtc
            });
        }
    }
}
