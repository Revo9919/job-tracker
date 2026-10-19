using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Api.Common;
using JobTracker.Api.Extensions;
using JobTracker.Business.DTOs.Profile;
using JobTracker.Business.Services.Interfaces;

namespace JobTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cv-profile")]
public class CvProfileController : ControllerBase
{
    private readonly ICvProfileService _cvProfileService;

    public CvProfileController(ICvProfileService cvProfileService)
    {
        _cvProfileService = cvProfileService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.GetCurrentUserId();
        var profile = await _cvProfileService.GetProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "CV Profile not found. Please create one."
            });
        }

        return Ok(new ApiResponse<CvProfileDto>
        {
            Success = true,
            Data = profile
        });
    }

    [HttpPut]
    public async Task<IActionResult> SaveProfile([FromBody] UpdateCvProfileDto request)
    {
        var userId = User.GetCurrentUserId();
        var result = await _cvProfileService.UpsertProfileAsync(userId, request);

        return Ok(new ApiResponse<CvProfileDto>
        {
            Success = true,
            Data = result,
            Message = "CV Profile saved successfully."
        });
    }
}