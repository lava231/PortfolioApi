using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<Models.Dtos.HealthResponseDto>> Get()
        {
            // Minimal async wrapper to follow async conventions
            var dto = await Task.FromResult(new Models.Dtos.HealthResponseDto
            {
                Status = "OK",
                Message = "Portfolio backend is running",
                Timestamp = DateTime.UtcNow
            });

            return Ok(dto);
        }
    }
}
