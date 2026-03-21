using FluentValidation;
using MyFeatures.DTO;

namespace MyFeatures.Validations
{
    //automatski radi bez da manualno ja moram postavit
    public class TaskOccurrenceDtoValidator : AbstractValidator<TaskOccurrenceDto>
    {
        public TaskOccurrenceDtoValidator()
        {
            // Validate the Description in the main DTO
            RuleFor(dto => dto.Description)
                .NotEmpty()
                .WithMessage("Description is required.");

            When(dto => dto.TaskTemplate != null, () =>
            {
                RuleFor(dto => dto.TaskTemplate.Description)
                    .NotEmpty()
                    .WithMessage("Task template description is required.");
            });
        }
    }
}
