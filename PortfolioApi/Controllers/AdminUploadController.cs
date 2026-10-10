using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using PortfolioApi.Services;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize]
    public class UploadController : ControllerBase
    {
        private readonly IFileStorage _storage;

        public UploadController(IFileStorage storage)
        {
            _storage = storage;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile image)
        {
            try
            {
                if (image == null) return BadRequest(new { error = "No file uploaded" });

                var result = await _storage.SaveFileAsync(image);

                var url = Url.Content(result.Url);

                return Ok(new { data = new { url, filename = result.FileName, originalName = result.OriginalName, size = result.Size } });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Upload failed" });
            }
        }

        [HttpDelete("{filename}")]
        public async Task<IActionResult> Delete(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return BadRequest(new { error = "Filename required" });
            var ok = await _storage.DeleteFileAsync(filename);
            if (!ok) return NotFound(new { error = "File not found" });
            return Ok(new { message = "File deleted successfully" });
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var files = await _storage.ListFilesAsync();
            return Ok(new { data = files });
        }
    }
}
