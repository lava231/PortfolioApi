using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Repositories;

namespace PortfolioApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioRepository _repo;

        public PortfolioController(IPortfolioRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var portfolio = await _repo.GetPublicAsync();
            if (portfolio == null) return NotFound(new { message = "Portfolio not found" });
            return Ok(portfolio);
        }
    }
}
