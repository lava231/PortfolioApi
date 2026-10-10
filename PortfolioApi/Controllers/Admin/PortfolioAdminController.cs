using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioApi.Models.Dtos;
using PortfolioApi.Repositories;

namespace PortfolioApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Policy = "SuperAdminOnly")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioRepository _repo;

        public PortfolioController(IPortfolioRepository repo)
        {
            _repo = repo;
        }

        [HttpPut]
        public async Task<IActionResult> Upsert([FromBody] PortfolioUpsertDto dto)
        {
            var updated = await _repo.UpsertAsync(dto);
            return Ok(updated);
        }
    }
}
