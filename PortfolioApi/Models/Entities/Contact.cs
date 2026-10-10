using System;
namespace PortfolioApi.Models.Entities
{
    public static class ContactStatus
    {
        public const string Unread = "unread";
        public const string Read = "read";
        public const string Replied = "replied";
    }

    public class Contact
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = ContactStatus.Unread;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
