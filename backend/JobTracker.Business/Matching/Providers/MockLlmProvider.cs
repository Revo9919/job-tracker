using System.Collections.Generic;
using System.Threading.Tasks;
using JobTracker.Business.DTOs.Matching;
using JobTracker.Business.Matching.Providers;

namespace JobTracker.Business.Matching.Providers
{
    public class MockLlmProvider : ILlmProvider
    {
        public Task<MatchResultDto> MatchAsync(string jobDescription, string cvSkillsText)
        {
            var result = new MatchResultDto
            {
                MatchScore = 78,
                MatchingSkills = new List<string> { "C#", "REST APIs", "SQL" },
                MissingSkills = new List<string> { "Docker", "Azure" },
                Suggestions = new List<string> { "Mention REST API project in experience section." }
            };

            return Task.FromResult(result);
        }
    }
}
