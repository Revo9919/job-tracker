using System.Threading.Tasks;
using JobTracker.Business.DTOs.Matching;

namespace JobTracker.Business.Matching.Providers
{
    public interface ILlmProvider
    {
        Task<MatchResultDto> MatchAsync(string jobDescription, string cvSkillsText);
    }
}
