using System;
using System.Threading.Tasks;
using JobTracker.Business.DTOs.Profile;
using JobTracker.Business.Services.Interfaces;
using JobTracker.DataAccess.Repositories.Interfaces;
using JobTracker.DataAccess.UnitOfWork;
using JobTracker.Domain;

namespace JobTracker.Business.Services.Implementations
{
    public class CvProfileService : ICvProfileService
    {
        private readonly ICvProfileRepository _cvProfileRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CvProfileService(ICvProfileRepository cvProfileRepository, IUnitOfWork unitOfWork)
        {
            _cvProfileRepository = cvProfileRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CvProfileDto?> GetProfileAsync(int userId)
        {
            var profile = await _cvProfileRepository.GetByUserIdAsync(userId);
            if (profile == null)
            {
                return null;
            }

            var dto = new CvProfileDto
            {
                Id = profile.Id,
                Name = profile.Name,
                Summary = profile.Summary,
                SkillsText = profile.SkillsText,
                ExperienceText = profile.ExperienceText,
                EducationText = profile.EducationText
            };

            return dto;
        }

        public async Task<CvProfileDto> UpsertProfileAsync(int userId, UpdateCvProfileDto dto)
        {
            var profile = await _cvProfileRepository.GetByUserIdAsync(userId);

            if (profile == null)
            {
                profile = new CvProfile
                {
                    UserId = userId,
                    Name = dto.Name,
                    Summary = dto.Summary,
                    SkillsText = dto.SkillsText,
                    ExperienceText = dto.ExperienceText,
                    EducationText = dto.EducationText,
                    UpdatedAt = DateTime.UtcNow
                };
                await _cvProfileRepository.AddAsync(profile);
            }
            else
            {
                profile.Name = dto.Name;
                profile.Summary = dto.Summary;
                profile.SkillsText = dto.SkillsText;
                profile.ExperienceText = dto.ExperienceText;
                profile.EducationText = dto.EducationText;
                profile.UpdatedAt = DateTime.UtcNow;

                _cvProfileRepository.Update(profile);
            }

            await _unitOfWork.CompleteAsync();

            var resultDto = new CvProfileDto
            {
                Id = profile.Id,
                Name = profile.Name,
                Summary = profile.Summary,
                SkillsText = profile.SkillsText,
                ExperienceText = profile.ExperienceText,
                EducationText = profile.EducationText
            };

            return resultDto;
        }
    }
}