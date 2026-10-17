using System.Threading.Tasks;
using JobTracker.Business.DTOs.Auth;

namespace JobTracker.Business.Services.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> SignupAsync(SignupDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}
