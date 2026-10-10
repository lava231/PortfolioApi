using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PortfolioApi.Data;
using PortfolioApi.Models.Entities;

namespace PortfolioApi.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly ApplicationDbContext _db;

        public ContactRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Contact> AddAsync(Contact contact)
        {
            contact.Id = Guid.NewGuid();
            contact.CreatedAt = DateTime.UtcNow;
            _db.Contacts.Add(contact);
            await _db.SaveChangesAsync();
            return contact;
        }

        public async Task<(IEnumerable<Contact> Items, int Total)> GetPagedAsync(int page, int limit, string? status)
        {
            var query = _db.Contacts.AsQueryable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedAt)
                                   .Skip((page - 1) * limit)
                                   .Take(limit)
                                   .ToListAsync();

            return (items, total);
        }

        public async Task<Contact?> GetByIdAsync(Guid id)
        {
            return await _db.Contacts.FindAsync(id);
        }

        public async Task<Contact?> UpdateAsync(Contact contact)
        {
            contact.UpdatedAt = DateTime.UtcNow;
            _db.Contacts.Update(contact);
            await _db.SaveChangesAsync();
            return contact;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var contact = await _db.Contacts.FindAsync(id);
            if (contact == null) return false;
            _db.Contacts.Remove(contact);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<(int total,int unread,int read,int replied,int recent)> GetStatsOverviewAsync()
        {
            var total = await _db.Contacts.CountAsync();
            var unread = await _db.Contacts.CountAsync(c => c.Status == ContactStatus.Unread);
            var read = await _db.Contacts.CountAsync(c => c.Status == ContactStatus.Read);
            var replied = await _db.Contacts.CountAsync(c => c.Status == ContactStatus.Replied);

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var recent = await _db.Contacts.CountAsync(c => c.CreatedAt >= sevenDaysAgo);

            return (total, unread, read, replied, recent);
        }

        public async Task<IEnumerable<Contact>> GetAllAsync()
        {
            return await _db.Contacts.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task ReplaceContactsAsync(IEnumerable<Contact> contacts)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Contacts.RemoveRange(_db.Contacts);
                await _db.SaveChangesAsync();

                foreach (var c in contacts)
                {
                    c.Id = Guid.NewGuid();
                    c.CreatedAt = c.CreatedAt == default ? DateTime.UtcNow : c.CreatedAt;
                    _db.Contacts.Add(c);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
