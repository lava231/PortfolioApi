using System.Threading.Tasks;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Repositories
{
    public interface IPortfolioRepository
    {
        Task<PortfolioResponseDto?> GetPublicAsync();
        Task<PortfolioResponseDto> UpsertAsync(PortfolioApi.Models.Dtos.PortfolioUpsertDto input);
    }
}
