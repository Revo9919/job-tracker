using System;

namespace JobTracker.Domain;

public sealed class MatchResult : Entity
{
    public int UserId { get; set; }
    public int? ApplicationId { get; set; }
    public string JobDescriptionText { get; set; } = string.Empty;
    public int MatchScore { get; set; }
    public string? MatchingSkills { get; set; }
    public string? MissingSkills { get; set; }
    public string? Suggestions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}