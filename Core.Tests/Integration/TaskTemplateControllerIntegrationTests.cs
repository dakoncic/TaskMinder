using Core.DomainModels;
using Core.Interfaces;
using InfrastructureEntity = Infrastructure.Entities;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MyFeatures.DTO;
using Xunit;

namespace Core.Tests.Integration;

public sealed class TaskTemplateControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _httpClient;

    public TaskTemplateControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _httpClient = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTaskTemplateAndOccurrence_WhenPayloadIsInvalid_ReturnsValidationProblemDetails()
    {
        var request = new TaskOccurrenceDto
        {
            Description = string.Empty,
            TaskTemplate = new TaskTemplateDto
            {
                Description = string.Empty,
                Recurring = false
            }
        };

        var response = await _httpClient.PostAsJsonAsync("/api/TaskTemplate/CreateTaskTemplateAndOccurrence", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(validationProblem);
        Assert.Equal(400, validationProblem!.Status);
        Assert.Equal("One or more validation errors occurred.", validationProblem.Title);
        Assert.Contains(nameof(TaskOccurrenceDto.Description), validationProblem.Errors.Keys);
        Assert.Contains("TaskTemplate.Description", validationProblem.Errors.Keys);
        Assert.True(HasTraceId(validationProblem.Extensions));
    }

    [Fact]
    public async Task GetTaskOccurrenceById_WhenEntityDoesNotExist_ReturnsProblemDetails()
    {
        var response = await _httpClient.GetAsync("/api/TaskTemplate/GetTaskOccurrenceById/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(404, problemDetails!.Status);
        Assert.Equal("Resource not found.", problemDetails.Title);
        Assert.Equal("TaskOccurrence with ID 999999 not found.", problemDetails.Detail);
        Assert.True(HasTraceId(problemDetails.Extensions));
    }

    [Fact]
    public async Task GetTaskOccurrenceById_WhenUnhandledExceptionOccurs_ReturnsInternalServerErrorProblemDetails()
    {
        using var factory = new ApiWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITaskTemplateService>();
                services.AddScoped<ITaskTemplateService, ThrowingTaskTemplateService>();
            });
        });

        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/TaskTemplate/GetTaskOccurrenceById/123");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails!.Status);
        Assert.Equal("An unexpected error occurred.", problemDetails.Title);
        Assert.Equal("The server encountered an unexpected error.", problemDetails.Detail);
        Assert.True(HasTraceId(problemDetails.Extensions));
    }

    private static bool HasTraceId(IDictionary<string, object?> extensions)
    {
        return extensions.TryGetValue("traceId", out var value)
            && value is not null
            && !string.IsNullOrWhiteSpace(GetExtensionText(value));
    }

    private static string? GetExtensionText(object extensionValue)
    {
        return extensionValue switch
        {
            JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.String => jsonElement.GetString(),
            _ => extensionValue.ToString()
        };
    }

    private sealed class ThrowingTaskTemplateService : ITaskTemplateService
    {
        public Task CreateTaskTemplateAndOccurrence(TaskOccurrence taskOccurrenceDomain)
        {
            throw new NotSupportedException();
        }

        public Task<TaskOccurrence> GetTaskOccurrenceById(int taskOccurrenceId)
        {
            throw new InvalidOperationException("Synthetic failure for integration testing.");
        }

        public Task UpdateTaskTemplateAndOccurrence(int taskOccurrenceId, TaskOccurrence taskOccurrenceDomain)
        {
            throw new NotSupportedException();
        }

        public Task DeleteTaskTemplateAndOccurrences(int taskTemplateId)
        {
            throw new NotSupportedException();
        }

        public Task CompleteTaskOccurrence(int taskOccurrenceId)
        {
            throw new NotSupportedException();
        }

        public Task CommitTaskOccurrenceOrReturnToGroup(DateTime? commitDay, int taskOccurrenceId)
        {
            throw new NotSupportedException();
        }

        public Task ReorderTaskTemplateInsideGroup(int taskTemplateId, int newIndex, bool recurring)
        {
            throw new NotSupportedException();
        }

        public Task ReorderTaskOccurrenceInsideGroup(int taskOccurrenceId, DateTime commitDate, int newIndex)
        {
            throw new NotSupportedException();
        }

        public Task<List<TaskOccurrence>> GetActiveTaskOccurrences(bool recurring)
        {
            throw new NotSupportedException();
        }

        public Task<Dictionary<DateTime, List<InfrastructureEntity.TaskOccurrence>>> GetCommittedTaskOccurrencesForNextWeek()
        {
            throw new NotSupportedException();
        }
    }
}