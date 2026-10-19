using JobTracker.Api.Common;
using JobTracker.DataAccess.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly JobTrackerDbContext _dbContext;

    public HealthController(JobTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        try
        {
            var databaseConnected = await _dbContext.Database.CanConnectAsync();
            var data = new
            {
                Status = databaseConnected ? "Healthy" : "Degraded",
                Database = databaseConnected ? "Connected" : "Unavailable"
            };

            if (!databaseConnected)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<object>
                {
                    Success = false,
                    Data = data,
                    Message = "API is running, but the database is unavailable."
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = data,
                Message = "API and database are healthy."
            });
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiResponse<object>
            {
                Success = false,
                Data = new
                {
                    Status = "Degraded",
                    Database = "Unavailable"
                },
                Message = "API is running, but the database health check failed."
            });
        }
    }
}
