using System.Linq.Expressions;
using Core.DomainModels;
using Core.Exceptions;
using Core.Services;
using Infrastructure.Interfaces.IRepository;
using Moq;
using Xunit;
using Entity = Infrastructure.Entities;

namespace Core.Tests.Services;

public class TaskTemplateServiceTests
{
    private const string TaskTemplateInclude = "TaskTemplate";
    private static readonly TimeProvider TestTimeProvider = new FixedTestTimeProvider(new DateTimeOffset(2026, 3, 20, 10, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly TestLocalDate = new(2026, 3, 20);

    [Fact]
    public async Task GetTaskOccurrenceById_WhenMissing_ThrowsNotFoundException()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var unitOfWork = CreateUnitOfWorkMock();

        taskOccurrenceRepository
            .Setup(repository => repository.GetByIdAsync(99, "TaskTemplate"))
            .ReturnsAsync((Entity.TaskOccurrence?)null);

        var service = CreateService(taskTemplateRepository, taskOccurrenceRepository, unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetTaskOccurrenceById(99));
    }

    [Fact]
    public async Task CompleteTaskOccurrence_ForOneTimeTask_MarksTemplateCompletedAndClearsOrdering()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var unitOfWork = CreateUnitOfWorkMock();

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

        var service = CreateService(taskTemplateRepository, taskOccurrenceRepository, unitOfWork);

        await service.CompleteTaskOccurrence(5, TestLocalDate);

        Assert.NotNull(taskOccurrenceEntity.CompletionDate);
        Assert.True(taskOccurrenceEntity.TaskTemplate.Completed);
        Assert.Null(taskOccurrenceEntity.TaskTemplate.RowIndex);
        unitOfWork.Verify(workUnit => workUnit.SaveChangesAsync(), Times.Once);
        taskOccurrenceRepository.Verify(repository => repository.Add(It.IsAny<Entity.TaskOccurrence>()), Times.Never);
    }

    [Fact]
    public async Task CompleteTaskOccurrence_ForRecurringTask_CreatesNextOccurrenceAndUpdatesOrdering()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var unitOfWork = CreateUnitOfWorkMock();

        Entity.TaskOccurrence? addedTaskOccurrence = null;
        var dueDate = TestTimeProvider.GetLocalNow().Date.AddDays(3);

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
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Entity.TaskOccurrence, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>>(),
                TaskTemplateInclude))
            .ReturnsAsync(
                new Entity.TaskOccurrence
                {
                    TaskTemplate = new Entity.TaskTemplate { RowIndex = 6 }
                });

        taskOccurrenceRepository
            .Setup(repository => repository.Add(It.IsAny<Entity.TaskOccurrence>()))
            .Callback<Entity.TaskOccurrence>(entity => addedTaskOccurrence = entity);

        var service = CreateService(taskTemplateRepository, taskOccurrenceRepository, unitOfWork);

        await service.CompleteTaskOccurrence(12, TestLocalDate);

        Assert.NotNull(addedTaskOccurrence);
        Assert.Equal(44, addedTaskOccurrence!.TaskTemplateId);
        Assert.Same(taskOccurrenceEntity.TaskTemplate, addedTaskOccurrence.TaskTemplate);
        Assert.Equal("Take vitamins", addedTaskOccurrence.Description);
        Assert.NotNull(addedTaskOccurrence.DueDate);
        Assert.NotNull(addedTaskOccurrence.CommittedDate);
        Assert.NotNull(taskOccurrenceEntity.TaskTemplate.RowIndex);
        unitOfWork.Verify(workUnit => workUnit.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCommittedTaskOccurrencesForNextWeek_UsesProvidedLocalDateToMoveExpiredTasks()
    {
        var taskTemplateRepository = new Mock<IGenericRepository<Entity.TaskTemplate>>();
        var taskOccurrenceRepository = new Mock<IGenericRepository<Entity.TaskOccurrence>>();
        var unitOfWork = CreateUnitOfWorkMock(hasChanges: true);

        var overdueTask = new Entity.TaskOccurrence
        {
            Id = 5,
            Description = "Overdue",
            CommittedDate = new DateTime(2026, 3, 20),
            TaskTemplate = new Entity.TaskTemplate
            {
                Id = 5,
                Description = "Overdue",
                Recurring = false,
                RowIndex = 2
            }
        };

        SetupTaskOccurrenceGetAll(taskOccurrenceRepository, new[] { overdueTask });

        taskOccurrenceRepository
            .Setup(repository => repository.GetFirstOrDefaultAsync(
                It.IsAny<Expression<Func<Entity.TaskOccurrence, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>>(),
                TaskTemplateInclude))
            .ReturnsAsync((Entity.TaskOccurrence?)null);

        var service = CreateService(taskTemplateRepository, taskOccurrenceRepository, unitOfWork);

        var result = await service.GetCommittedTaskOccurrencesForNextWeek(new DateOnly(2026, 3, 21));

        Assert.Equal(new DateTime(2026, 3, 21), overdueTask.CommittedDate);
        Assert.True(result.ContainsKey(new DateTime(2026, 3, 21)));
        Assert.Single(result[new DateTime(2026, 3, 21)]);
        Assert.NotNull(overdueTask.TaskTemplate.RowIndex);
        unitOfWork.Verify(workUnit => workUnit.SaveChangesAsync(), Times.Once);
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(bool hasChanges = false)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.SaveChangesAsync()).Returns(Task.CompletedTask);
        unitOfWork.SetupGet(unit => unit.HasChanges).Returns(hasChanges);
        return unitOfWork;
    }

    private static void SetupTaskOccurrenceGetAll(
        Mock<IGenericRepository<Entity.TaskOccurrence>> taskOccurrenceRepository,
        IEnumerable<Entity.TaskOccurrence> source)
    {
        taskOccurrenceRepository
            .Setup(repository => repository.GetAllAsync(
                It.IsAny<Expression<Func<Entity.TaskOccurrence, bool>>>(),
                It.IsAny<Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<int?>()))
            .ReturnsAsync((Expression<Func<Entity.TaskOccurrence, bool>> filter,
                Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>? orderBy,
                string includeProperties,
                int? skip,
                int? take) => ApplyQuery(source, filter, orderBy));
    }

    private static IEnumerable<Entity.TaskOccurrence> ApplyQuery(
        IEnumerable<Entity.TaskOccurrence> source,
        Expression<Func<Entity.TaskOccurrence, bool>>? filter,
        Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>>? orderBy)
    {
        IQueryable<Entity.TaskOccurrence> query = source.AsQueryable();

        if (filter is not null)
        {
            query = query.Where(filter);
        }

        if (orderBy is not null)
        {
            query = orderBy(query);
        }

        return query.ToList();
    }

    private static TaskTemplateService CreateService(
        Mock<IGenericRepository<Entity.TaskTemplate>> taskTemplateRepository,
        Mock<IGenericRepository<Entity.TaskOccurrence>> taskOccurrenceRepository,
        Mock<IUnitOfWork> unitOfWork)
    {
        return new TaskTemplateService(
            taskTemplateRepository.Object,
            taskOccurrenceRepository.Object,
            unitOfWork.Object,
            TestTimeProvider);
    }

    private sealed class FixedTestTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FixedTestTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}