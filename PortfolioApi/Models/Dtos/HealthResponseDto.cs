using System;

namespace PortfolioApi.Models.Dtos
{
    public class HealthResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
