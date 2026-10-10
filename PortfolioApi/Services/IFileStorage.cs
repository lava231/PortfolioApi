using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Services
{
    public interface IFileStorage
    {
        Task<FileUploadResultDto> SaveFileAsync(IFormFile file);
        Task<bool> DeleteFileAsync(string filename);
        Task<IEnumerable<UploadedFileInfoDto>> ListFilesAsync();
    }
}
