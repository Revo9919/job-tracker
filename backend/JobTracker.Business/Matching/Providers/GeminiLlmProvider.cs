using System;
using System.Text.Json;
using System.Threading.Tasks;
using Google.GenAI;
using Google.GenAI.Types;
using JobTracker.Business.DTOs.Matching;
using Microsoft.Extensions.Configuration;

namespace JobTracker.Business.Matching.Providers;

public class GeminiLlmProvider : ILlmProvider
{
    private readonly IConfiguration _configuration;

    public GeminiLlmProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<MatchResultDto> MatchAsync(string jobDescription, string cvSkillsText)
    {
        var apiKey = _configuration["Matching:GeminiApiKey"];
        var modelName = _configuration["Matching:ModelName"] ?? "gemini-1.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured in appsettings.json.");
        }

        var client = new Client(apiKey: apiKey);

        var prompt = $@"
You are an expert ATS (Applicant Tracking System) career advisor.
Compare the following candidate skills against the job description and evaluate their match.

Candidate Skills:
{cvSkillsText}

Job Description:
{jobDescription}

Respond strictly in valid JSON format matching this exact schema:
{{
  ""matchScore"": <integer between 0 and 100>,
  ""matchingSkills"": [<array of strings containing matched skills>],
  ""missingSkills"": [<array of strings containing missing or weak skills>],
  ""suggestions"": [<array of actionable advice strings>]
}}";

        var config = new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            Temperature = 0.2f
        };

        var response = await client.Models.GenerateContentAsync(
            model: modelName,
            contents: prompt,
            config: config);

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            throw new InvalidOperationException("Received an empty response from the Gemini API.");
        }

        var result = JsonSerializer.Deserialize<MatchResultDto>(response.Text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return result ?? new MatchResultDto();
    }
}
