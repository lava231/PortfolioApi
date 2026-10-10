using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioApi.Models.Entities;

namespace PortfolioApi.Repositories
{
    public interface IAdminRepository
    {
        Task<Admin?> FindByEmailAsync(string email);
        Task<Admin?> GetByIdAsync(Guid id);
        Task<Admin> CreateAsync(Admin admin);
        Task<IEnumerable<Admin>> GetAllAsync();
        Task<Admin?> UpdateAsync(Admin admin);
        Task<bool> DeleteAsync(Guid id);
    }
}
