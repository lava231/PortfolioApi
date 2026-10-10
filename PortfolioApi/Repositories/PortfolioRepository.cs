using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PortfolioApi.Data;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Repositories
{
    public class PortfolioRepository : IPortfolioRepository
    {
        private readonly ApplicationDbContext _db;

        public PortfolioRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PortfolioResponseDto?> GetPublicAsync()
        {
            var portfolio = await _db.Set<PortfolioApi.Models.Entities.Portfolio>()
                .Include(p => p.Skills)
                .Include(p => p.Projects).ThenInclude(pr => pr.Technologies)
                .Include(p => p.ContactInfo)
                .Include(p => p.FooterAdditionalLinks)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (portfolio == null) return null;

            var dto = new PortfolioResponseDto
            {
                Title = portfolio.Title,
                Subtitle = portfolio.Subtitle,
                Description = portfolio.Description,
                AboutTitle = portfolio.AboutTitle,
                AboutBody = portfolio.AboutBody,
                Skills = portfolio.Skills.OrderBy(s => s.Order).Select(s => new Models.Dtos.SkillDto { Name = s.Name, Order = s.Order }).ToList(),
                Projects = portfolio.Projects.OrderBy(p => p.Order).Select(p => new Models.Dtos.ProjectDto
                {
                    Title = p.Title,
                    Description = p.Description,
                    Url = p.Url,
                    Image = p.Image,
                    Order = p.Order,
                    Technologies = p.Technologies.OrderBy(t => t.Order).Select(t => t.Name).ToList()
                }).ToList(),
                ContactInfo = portfolio.ContactInfo.OrderBy(c => c.Order).Select(c => new Models.Dtos.ContactInfoDto { Label = c.Label, Value = c.Value, Order = c.Order }).ToList(),
                FooterAdditionalLinks = portfolio.FooterAdditionalLinks.OrderBy(f => f.Order).Select(f => new Models.Dtos.FooterLinkDto { Label = f.Label, Url = f.Url, Order = f.Order }).ToList()
            };

            return dto;
        }

        // Update or create the singleton portfolio
        public async Task<PortfolioResponseDto> UpsertAsync(PortfolioApi.Models.Dtos.PortfolioUpsertDto input)
        {
            var existing = await _db.Set<PortfolioApi.Models.Entities.Portfolio>().Include(p => p.Skills).Include(p => p.Projects).ThenInclude(pr => pr.Technologies).Include(p => p.ContactInfo).Include(p => p.FooterAdditionalLinks).FirstOrDefaultAsync();

            if (existing == null)
            {
                existing = new PortfolioApi.Models.Entities.Portfolio { Id = System.Guid.NewGuid(), CreatedAt = System.DateTime.UtcNow };
                _db.Set<PortfolioApi.Models.Entities.Portfolio>().Add(existing);
            }

            existing.Title = input.Title;
            existing.Subtitle = input.Subtitle;
            existing.Description = input.Description;
            existing.AboutTitle = input.AboutTitle;
            existing.AboutBody = input.AboutBody;
            existing.UpdatedAt = System.DateTime.UtcNow;

            // Replace simple collections for now
            existing.Skills.Clear();
            foreach (var s in input.Skills)
            {
                existing.Skills.Add(new PortfolioApi.Models.Entities.PortfolioSkill { Id = System.Guid.NewGuid(), Name = s.Name, Order = s.Order, PortfolioId = existing.Id });
            }

            existing.Projects.Clear();
            foreach (var p in input.Projects)
            {
                var proj = new PortfolioApi.Models.Entities.Project { Id = p.Id ?? System.Guid.NewGuid(), Title = p.Title, Description = p.Description, Url = p.Url, Image = p.Image, Order = p.Order, PortfolioId = existing.Id };
                foreach (var t in p.Technologies)
                {
                    proj.Technologies.Add(new PortfolioApi.Models.Entities.ProjectTechnology { Id = System.Guid.NewGuid(), Name = t, Order = 0, ProjectId = proj.Id });
                }
                existing.Projects.Add(proj);
            }

            existing.ContactInfo.Clear();
            foreach (var c in input.ContactInfo)
            {
                existing.ContactInfo.Add(new PortfolioApi.Models.Entities.PortfolioContactInfo { Id = System.Guid.NewGuid(), Label = c.Label, Value = c.Value, Order = c.Order, PortfolioId = existing.Id });
            }

            existing.FooterAdditionalLinks.Clear();
            foreach (var f in input.FooterAdditionalLinks)
            {
                existing.FooterAdditionalLinks.Add(new PortfolioApi.Models.Entities.FooterAdditionalLink { Id = System.Guid.NewGuid(), Label = f.Label, Url = f.Url, Order = f.Order, PortfolioId = existing.Id });
            }

            await _db.SaveChangesAsync();

            // Return the updated DTO
            return await GetPublicAsync() ?? new PortfolioResponseDto();
        }
    }
}
