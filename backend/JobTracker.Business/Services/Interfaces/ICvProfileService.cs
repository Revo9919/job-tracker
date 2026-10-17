using System.Threading.Tasks;
using JobTracker.Business.DTOs.Profile;

namespace JobTracker.Business.Services.Interfaces
{
    public interface ICvProfileService
    {
        Task<CvProfileDto?> GetProfileAsync(int userId);
        Task<CvProfileDto> UpsertProfileAsync(int userId, UpdateCvProfileDto dto);
    }
}
