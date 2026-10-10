using System;

namespace PortfolioApi.Models.Dtos
{
    public class ProfileUpdateDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class PasswordChangeDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
