using System;
namespace PortfolioApi.Models.Entities
{
    public enum AdminRole { Admin, SuperAdmin }

    public class Admin
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "admin"; // 'admin' or 'super-admin'
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
