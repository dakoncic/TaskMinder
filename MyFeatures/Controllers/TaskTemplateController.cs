using Core.DomainModels;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyFeatures.DTO;

namespace MyFeatures.Controllers
{
    [ApiController]
    //route attribut da ima kontrollera
    [Route("api/[controller]")]
    public class TaskTemplateController : ControllerBase
    {
        private readonly ITaskTemplateService _taskTemplateService;

        public TaskTemplateController(ITaskTemplateService taskTemplateService)
        {
            _taskTemplateService = taskTemplateService;
        }

        /// <summary>
        /// Creates a new task template and associated task occurrence.
        /// </summary>
        /// <param name="taskOccurrenceDto">The data transfer object containing the task template and occurrence details.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CreateTaskTemplateAndOccurrence")]
        //mogao sam i [Route("[action]")] pa iznad [HttpPost], isto je, ali je čišće u jednoj liniji
        public async Task<ActionResult> CreateTaskTemplateAndOccurrence(TaskOccurrenceDto taskOccurrenceDto)
        {
            var taskOccurrenceDomain = taskOccurrenceDto.Adapt<TaskOccurrence>();

            await _taskTemplateService.CreateTaskTemplateAndOccurrence(taskOccurrenceDomain);

            return Ok();
        }

        /// <summary>
        /// Retrieves a task occurrence by its ID.
        /// </summary>
        /// <param name="id">The ID of the task occurrence to retrieve.</param>
        /// <returns>An ActionResult containing the task occurrence data transfer object.</returns>
        //bez {id} bi morao zvat metodu preko query parametra Get?id=123, 
        //a sa {id} mogu Get/1
        [HttpGet("GetTaskOccurrenceById/{id}")]
        public async Task<ActionResult<TaskOccurrenceDto>> GetTaskOccurrenceById(int id)
        {
            var taskOccurrence = await _taskTemplateService.GetTaskOccurrenceById(id);

            var taskOccurrenceDto = taskOccurrence.Adapt<TaskOccurrenceDto>();
            return Ok(taskOccurrenceDto);
        }

        /// <summary>
        /// Updates an existing task template and associated task occurrence.
        /// </summary>
        /// <param name="id">The ID of the task occurrence to update.</param>
        /// <param name="taskOccurrenceDto">The data transfer object containing the updated task template and occurrence details.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPut("UpdateTaskTemplateAndOccurrence/{id}")]
        public async Task<ActionResult> UpdateTaskTemplateAndOccurrence(int id, TaskOccurrenceDto taskOccurrenceDto)
        {
            var taskOccurrenceDomain = taskOccurrenceDto.Adapt<TaskOccurrence>();

            await _taskTemplateService.UpdateTaskTemplateAndOccurrence(id, taskOccurrenceDomain);

            return Ok();
        }

        /// <summary>
        /// Deletes a task template and its associated task occurrences.
        /// </summary>
        /// <param name="id">The ID of the task template to delete.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpDelete("DeleteTaskTemplateAndOccurrences/{id}")]
        public async Task<IActionResult> DeleteTaskTemplateAndOccurrences(int id)
        {
            await _taskTemplateService.DeleteTaskTemplateAndOccurrences(id);
            return NoContent();
        }

        /// <summary>
        /// Marks a task occurrence as complete.
        /// </summary>
        /// <param name="taskOccurrenceId">The ID of the task occurrence to mark as complete.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CompleteTaskOccurrence/{taskOccurrenceId}")]
        public async Task<IActionResult> CompleteTaskOccurrence(int taskOccurrenceId, [FromQuery, BindRequired] DateOnly localDate)
        {
            await _taskTemplateService.CompleteTaskOccurrence(taskOccurrenceId, localDate);

            return Ok();
        }

        /// <summary>
        /// Commits a task occurrence to a specific day or returns it to the group.
        /// </summary>
        /// <param name="taskOccurrenceDto">The data transfer object containing the commit day and task occurrence ID.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CommitTaskOccurrence")]
        public async Task<ActionResult> CommitTaskOccurrence(CommitTaskOccurrenceDto taskOccurrenceDto)
        {
            await _taskTemplateService.CommitTaskOccurrenceOrReturnToGroup(taskOccurrenceDto.CommitDay, taskOccurrenceDto.TaskOccurrenceId);

            return Ok();
        }

        /// <summary>
        /// Reorders a task template within a group.
        /// </summary>
        /// <param name="updateTaskTemplateIndexDto">The data transfer object containing the task template ID, new index, and recurrence flag.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("ReorderTaskTemplateInsideGroup")]
        public async Task<ActionResult> ReorderTaskTemplateInsideGroup(UpdateTaskTemplateIndexDto updateTaskTemplateIndexDto)
        {
            await _taskTemplateService.ReorderTaskTemplateInsideGroup(
            updateTaskTemplateIndexDto.TaskTemplateId,
            updateTaskTemplateIndexDto.NewIndex,
            updateTaskTemplateIndexDto.Recurring
                );

            return Ok();
        }

        /// <summary>
        /// Reorders a task occurrence within a group.
        /// </summary>
        /// <param name="updateTaskOccurrenceIndexDto">The data transfer object containing the task occurrence ID, commit day, and new index.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("ReorderTaskOccurrenceInsideGroup")]
        public async Task<ActionResult> ReorderTaskOccurrenceInsideGroup(UpdateTaskOccurrenceIndexDto updateTaskOccurrenceIndexDto)
        {
            await _taskTemplateService.ReorderTaskOccurrenceInsideGroup(
            updateTaskOccurrenceIndexDto.TaskOccurrenceId,
            updateTaskOccurrenceIndexDto.CommitDay,
            updateTaskOccurrenceIndexDto.NewIndex
                );

            return Ok();
        }

        /// <summary>
        /// Retrieves a list of one-time task occurrences.
        /// </summary>
        /// <returns>An ActionResult containing a list of one-time task occurrence data transfer objects.</returns>
        [HttpGet("GetOneTimeTaskOccurrences")]
        public async Task<ActionResult<IEnumerable<TaskOccurrenceDto>>> GetOneTimeTaskOccurrences([FromQuery, BindRequired] DateOnly localDate)
        {
            var taskOccurrences = await _taskTemplateService.GetActiveTaskOccurrences(false, localDate);
            var taskOccurrenceDtos = taskOccurrences.Adapt<List<TaskOccurrenceDto>>();

            return Ok(taskOccurrenceDtos);
        }

        /// <summary>
        /// Retrieves a list of recurring task occurrences.
        /// </summary>
        /// <returns>An ActionResult containing a list of recurring task occurrence data transfer objects.</returns>
        [HttpGet("GetRecurringTaskOccurrences")]
        public async Task<ActionResult<IEnumerable<TaskOccurrenceDto>>> GetRecurringTaskOccurrences([FromQuery, BindRequired] DateOnly localDate)
        {
            var taskOccurrences = await _taskTemplateService.GetActiveTaskOccurrences(true, localDate);
            var taskOccurrenceDtos = taskOccurrences.Adapt<List<TaskOccurrenceDto>>();

            return Ok(taskOccurrenceDtos);
        }

        /// <summary>
        /// Retrieves committed task occurrences for the next week, grouped by day.
        /// </summary>
        /// <returns>A list of WeekDayDto objects containing the committed task occurrences grouped by day.</returns>
        [HttpGet("GetCommittedTaskOccurrencesForNextWeek")]
        public async Task<IEnumerable<WeekDayDto>> GetCommittedTaskOccurrencesForNextWeek([FromQuery, BindRequired] DateOnly localDate)
        {
            var groupedTaskOccurrences = await _taskTemplateService.GetCommittedTaskOccurrencesForNextWeek(localDate);
            var weekDayDtos = groupedTaskOccurrences
                .Select(group => new WeekDayDto
                {
                    WeekDayDate = group.Key,
                    TaskOccurrences = group.Value.Select(taskOccurrence => taskOccurrence.Adapt<TaskOccurrenceDto>()).ToList()
                })
                .ToList();

            return weekDayDtos;
        }
    }
}
