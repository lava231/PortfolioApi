using System;
using System.ComponentModel.DataAnnotations;

namespace PortfolioApi.Models.Dtos
{
    public class ContactImportDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Message { get; set; } = string.Empty;
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
