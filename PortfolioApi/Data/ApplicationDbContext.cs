using System;
using Microsoft.EntityFrameworkCore;
using PortfolioApi.Models.Entities;

namespace PortfolioApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<Portfolio> Portfolios => Set<Portfolio>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Admin configuration
            builder.Entity<Admin>(b =>
            {
                b.HasKey(a => a.Id);
                b.Property(a => a.Email).IsRequired().HasMaxLength(256);
                b.HasIndex(a => a.Email).IsUnique();
                b.Property(a => a.Name).IsRequired().HasMaxLength(100);
                b.Property(a => a.PasswordHash).IsRequired().HasMaxLength(512);
                b.Property(a => a.Role).IsRequired().HasMaxLength(20).HasDefaultValue("admin");
                b.Property(a => a.IsActive).HasDefaultValue(true);
                b.Property(a => a.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            // Portfolio configuration
            builder.Entity<Portfolio>(b =>
            {
                b.HasKey(p => p.Id);
                b.Property(p => p.Title).HasMaxLength(256);
                b.Property(p => p.Subtitle).HasMaxLength(512);
                b.Property(p => p.Description).HasMaxLength(4000);
                b.Property(p => p.AboutTitle).HasMaxLength(256);
                b.Property(p => p.AboutBody).HasMaxLength(4000);
                b.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                b.HasMany(p => p.Skills).WithOne().HasForeignKey(s => s.PortfolioId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(p => p.Projects).WithOne().HasForeignKey(pr => pr.PortfolioId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(p => p.ContactInfo).WithOne().HasForeignKey(c => c.PortfolioId).OnDelete(DeleteBehavior.Cascade);
                b.HasMany(p => p.FooterAdditionalLinks).WithOne().HasForeignKey(f => f.PortfolioId).OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<PortfolioSkill>(b => { b.HasKey(s => s.Id); b.Property(s => s.Name).IsRequired().HasMaxLength(200); });
            builder.Entity<Project>(b => { b.HasKey(p => p.Id); b.Property(p => p.Title).IsRequired().HasMaxLength(256); b.Property(p => p.Image).HasMaxLength(512); });
            builder.Entity<ProjectTechnology>(b => { b.HasKey(t => t.Id); b.Property(t => t.Name).IsRequired().HasMaxLength(128); });
            builder.Entity<PortfolioContactInfo>(b => { b.HasKey(c => c.Id); b.Property(c => c.Label).IsRequired().HasMaxLength(128); b.Property(c => c.Value).IsRequired().HasMaxLength(512); });
            builder.Entity<FooterAdditionalLink>(b => { b.HasKey(f => f.Id); b.Property(f => f.Label).IsRequired().HasMaxLength(128); b.Property(f => f.Url).IsRequired().HasMaxLength(512); });

            // Contact configuration
            builder.Entity<Contact>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Name).IsRequired().HasMaxLength(50);
                b.Property(c => c.Email).IsRequired().HasMaxLength(320);
                b.Property(c => c.Message).IsRequired().HasMaxLength(1000);
                b.Property(c => c.Status).IsRequired().HasMaxLength(16).HasDefaultValue(ContactStatus.Unread);
                b.Property(c => c.IpAddress).HasMaxLength(45);
                b.Property(c => c.UserAgent).HasMaxLength(512);
                b.Property(c => c.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                b.HasIndex(c => c.Status);
                b.HasIndex(c => c.CreatedAt);
            });
        }
    }
}
