using System;
using System.Collections.Generic;

namespace PortfolioApi.Models.Dtos
{
    public class PortfolioResponseDto
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Description { get; set; }

        public string? AboutTitle { get; set; }
        public string? AboutBody { get; set; }

        public List<SkillDto> Skills { get; set; } = new();
        public List<ProjectDto> Projects { get; set; } = new();
        public List<ContactInfoDto> ContactInfo { get; set; } = new();
        public List<FooterLinkDto> FooterAdditionalLinks { get; set; } = new();
    }

    public class SkillDto { public string Name { get; set; } = string.Empty; public int Order { get; set; } }
    public class ProjectDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public List<string> Technologies { get; set; } = new();
        public int Order { get; set; }
    }

    public class ContactInfoDto { public string Label { get; set; } = string.Empty; public string Value { get; set; } = string.Empty; public int Order { get; set; } }
    public class FooterLinkDto { public string Label { get; set; } = string.Empty; public string Url { get; set; } = string.Empty; public int Order { get; set; } }
}
