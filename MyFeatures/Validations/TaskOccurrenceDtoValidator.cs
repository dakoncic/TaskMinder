using FluentValidation;
using MyFeatures.DTO;

namespace MyFeatures.Validations
{
    public class TaskOccurrenceDtoValidator : AbstractValidator<TaskOccurrenceDto>
    {
        public TaskOccurrenceDtoValidator()
        {
            RuleFor(dto => dto.Description)
                .NotEmpty()
                .WithMessage("Description is required.");

            RuleFor(dto => dto.TaskTemplate)
                .NotNull()
                .WithMessage("Task template is required.");

            When(dto => dto.TaskTemplate != null, () =>
            {
                RuleFor(dto => dto.TaskTemplate.Description)
                    .NotEmpty()
                    .WithMessage("Task template description is required.");
            });
        }
    }
}
