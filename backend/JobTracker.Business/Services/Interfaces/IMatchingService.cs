using System.Threading.Tasks;
using JobTracker.Business.DTOs.Matching;

namespace JobTracker.Business.Services.Interfaces
{
    public interface IMatchingService
    {
        Task<MatchResultDto> MatchAsync(int userId, MatchRequestDto request);
    }
}
