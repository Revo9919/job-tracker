namespace JobTracker.Business.DTOs.Profile;

public class CvProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string SkillsText { get; set; } = string.Empty;
    public string? ExperienceText { get; set; }
    public string? EducationText { get; set; }
}
