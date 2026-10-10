using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PortfolioApi.Data;
using PortfolioApi.Models.Entities;

namespace PortfolioApi.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _db;

        public AdminRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Admin> CreateAsync(Admin admin)
        {
            admin.Id = Guid.NewGuid();
            admin.CreatedAt = DateTime.UtcNow;
            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();
            return admin;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var admin = await _db.Admins.FindAsync(id);
            if (admin == null) return false;
            _db.Admins.Remove(admin);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Admin>> GetAllAsync()
        {
            return await _db.Admins.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        public async Task<Admin?> GetByIdAsync(Guid id)
        {
            return await _db.Admins.FindAsync(id);
        }

        public async Task<Admin?> FindByEmailAsync(string email)
        {
            return await _db.Admins.FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<Admin?> UpdateAsync(Admin admin)
        {
            admin.UpdatedAt = DateTime.UtcNow;
            _db.Admins.Update(admin);
            await _db.SaveChangesAsync();
            return admin;
        }
    }
}
