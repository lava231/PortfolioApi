using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PortfolioApi.Models.Entities;

namespace PortfolioApi.Repositories
{
    public interface IContactRepository
    {
        Task<Contact> AddAsync(Contact contact);
        Task<(IEnumerable<Contact> Items, int Total)> GetPagedAsync(int page, int limit, string? status);
        Task<Contact?> GetByIdAsync(Guid id);
        Task<Contact?> UpdateAsync(Contact contact);
        Task<bool> DeleteAsync(Guid id);
        Task<(int total,int unread,int read,int replied,int recent)> GetStatsOverviewAsync();
        Task<IEnumerable<Contact>> GetAllAsync();
        Task ReplaceContactsAsync(IEnumerable<Contact> contacts);
    }
}
