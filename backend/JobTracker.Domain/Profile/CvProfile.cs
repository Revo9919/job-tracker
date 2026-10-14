using System;

public sealed class CvProfile : Entity
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string SkillsText { get; set; } = string.Empty;
    public string? ExperienceText { get; set; }
    public string? EducationText { get; set; }
    public DateTime? UpdatedAt { get; set; }
}