using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet("live")]
        public ActionResult GetLiveness()
        {
            // Liveness probe: process is running
            return Ok(new { status = "live" });
        }

        [HttpGet("ready")]
        public async Task<ActionResult<Models.Dtos.HealthResponseDto>> GetReadiness()
        {
            // Readiness: perform a lightweight DB connectivity check without modifying data
            var dbHealthy = true;
            var message = "OK";
            try
            {
                // Resolve the DbContext from DI if available
                var db = HttpContext.RequestServices.GetService<PortfolioApi.Data.ApplicationDbContext>();
                if (db != null)
                {
                    // Use a simple, fast operation: check database can be connected to
                    dbHealthy = await db.Database.CanConnectAsync();
                    message = dbHealthy ? "DB connected" : "DB unavailable";
                }
                else
                {
                    message = "No DB configured";
                }
            }
            catch
            {
                dbHealthy = false;
                message = "DB check failed";
            }

            var dto = new Models.Dtos.HealthResponseDto
            {
                Status = dbHealthy ? "OK" : "Unhealthy",
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            if (!dbHealthy) return StatusCode(503, dto);
            return Ok(dto);
        }
    }
}
