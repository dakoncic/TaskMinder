using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Shared;
using System.Linq.Expressions;
//using Infrastructure.Entities; ako imam error ambiguous reference, onda maknut ovu liniju
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class TaskTemplateService : BaseService, ITaskTemplateService
    {
        private const string TaskTemplateInclude = "TaskTemplate";

        private readonly IGenericRepository<Entity.TaskTemplate> _taskTemplateRepository;
        private readonly IGenericRepository<Entity.TaskOccurrence> _taskOccurrenceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeProvider _timeProvider;

        public TaskTemplateService(
            IGenericRepository<Entity.TaskTemplate> taskTemplateRepository,
            IGenericRepository<Entity.TaskOccurrence> taskOccurrenceRepository,
            IUnitOfWork unitOfWork,
            TimeProvider timeProvider
            )
        {
            _taskTemplateRepository = taskTemplateRepository;
            _taskOccurrenceRepository = taskOccurrenceRepository;
            _unitOfWork = unitOfWork;
            _timeProvider = timeProvider;
        }

        public async Task CreateTaskTemplateAndOccurrence(TaskOccurrence taskOccurrenceDomain)
        {
            SyncCommittedDateFromDueDate(taskOccurrenceDomain);
            taskOccurrenceDomain.TaskTemplate.RowIndex = await GetNextRowIndexForCurrentGroup(taskOccurrenceDomain);

            var taskTemplateEntity = taskOccurrenceDomain.TaskTemplate.Adapt<Entity.TaskTemplate>();
            var taskOccurrenceEntity = taskOccurrenceDomain.Adapt<Entity.TaskOccurrence>();

            taskTemplateEntity.TaskOccurrences.Add(taskOccurrenceEntity);

            _taskTemplateRepository.Add(taskTemplateEntity);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<TaskOccurrence> GetTaskOccurrenceById(int taskOccurrenceId)
        {
            var taskOccurrenceEntity = await _taskOccurrenceRepository.GetByIdAsync(taskOccurrenceId, TaskTemplateInclude);

            //ostavljamo check ovdje u servisu, ako je entity null, onda on ne može zvat
            //nikakvu metodu da provjeri sam sebe jeli null
            CheckIfNull(taskOccurrenceEntity, $"TaskOccurrence with ID {taskOccurrenceId} not found.");

            return taskOccurrenceEntity.Adapt<TaskOccurrence>();
        }

        public async Task UpdateTaskTemplateAndOccurrence(int taskOccurrenceId, TaskOccurrence taskOccurrenceDomain)
        {
            var taskOccurrenceEntity = await _taskOccurrenceRepository.GetByIdAsync(taskOccurrenceId, TaskTemplateInclude);

            CheckIfNull(taskOccurrenceEntity, $"TaskOccurrence with ID {taskOccurrenceId} not found.");

            var updatedTaskOccurrence = taskOccurrenceEntity.Adapt<TaskOccurrence>();
            taskOccurrenceDomain.Adapt(updatedTaskOccurrence);

            var oldDueDate = taskOccurrenceEntity.DueDate;
            var newDueDate = updatedTaskOccurrence.DueDate?.Date;
            var oldCommittedDate = taskOccurrenceEntity.CommittedDate;

            if (oldDueDate?.Date != newDueDate?.Date)
            {
                await HandleDueDateChange(taskOccurrenceEntity, updatedTaskOccurrence, oldCommittedDate, newDueDate);
            }

            updatedTaskOccurrence.Adapt(taskOccurrenceEntity);
            await _unitOfWork.SaveChangesAsync();
        }

        private async Task HandleDueDateChange(Entity.TaskOccurrence taskOccurrenceEntity, TaskOccurrence updatedTaskOccurrence, DateTime? oldCommittedDate, DateTime? newDueDate)
        {
            SyncCommittedDateFromDueDate(updatedTaskOccurrence);
            updatedTaskOccurrence.TaskTemplate.RowIndex = await GetNextRowIndexAfterGroupChange(
                oldCommittedDate,
                taskOccurrenceEntity.TaskTemplate,
                updatedTaskOccurrence);
        }

        public async Task DeleteTaskTemplateAndOccurrences(int taskTemplateId)
        {
            var taskTemplateEntity = await _taskTemplateRepository.GetByIdAsync(taskTemplateId, "TaskOccurrences");

            CheckIfNull(taskTemplateEntity, $"TaskTemplate with ID {taskTemplateId} not found.");

            //dohvaćam committed TaskOccurrence za TaskTemplate ako postoji
            var taskOccurrenceEntity = taskTemplateEntity.TaskOccurrences.FirstOrDefault(x => x.CommittedDate != null && x.CompletionDate == null);

            await RemoveTaskTemplateFromCurrentGroup(taskTemplateEntity, taskOccurrenceEntity?.CommittedDate);

            _taskTemplateRepository.Delete(taskTemplateId);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CompleteTaskOccurrence(int taskOccurrenceId, DateOnly localDate)
        {
            var taskOccurrenceEntity = await _taskOccurrenceRepository.GetByIdAsync(taskOccurrenceId, TaskTemplateInclude);

            CheckIfNull(taskOccurrenceEntity, $"TaskOccurrence with ID {taskOccurrenceId} not found.");

            taskOccurrenceEntity.CompletionDate = _timeProvider.GetUtcNow().UtcDateTime;

            if (!taskOccurrenceEntity.TaskTemplate.Recurring)
            {
                await RemoveTaskTemplateFromCurrentGroup(taskOccurrenceEntity.TaskTemplate, taskOccurrenceEntity.CommittedDate);
                taskOccurrenceEntity.TaskTemplate.Completed = true;
                taskOccurrenceEntity.TaskTemplate.RowIndex = null;
            }
            else
            {
                var taskOccurrenceDomain = taskOccurrenceEntity.Adapt<TaskOccurrence>();

                var currentLocalDateTime = localDate.ToDateTime(TimeOnly.MinValue);

                //dobar primjer enkapsulacije biznis logike u domain klasu
                var newTaskOccurrence = taskOccurrenceDomain.CreateNewRecurringTask(currentLocalDateTime);

                taskOccurrenceEntity.TaskTemplate.RowIndex = await GetNextRowIndexAfterGroupChange(
                    taskOccurrenceEntity.CommittedDate,
                    taskOccurrenceEntity.TaskTemplate,
                    newTaskOccurrence);

                var newTaskOccurrenceEntity = newTaskOccurrence.Adapt<Entity.TaskOccurrence>();
                newTaskOccurrenceEntity.TaskTemplate = taskOccurrenceEntity.TaskTemplate;
                newTaskOccurrenceEntity.TaskTemplateId = taskOccurrenceEntity.TaskTemplateId;
                newTaskOccurrenceEntity.Description = taskOccurrenceEntity.TaskTemplate.Description;
                _taskOccurrenceRepository.Add(newTaskOccurrenceEntity);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CommitTaskOccurrenceOrReturnToGroup(DateTime? commitDay, int taskOccurrenceId)
        {
            var taskOccurrenceEntity = await _taskOccurrenceRepository.GetByIdAsync(taskOccurrenceId, TaskTemplateInclude);
            CheckIfNull(taskOccurrenceEntity, $"TaskOccurrence with ID {taskOccurrenceId} not found.");

            var taskOccurrenceDomain = taskOccurrenceEntity.Adapt<TaskOccurrence>();

            var oldCommittedDate = taskOccurrenceDomain.CommittedDate;

            if (oldCommittedDate?.Date != commitDay?.Date)
            {
                taskOccurrenceDomain.CommittedDate = commitDay?.Date;

                if (commitDay is null)
                {
                    taskOccurrenceDomain.Description = taskOccurrenceDomain.TaskTemplate.Description;
                    taskOccurrenceDomain.DueDate = null;
                }

                taskOccurrenceDomain.TaskTemplate.RowIndex = await GetNextRowIndexAfterGroupChange(
                    oldCommittedDate,
                    taskOccurrenceEntity.TaskTemplate,
                    taskOccurrenceDomain);
            }

            taskOccurrenceDomain.Adapt(taskOccurrenceEntity);

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReorderTaskTemplateInsideGroup(int taskTemplateId, int newIndex, bool recurring)
        {
            var taskTemplateEntity = await _taskTemplateRepository.GetByIdAsync(taskTemplateId);

            CheckIfNull(taskTemplateEntity, $"TaskTemplate with ID {taskTemplateId} not found.");

            int currentIndex = taskTemplateEntity.RowIndex!.Value;

            Expression<Func<Entity.TaskTemplate, bool>> filter = x =>
                !x.Completed &&
                x.Recurring.Equals(recurring) &&
                x.TaskOccurrences.Any(task => task.CompletionDate == null && !task.CommittedDate.HasValue) &&
                x.Id != taskTemplateId;
            Func<IQueryable<Entity.TaskTemplate>, IOrderedQueryable<Entity.TaskTemplate>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var taskTemplatesEntity = await _taskTemplateRepository.GetAllAsync(filter, orderBy);

            RowIndexHelper.ManaulReorderRowIndexes<Entity.TaskTemplate>(taskTemplatesEntity, newIndex, currentIndex);

            taskTemplateEntity.RowIndex = newIndex;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReorderTaskOccurrenceInsideGroup(int taskOccurrenceId, DateTime commitDate, int newIndex)
        {
            var taskOccurrenceEntity = await _taskOccurrenceRepository.GetByIdAsync(taskOccurrenceId, TaskTemplateInclude);

            CheckIfNull(taskOccurrenceEntity, $"TaskOccurrence with ID {taskOccurrenceId} not found.");

            int currentIndex = taskOccurrenceEntity.TaskTemplate.RowIndex!.Value;

            Expression<Func<Entity.TaskOccurrence, bool>> filter =
                x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date == commitDate.Date &&
                x.Id != taskOccurrenceId;
            Func<IQueryable<Entity.TaskOccurrence>, IOrderedQueryable<Entity.TaskOccurrence>> orderBy = q => q.OrderBy(x => x.TaskTemplate.RowIndex);

            var taskOccurrencesForDateEntity = await _taskOccurrenceRepository.GetAllAsync(filter, orderBy, "TaskTemplate");

            var taskTemplatesForDateEntity = taskOccurrencesForDateEntity.Select(x => x.TaskTemplate);

            RowIndexHelper.ManaulReorderRowIndexes<Entity.TaskTemplate>(taskTemplatesForDateEntity, newIndex, currentIndex);

            taskOccurrenceEntity.TaskTemplate.RowIndex = newIndex;

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<List<TaskOccurrence>> GetActiveTaskOccurrences(bool recurring, DateOnly localDate)
        {
            var businessDate = ResolveBusinessDate(localDate);

            Expression<Func<Entity.TaskOccurrence, bool>> filter = i =>
                i.TaskTemplate.Recurring.Equals(recurring) &&
                i.CompletionDate == null &&
                (i.CommittedDate == null || i.CommittedDate.Value.Date >= businessDate.AddDays(GlobalConstants.DaysRange));

            var taskOccurrencesEntity = await _taskOccurrenceRepository.GetAllAsync(
                filter: filter,
                orderBy: x => x.OrderBy(n => n.DueDate).ThenBy(n => n.TaskTemplate.RowIndex),
                includeProperties: TaskTemplateInclude
                );

            return taskOccurrencesEntity.Adapt<List<TaskOccurrence>>();
        }

        public async Task<Dictionary<DateTime, List<TaskOccurrence>>> GetCommittedTaskOccurrencesForNextWeek(DateOnly localDate)
        {
            var businessDate = ResolveBusinessDate(localDate);

            await UpdateExpiredTaskOccurrences(businessDate);

            var taskOccurrencesEntity = await GetTaskOccurrencesGroupedByCommitDateForNextWeek(businessDate);

            return taskOccurrencesEntity.ToDictionary(
                group => group.Key,
                group => group.Value.Adapt<List<TaskOccurrence>>());
        }

        private async Task UpdateExpiredTaskOccurrences(DateTime businessDate)
        {
            Expression<Func<Entity.TaskOccurrence, bool>> filter = x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
            x.CommittedDate.Value.Date < businessDate;

            var expiredTaskOccurrencesEntity = await _taskOccurrenceRepository.GetAllAsync(filter, includeProperties: TaskTemplateInclude);

            if (expiredTaskOccurrencesEntity.Any())
            {
                int newRowIndex = await GetNewScheduledRowIndex(businessDate);

                foreach (var task in expiredTaskOccurrencesEntity)
                {
                    task.CommittedDate = businessDate;
                    task.TaskTemplate.RowIndex = newRowIndex++;
                }

                if (_unitOfWork.HasChanges)
                {
                    await _unitOfWork.SaveChangesAsync();
                }
            }
        }

        private async Task<Dictionary<DateTime, List<Entity.TaskOccurrence>>> GetTaskOccurrencesGroupedByCommitDateForNextWeek(DateTime businessDate)
        {
            var endOfDayRange = businessDate.AddDays(GlobalConstants.DaysRange);

            Expression<Func<Entity.TaskOccurrence, bool>> filter = x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date >= businessDate &&
                x.CommittedDate.Value.Date < endOfDayRange;

            var taskOccurrencesForNextWeekEntity = await _taskOccurrenceRepository.GetAllAsync(filter, includeProperties: TaskTemplateInclude);

            var groupedTaskOccurrencesEntity = new Dictionary<DateTime, List<Entity.TaskOccurrence>>();

            for (DateTime day = businessDate; day < endOfDayRange; day = day.AddDays(1))
            {
                // commitani taskovi za specifičan dan
                var tasksForDay = taskOccurrencesForNextWeekEntity
                    .Where(t =>
                        t.CommittedDate.HasValue &&
                        t.CommittedDate.Value.Date == day
                        )
                    .OrderBy(t => t.TaskTemplate.RowIndex)
                    .ToList();

                groupedTaskOccurrencesEntity.Add(day, tasksForDay);
            }

            return groupedTaskOccurrencesEntity;
        }

        private async Task<int> GetNewBacklogRowIndex(bool recurring)
        {
            var maxRowIndexTaskTemplateEntity = await _taskTemplateRepository.GetFirstOrDefaultAsync(
                            x =>
                            !x.Completed &&
                            x.Recurring.Equals(recurring) &&
                            x.TaskOccurrences.Any(task => task.CompletionDate == null && !task.CommittedDate.HasValue),
                            q => q.OrderByDescending(x => x.RowIndex)
                        );

            return GetNextRowIndexFromMax(maxRowIndexTaskTemplateEntity?.RowIndex);
        }

        private static void SyncCommittedDateFromDueDate(TaskOccurrence taskOccurrenceDomain)
        {
            taskOccurrenceDomain.CommittedDate = taskOccurrenceDomain.DueDate?.Date;
        }

        private async Task<int> GetNextRowIndexForCurrentGroup(TaskOccurrence taskOccurrenceDomain)
        {
            return await GetNextRowIndex(taskOccurrenceDomain.CommittedDate, taskOccurrenceDomain.TaskTemplate.Recurring);
        }

        private async Task<int> GetNextRowIndex(DateTime? committedDate, bool recurring)
        {
            if (committedDate.HasValue)
            {
                return await GetNewScheduledRowIndex(committedDate.Value.Date);
            }

            return await GetNewBacklogRowIndex(recurring);
        }

        private async Task<int> GetNextRowIndexAfterGroupChange(DateTime? oldCommittedDate, Entity.TaskTemplate currentTaskTemplate, TaskOccurrence targetTaskOccurrence)
        {
            await RemoveTaskTemplateFromCurrentGroup(currentTaskTemplate, oldCommittedDate);
            return await GetNextRowIndexForCurrentGroup(targetTaskOccurrence);
        }

        private async Task<int> GetNewScheduledRowIndex(DateTime? compareDate)
        {
            var maxRowIndexTaskOccurrenceEntity = await _taskOccurrenceRepository.GetFirstOrDefaultAsync(
                x =>
                    x.CompletionDate == null &&
                    x.CommittedDate.HasValue &&
                    compareDate.HasValue &&
                    x.CommittedDate.Value.Date == compareDate.Value.Date,
                q => q.OrderByDescending(x => x.TaskTemplate.RowIndex),
                TaskTemplateInclude);

            return GetNextRowIndexFromMax(maxRowIndexTaskOccurrenceEntity?.TaskTemplate?.RowIndex);
        }

        private static int GetNextRowIndexFromMax(int? maxRowIndex)
        {
            return maxRowIndex + 1 ?? 0;
        }

        private static DateTime ResolveBusinessDate(DateOnly localDate)
        {
            return localDate.ToDateTime(TimeOnly.MinValue);
        }

        private async Task RemoveTaskTemplateFromCurrentGroup(Entity.TaskTemplate currentTaskTemplate, DateTime? oldCommittedDate)
        {
            if (oldCommittedDate is not null)
            {
                await UpdateScheduledRowIndexesIfDateProvided(oldCommittedDate, currentTaskTemplate.RowIndex);
            }
            else if (currentTaskTemplate.RowIndex.HasValue)
            {
                await UpdateBacklogRowIndexesForRemainingItems(currentTaskTemplate);
            }
        }

        private async Task UpdateBacklogRowIndexesForRemainingItems(Entity.TaskTemplate taskTemplateEntity)
        {
            await _taskTemplateRepository.UpdateBatchAsync(
                x => !x.Completed &&
                      x.Recurring.Equals(taskTemplateEntity.Recurring) &&
                      x.TaskOccurrences.Any(task => task.CompletionDate == null && !task.CommittedDate.HasValue) &&
                      x.RowIndex > taskTemplateEntity.RowIndex,
                x => new Entity.TaskTemplate { RowIndex = x.RowIndex - 1 }
            );
        }

        private async Task UpdateScheduledRowIndexesIfDateProvided(DateTime? oldCommittedDate, int? oldTaskTemplateRowIndex)
        {
            if (!oldCommittedDate.HasValue || !oldTaskTemplateRowIndex.HasValue)
            {
                return;
            }

            await _taskTemplateRepository.UpdateBatchAsync(
                x => !x.Completed &&
                     x.RowIndex > oldTaskTemplateRowIndex &&
                     x.TaskOccurrences.Any(task =>
                        task.CompletionDate == null &&
                        task.CommittedDate.HasValue &&
                        task.CommittedDate.Value.Date == oldCommittedDate.Value.Date),
                x => new Entity.TaskTemplate { RowIndex = x.RowIndex - 1 }
            );
        }
    }
}
