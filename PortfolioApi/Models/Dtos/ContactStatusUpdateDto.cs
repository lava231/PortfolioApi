using System.ComponentModel.DataAnnotations;

namespace PortfolioApi.Models.Dtos
{
    public class ContactStatusUpdateDto
    {
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
