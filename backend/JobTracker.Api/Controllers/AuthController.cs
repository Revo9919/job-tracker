using Microsoft.AspNetCore.Mvc;
using JobTracker.Api.Common;
using JobTracker.Business.DTOs.Auth;
using JobTracker.Business.Services.Interfaces;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    public async Task<IActionResult> Signup([FromBody] SignupDto request)
    {
        var result = await _authService.SignupAsync(request);

        var response = new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Data = result,
            Message = "User registered successfully."
        };

        return Created(string.Empty, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var result = await _authService.LoginAsync(request);

        var response = new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Data = result,
            Message = "Login successful."
        };

        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Data = result,
            Message = "Token refreshed successfully."
        });
    }
}