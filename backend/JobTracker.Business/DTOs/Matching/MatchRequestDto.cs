namespace JobTracker.Business.DTOs.Matching;

public class MatchRequestDto
{
    public string JobDescription { get; set; } = string.Empty;
    public int CvProfileId { get; set; }
}
