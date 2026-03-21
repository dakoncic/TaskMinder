using Core.DomainModels;
using Entity = Infrastructure.Entities;

namespace Core.Interfaces
{
    public interface ITaskTemplateService
    {
        Task CreateTaskTemplateAndOccurrence(TaskOccurrence taskOccurrenceDomain);
        Task<TaskOccurrence> GetTaskOccurrenceById(int taskOccurrenceId);
        Task UpdateTaskTemplateAndOccurrence(int taskOccurrenceId, TaskOccurrence taskOccurrenceDomain);
        Task DeleteTaskTemplateAndOccurrences(int taskTemplateId);
        Task CompleteTaskOccurrence(int taskOccurrenceId);
        Task CommitTaskOccurrenceOrReturnToGroup(DateTime? commitDay, int taskOccurrenceId);
        Task ReorderTaskTemplateInsideGroup(int taskTemplateId, int newIndex, bool recurring);
        Task ReorderTaskOccurrenceInsideGroup(int taskOccurrenceId, DateTime commitDate, int newIndex);
        Task<List<TaskOccurrence>> GetActiveTaskOccurrences(bool recurring);
        Task<Dictionary<DateTime, List<Entity.TaskOccurrence>>> GetCommittedTaskOccurrencesForNextWeek();
    }
}
