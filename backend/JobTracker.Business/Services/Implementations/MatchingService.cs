using System;
using System.Text.Json;
using System.Threading.Tasks;
using JobTracker.Business.DTOs.Matching;
using JobTracker.Business.Matching.Providers;
using JobTracker.Business.Services.Interfaces;
using JobTracker.DataAccess.UnitOfWork;
using JobTracker.Domain;

namespace JobTracker.Business.Services.Implementations
{
    public class MatchingService : IMatchingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILlmProvider _llmProvider;

        public MatchingService(IUnitOfWork unitOfWork, ILlmProvider llmProvider)
        {
            _unitOfWork = unitOfWork;
            _llmProvider = llmProvider;
        }

        public async Task<MatchResultDto> MatchAsync(int userId, MatchRequestDto request)
        {
            var cvProfile = await _unitOfWork.CvProfiles.GetByUserIdAsync(userId);
            if (cvProfile == null)
            {
                throw new InvalidOperationException("CV Profile not found for this user.");
            }

            if (string.IsNullOrWhiteSpace(cvProfile.SkillsText))
            {
                throw new InvalidOperationException("Skills text is required for matching.");
            }

            var matchResultDto = await _llmProvider.MatchAsync(request.JobDescription, cvProfile.SkillsText);

            var matchResult = new MatchResult
            {
                UserId = userId,
                ApplicationId = null,
                JobDescriptionText = request.JobDescription,
                MatchScore = matchResultDto.MatchScore,
                MatchingSkills = JsonSerializer.Serialize(matchResultDto.MatchingSkills),
                MissingSkills = JsonSerializer.Serialize(matchResultDto.MissingSkills),
                Suggestions = JsonSerializer.Serialize(matchResultDto.Suggestions),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.MatchResults.AddAsync(matchResult);
            await _unitOfWork.SaveChangesAsync();

            return matchResultDto;
        }
    }
}
