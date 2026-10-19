using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JobTracker.Api.Common;
using JobTracker.Api.Extensions;
using JobTracker.Business.DTOs.Matching;
using JobTracker.Business.Services.Interfaces;

namespace JobTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MatchingController : ControllerBase
{
    private readonly IMatchingService _matchingService;

    public MatchingController(IMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpPost]
    public async Task<IActionResult> Match([FromBody] MatchRequestDto request)
    {
        var userId = User.GetCurrentUserId();
        var result = await _matchingService.MatchAsync(userId, request);

        return Ok(new ApiResponse<MatchResultDto>
        {
            Success = true,
            Data = result,
            Message = "Matching completed successfully."
        });
    }
}