using Core.DomainModels;

namespace Core.Interfaces
{
    public interface ITaskTemplateService
    {
        Task CreateTaskTemplateAndOccurrence(TaskOccurrence taskOccurrenceDomain);
        Task<TaskOccurrence> GetTaskOccurrenceById(int taskOccurrenceId);
        Task UpdateTaskTemplateAndOccurrence(int taskOccurrenceId, TaskOccurrence taskOccurrenceDomain);
        Task DeleteTaskTemplateAndOccurrences(int taskTemplateId);
        Task CompleteTaskOccurrence(int taskOccurrenceId, DateOnly localDate);
        Task CommitTaskOccurrenceOrReturnToGroup(DateOnly? commitDay, int taskOccurrenceId);
        Task ReorderTaskTemplateInsideGroup(int taskTemplateId, int newIndex, bool recurring);
        Task ReorderTaskOccurrenceInsideGroup(int taskOccurrenceId, DateTime commitDate, int newIndex);
        Task<List<TaskOccurrence>> GetActiveTaskOccurrences(bool recurring, DateOnly localDate);
        Task<Dictionary<DateTime, List<TaskOccurrence>>> GetCommittedTaskOccurrencesForNextWeek(DateOnly localDate);
    }
}
