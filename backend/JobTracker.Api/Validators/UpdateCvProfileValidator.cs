using FluentValidation;
using JobTracker.Business.DTOs.Profile;

namespace JobTracker.Api.Validators
{
    public class UpdateCvProfileValidator : AbstractValidator<UpdateCvProfileDto>
    {
        public UpdateCvProfileValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");

            RuleFor(x => x.SkillsText)
                .NotEmpty().WithMessage("Skills text is required for matching capabilities.");
        }
    }
}