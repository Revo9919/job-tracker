using FluentValidation;
using JobTracker.Business.DTOs.Matching;

namespace JobTracker.Api.Validators
{
    public class MatchRequestValidator : AbstractValidator<MatchRequestDto>
    {
        public MatchRequestValidator()
        {
            RuleFor(x => x.JobDescription)
                .NotEmpty().WithMessage("Job description is required.")
                .MaximumLength(8000).WithMessage("Job description cannot exceed 8000 characters to respect LLM token limits.");

            RuleFor(x => x.CvProfileId)
                .GreaterThan(0).WithMessage("A valid CV Profile ID is required.");
        }
    }
}