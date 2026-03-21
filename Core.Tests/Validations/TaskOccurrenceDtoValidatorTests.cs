using MyFeatures.DTO;
using MyFeatures.Validations;
using Xunit;

namespace Core.Tests.Validations;

public class TaskOccurrenceDtoValidatorTests
{
    [Fact]
    public void Validate_WhenDescriptionIsMissing_ReturnsMainDescriptionError()
    {
        var validator = new TaskOccurrenceDtoValidator();
        var dto = new TaskOccurrenceDto
        {
            Description = string.Empty,
            TaskTemplate = new TaskTemplateDto { Description = "Template description" }
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(TaskOccurrenceDto.Description));
    }

    [Fact]
    public void Validate_WhenTemplateDescriptionIsMissing_ReturnsTemplateDescriptionError()
    {
        var validator = new TaskOccurrenceDtoValidator();
        var dto = new TaskOccurrenceDto
        {
            Description = "Occurrence description",
            TaskTemplate = new TaskTemplateDto { Description = string.Empty }
        };

        var result = validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "TaskTemplate.Description");
    }

    [Fact]
    public void Validate_WhenRequiredDescriptionsExist_Passes()
    {
        var validator = new TaskOccurrenceDtoValidator();
        var dto = new TaskOccurrenceDto
        {
            Description = "Occurrence description",
            TaskTemplate = new TaskTemplateDto { Description = "Template description" }
        };

        var result = validator.Validate(dto);

        Assert.True(result.IsValid);
    }
}