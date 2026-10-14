using System.Collections.Generic;

namespace JobTracker.Business.DTOs.Matching;

public class MatchResultDto
{
    public int MatchScore { get; set; }
    public List<string> MatchingSkills { get; set; } = new List<string>();
    public List<string> MissingSkills { get; set; } = new List<string>();
    public List<string> Suggestions { get; set; } = new List<string>();
}
