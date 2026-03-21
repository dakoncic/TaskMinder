using System.Linq.Expressions;
using Core.DomainModels;
using Core.Enum;
using Core.Exceptions;
using Core.Services;
using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Entity = Infrastructure.Entities;

namespace Core.Tests.Services;

public class TaskTemplateServiceTests
{
    private const string TaskTemplateInclude = "TaskTemplate";

    [Fact]
    public async Task CreateTaskTemplateAndOccurrence_WithBacklogTask_AssignsNextBacklogIndex()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var context = CreateContext();

        Entity.TaskTemplate? addedTaskTemplate = null;

        taskTemplateRepository
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Entity.TaskTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskTemplate>, IOrderedQueryable<Entity.TaskTemplate>>>() ))
            .ReturnsAsync(new Entity.TaskTemplate { RowIndex = 2 });

        taskTemplateRepository
            .Setup(repository => repository.Add(It.IsAny<Entity.TaskTemplate>()))
            .Callback<Entity.TaskTemplate>(entity => addedTaskTemplate = entity);

        var service = new TaskTemplateService(context, taskTemplateRepository.Object, taskOccurrenceRepository.Object);

        var taskOccurrence = new TaskOccurrence
        {
            Description = "Prepare documents",
            TaskTemplate = new TaskTemplate
            {
                Description = "Prepare documents",
                Recurring = false
            }
        };

        await service.CreateTaskTemplateAndOccurrence(taskOccurrence);

        Assert.NotNull(addedTaskTemplate);
        Assert.Equal(3, addedTaskTemplate!.RowIndex);
        var addedOccurrence = Assert.Single(addedTaskTemplate.TaskOccurrences);
        Assert.Null(addedOccurrence.CommittedDate);
        Assert.Null(addedOccurrence.DueDate);
    }

    [Fact]
    public async Task CreateTaskTemplateAndOccurrence_WithScheduledTask_AssignsCommittedDateAndNextScheduledIndex()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var context = CreateContext();

        Entity.TaskTemplate? addedTaskTemplate = null;
        var dueDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        taskOccurrenceRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<Entity.TaskOccurrence, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>>(),
                TaskTemplateInclude,
                null,
                1))
            .ReturnsAsync(new[]
            {
                new Entity.TaskOccurrence
                {
                    TaskTemplate = new Entity.TaskTemplate { RowIndex = 4 }
                }
            });

        taskTemplateRepository
            .Setup(repository => repository.Add(It.IsAny<Entity.TaskTemplate>()))
            .Callback<Entity.TaskTemplate>(entity => addedTaskTemplate = entity);

        var service = new TaskTemplateService(context, taskTemplateRepository.Object, taskOccurrenceRepository.Object);

        var taskOccurrence = new TaskOccurrence
        {
            Description = "Pay bill",
            DueDate = dueDate,
            TaskTemplate = new TaskTemplate
            {
                Description = "Pay bill",
                Recurring = false
            }
        };

        await service.CreateTaskTemplateAndOccurrence(taskOccurrence);

        Assert.NotNull(addedTaskTemplate);
        Assert.Equal(5, addedTaskTemplate!.RowIndex);
        var addedOccurrence = Assert.Single(addedTaskTemplate.TaskOccurrences);
        Assert.Equal(dueDate, addedOccurrence.DueDate);
        Assert.Equal(dueDate, addedOccurrence.CommittedDate);
    }

    [Fact]
    public async Task GetTaskOccurrenceById_WhenMissing_ThrowsNotFoundException()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var context = CreateContext();

        taskOccurrenceRepository
            .Setup(repository => repository.GetByIdAsync(99, "TaskTemplate"))
            .ReturnsAsync((Entity.TaskOccurrence?)null);

        var service = new TaskTemplateService(context, taskTemplateRepository.Object, taskOccurrenceRepository.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetTaskOccurrenceById(99));
    }

    [Fact]
    public async Task CommitTaskOccurrenceOrReturnToGroup_WhenReturnedToBacklog_ResetsSchedulingFieldsAndAssignsBacklogIndex()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var context = CreateContext();

        var taskOccurrenceEntity = new Entity.TaskOccurrence
        {
            Id = 7,
            TaskTemplateId = 21,
            Description = "Committed description",
            DueDate = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
            CommittedDate = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
            TaskTemplate = new Entity.TaskTemplate
            {
                Id = 21,
                Description = "Template description",
                Recurring = false,
                RowIndex = 4
            }
        };

        taskOccurrenceRepository
            .Setup(repository => repository.GetByIdAsync(7, TaskTemplateInclude))
            .ReturnsAsync(taskOccurrenceEntity);

        taskTemplateRepository
            .Setup(repository => repository.UpdateBatchAsync(
                It.IsAny<Expression<Func<Entity.TaskTemplate, bool>>>(),
                It.IsAny<Expression<Func<Entity.TaskTemplate, Entity.TaskTemplate>>>() ))
            .ReturnsAsync(1);

        taskTemplateRepository
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Entity.TaskTemplate, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskTemplate>, IOrderedQueryable<Entity.TaskTemplate>>>() ))
            .ReturnsAsync(new Entity.TaskTemplate { RowIndex = 1 });

        var service = new TaskTemplateService(context, taskTemplateRepository.Object, taskOccurrenceRepository.Object);

        await service.CommitTaskOccurrenceOrReturnToGroup(null, 7);

        Assert.Null(taskOccurrenceEntity.CommittedDate);
        Assert.Null(taskOccurrenceEntity.DueDate);
        Assert.Equal("Template description", taskOccurrenceEntity.Description);
        Assert.Equal(2, taskOccurrenceEntity.TaskTemplate.RowIndex);
    }

    [Fact]
    public async Task CompleteTaskOccurrence_ForOneTimeTask_MarksTemplateCompletedAndClearsOrdering()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var context = CreateContext();

        var taskOccurrenceEntity = new Entity.TaskOccurrence
        {
            Id = 5,
            Description = "Book dentist appointment",
            TaskTemplate = new Entity.TaskTemplate
            {
                Id = 14,
                Description = "Book dentist appointment",
                Recurring = false,
                RowIndex = 2
            }
        };

        taskOccurrenceRepository
            .Setup(repository => repository.GetByIdAsync(5, TaskTemplateInclude))
            .ReturnsAsync(taskOccurrenceEntity);

        taskTemplateRepository
            .Setup(repository => repository.UpdateBatchAsync(
                It.IsAny<Expression<Func<Entity.TaskTemplate, bool>>>(),
                It.IsAny<Expression<Func<Entity.TaskTemplate, Entity.TaskTemplate>>>() ))
            .ReturnsAsync(1);

        var service = new TaskTemplateService(context, taskTemplateRepository.Object, taskOccurrenceRepository.Object);

        await service.CompleteTaskOccurrence(5);

        Assert.NotNull(taskOccurrenceEntity.CompletionDate);
        Assert.True(taskOccurrenceEntity.TaskTemplate.Completed);
        Assert.Null(taskOccurrenceEntity.TaskTemplate.RowIndex);
        taskOccurrenceRepository.Verify(repository => repository.Add(It.IsAny<Entity.TaskOccurrence>()), Times.Never);
    }

    [Fact]
    public async Task CompleteTaskOccurrence_ForRecurringTask_CreatesNextOccurrenceAndUpdatesOrdering()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var context = CreateContext();

        Entity.TaskOccurrence? addedTaskOccurrence = null;
        var dueDate = DateTime.Now.Date.AddDays(3);

        var taskOccurrenceEntity = new Entity.TaskOccurrence
        {
            Id = 12,
            TaskTemplateId = 44,
            Description = "Take vitamins",
            DueDate = dueDate,
            CommittedDate = dueDate,
            TaskTemplate = new Entity.TaskTemplate
            {
                Id = 44,
                Description = "Take vitamins",
                Recurring = true,
                RenewOnDueDate = true,
                IntervalType = Shared.Enum.IntervalType.Days,
                IntervalValue = 7,
                RowIndex = 3
            }
        };

        taskOccurrenceRepository
            .Setup(repository => repository.GetByIdAsync(12, TaskTemplateInclude))
            .ReturnsAsync(taskOccurrenceEntity);

        taskTemplateRepository
            .Setup(repository => repository.UpdateBatchAsync(
                It.IsAny<Expression<Func<Entity.TaskTemplate, bool>>>(),
                It.IsAny<Expression<Func<Entity.TaskTemplate, Entity.TaskTemplate>>>() ))
            .ReturnsAsync(1);

        taskOccurrenceRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<Entity.TaskOccurrence, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>>(),
                TaskTemplateInclude,
                null,
                1))
            .ReturnsAsync(new[]
            {
                new Entity.TaskOccurrence
                {
                    TaskTemplate = new Entity.TaskTemplate { RowIndex = 6 }
                }
            });

        taskOccurrenceRepository
            .Setup(repository => repository.Add(It.IsAny<Entity.TaskOccurrence>()))
            .Callback<Entity.TaskOccurrence>(entity => addedTaskOccurrence = entity);

        var service = new TaskTemplateService(context, taskTemplateRepository.Object, taskOccurrenceRepository.Object);

        await service.CompleteTaskOccurrence(12);

        Assert.NotNull(addedTaskOccurrence);
        Assert.Equal(44, addedTaskOccurrence!.TaskTemplateId);
        Assert.Equal("Take vitamins", addedTaskOccurrence.Description);
        Assert.NotNull(addedTaskOccurrence.DueDate);
        Assert.NotNull(addedTaskOccurrence.CommittedDate);
        Assert.Equal(7, taskOccurrenceEntity.TaskTemplate.RowIndex);
    }

    private static MyFeaturesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyFeaturesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MyFeaturesDbContext(options);
    }
}