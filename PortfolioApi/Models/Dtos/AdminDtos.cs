using System;

namespace PortfolioApi.Models.Dtos
{
    public class AdminCreateDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Role { get; set; }
    }

    public class AdminUpdateDto
    {
        public string? Name { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public string? Password { get; set; }
    }
}
