using System;
using System.Collections.Generic;

namespace PortfolioApi.Models.Entities
{
    public class Portfolio
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? Description { get; set; }

        public string? AboutTitle { get; set; }
        public string? AboutBody { get; set; }

        public List<PortfolioSkill> Skills { get; set; } = new();
        public List<Project> Projects { get; set; } = new();
        public List<PortfolioContactInfo> ContactInfo { get; set; } = new();
        public List<FooterAdditionalLink> FooterAdditionalLinks { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PortfolioSkill
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class Project
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Image { get; set; }
        public List<ProjectTechnology> Technologies { get; set; } = new();
        public int Order { get; set; }
    }

    public class ProjectTechnology
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class PortfolioContactInfo
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class FooterAdditionalLink
    {
        public Guid Id { get; set; }
        public Guid PortfolioId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
