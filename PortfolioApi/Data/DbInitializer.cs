using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using PortfolioApi.Models.Entities;
using PortfolioApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace PortfolioApi.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var logger = services.GetService<ILoggerFactory>()?.CreateLogger("DbInitializer");

            try
            {
                var db = services.GetRequiredService<ApplicationDbContext>();
                // Apply migrations (use migrations instead of EnsureCreated)
                await db.Database.MigrateAsync();

                var adminRepo = services.GetRequiredService<IAdminRepository>();
                var passwordHasher = services.GetRequiredService<IPasswordHasher<Admin>>();
                var config = services.GetRequiredService<IConfiguration>();

                // Create default admin if none exists
                var admins = await adminRepo.GetAllAsync();
                if (admins == null || System.Linq.Enumerable.Count(admins) == 0)
                {
                    var adminEmail = config["ADMIN_EMAIL"] ?? config["Admin:Email"];
                    var adminPassword = config["ADMIN_PASSWORD"] ?? config["Admin:Password"];

                    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
                    {
                        var admin = new Admin
                        {
                            Email = adminEmail,
                            Name = config["Admin:Name"] ?? "Portfolio Admin",
                            Role = "super-admin",
                            IsActive = true
                        };

                        admin.PasswordHash = passwordHasher.HashPassword(admin, adminPassword);
                        await adminRepo.CreateAsync(admin);
                        logger?.LogInformation("Default super-admin created: {email}", adminEmail);
                    }
                    else
                    {
                        logger?.LogWarning("No ADMIN_EMAIL/ADMIN_PASSWORD configured; skipping default admin creation.");
                    }
                }

                // Create a minimal default portfolio if none exists
                var portfolioExists = await db.Set<PortfolioApi.Models.Entities.Portfolio>().AnyAsync();
                if (!portfolioExists)
                {
                    var portfolio = new PortfolioApi.Models.Entities.Portfolio
                    {
                        Id = System.Guid.NewGuid(),
                        Title = "Portfolio",
                        Subtitle = "A simple portfolio",
                        Description = "This is a seeded default portfolio.",
                        AboutTitle = "About",
                        AboutBody = "This portfolio was created by the initializer.",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var skill = new PortfolioApi.Models.Entities.PortfolioSkill { Id = System.Guid.NewGuid(), Name = "C#", Order = 1, PortfolioId = portfolio.Id };
                    var project = new PortfolioApi.Models.Entities.Project { Id = System.Guid.NewGuid(), Title = "Sample Project", Description = "A sample project", Order = 1, PortfolioId = portfolio.Id };
                    var tech = new PortfolioApi.Models.Entities.ProjectTechnology { Id = System.Guid.NewGuid(), Name = "ASP.NET Core", Order = 1, ProjectId = project.Id };
                    project.Technologies.Add(tech);
                    var contactInfo = new PortfolioApi.Models.Entities.PortfolioContactInfo { Id = System.Guid.NewGuid(), Label = "Email", Value = "info@example.com", Order = 1, PortfolioId = portfolio.Id };
                    var footerLink = new PortfolioApi.Models.Entities.FooterAdditionalLink { Id = System.Guid.NewGuid(), Label = "GitHub", Url = "https://github.com", Order = 1, PortfolioId = portfolio.Id };

                    portfolio.Skills.Add(skill);
                    portfolio.Projects.Add(project);
                    portfolio.ContactInfo.Add(contactInfo);
                    portfolio.FooterAdditionalLinks.Add(footerLink);

                    db.Set<PortfolioApi.Models.Entities.Portfolio>().Add(portfolio);
                    await db.SaveChangesAsync();
                    logger?.LogInformation("Seeded default portfolio.");
                }
            }
            catch (Exception ex)
            {
                var logger2 = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("DbInitializer");
                logger2?.LogError(ex, "An error occurred seeding the DB.");
                throw;
            }
        }
    }
}
