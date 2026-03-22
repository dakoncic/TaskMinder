using Core.DomainModels;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyFeatures.DTO;
using System.Net.Mime;

namespace MyFeatures.Controllers
{
    [ApiController]
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
        /// <param name="taskOccurrenceDto">The DTO containing task occurrence and template details.</param>
        /// <returns>An ActionResult indicating the operation result (Ok on success).</returns>
        [HttpPost("CreateTaskTemplateAndOccurrence")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> CreateTaskTemplateAndOccurrence(TaskOccurrenceDto taskOccurrenceDto)
        {
            var taskOccurrenceDomain = taskOccurrenceDto.Adapt<TaskOccurrence>();

            await _taskTemplateService.CreateTaskTemplateAndOccurrence(taskOccurrenceDomain);

            return Ok();
        }

        /// <summary>
        /// Retrieves a task occurrence by its ID.
        /// </summary>
        /// <param name="id">The ID of the task occurrence to retrieve.</param>
        /// <returns>An ActionResult containing the TaskOccurrenceDto when found.</returns>
        [HttpGet("GetTaskOccurrenceById/{id}")]
        [ProducesResponseType(typeof(TaskOccurrenceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<ActionResult<TaskOccurrenceDto>> GetTaskOccurrenceById(int id)
        {
            return Ok((await _taskTemplateService.GetTaskOccurrenceById(id)).Adapt<TaskOccurrenceDto>());
        }

        /// <summary>
        /// Updates an existing task template and associated task occurrence.
        /// </summary>
        /// <param name="id">The ID of the task template to update.</param>
        /// <param name="taskOccurrenceDto">The DTO containing updated task occurrence and template details.</param>
        /// <returns>An ActionResult indicating the operation result (Ok on success).</returns>
        [HttpPut("UpdateTaskTemplateAndOccurrence/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> UpdateTaskTemplateAndOccurrence(int id, TaskOccurrenceDto taskOccurrenceDto)
        {
            var taskOccurrenceDomain = taskOccurrenceDto.Adapt<TaskOccurrence>();

            await _taskTemplateService.UpdateTaskTemplateAndOccurrence(id, taskOccurrenceDomain);

            return Ok();
        }

        /// <summary>
        /// Deletes a task template and its associated task occurrences.
        /// </summary>
        /// <param name="id">The ID of the task template to delete.</param>
        /// <returns>NoContent on success.</returns>
        [HttpDelete("DeleteTaskTemplateAndOccurrences/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> DeleteTaskTemplateAndOccurrences(int id)
        {
            await _taskTemplateService.DeleteTaskTemplateAndOccurrences(id);
            return NoContent();
        }

        /// <summary>
        /// Marks a task occurrence as complete.
        /// </summary>
        /// <param name="taskOccurrenceId">The ID of the task occurrence to mark complete.</param>
        /// <param name="localDate">The local date to apply the completion to.</param>
        /// <returns>An ActionResult indicating the operation result (Ok on success).</returns>
        [HttpPost("CompleteTaskOccurrence/{taskOccurrenceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> CompleteTaskOccurrence(int taskOccurrenceId, [FromQuery, BindRequired] DateOnly localDate)
        {
            await _taskTemplateService.CompleteTaskOccurrence(taskOccurrenceId, localDate);

            return Ok();
        }

        /// <summary>
        /// Commits a task occurrence to a specific day or returns it to the group.
        /// </summary>
        /// <param name="taskOccurrenceDto">The DTO containing commit details (CommitDay and TaskOccurrenceId).</param>
        /// <returns>An ActionResult indicating the operation result (Ok on success).</returns>
        [HttpPost("CommitTaskOccurrence")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> CommitTaskOccurrence(CommitTaskOccurrenceDto taskOccurrenceDto)
        {
            await _taskTemplateService.CommitTaskOccurrenceOrReturnToGroup(taskOccurrenceDto.CommitDay, taskOccurrenceDto.TaskOccurrenceId);

            return Ok();
        }

        /// <summary>
        /// Reorders a task template within a group.
        /// </summary>
        /// <param name="updateTaskTemplateIndexDto">The DTO containing TaskTemplateId, NewIndex, and Recurring flag used to reorder a template within its group.</param>
        /// <returns>An ActionResult indicating the operation result (Ok on success).</returns>
        [HttpPost("ReorderTaskTemplateInsideGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ReorderTaskTemplateInsideGroup(UpdateTaskTemplateIndexDto updateTaskTemplateIndexDto)
        {
            await _taskTemplateService.ReorderTaskTemplateInsideGroup(
                updateTaskTemplateIndexDto.TaskTemplateId,
                updateTaskTemplateIndexDto.NewIndex,
                updateTaskTemplateIndexDto.Recurring);

            return Ok();
        }

        /// <summary>
        /// Reorders a task occurrence within a group.
        /// </summary>
        /// <param name="updateTaskOccurrenceIndexDto">The DTO containing TaskOccurrenceId, CommitDay, and NewIndex used to reorder an occurrence within its group.</param>
        /// <returns>An ActionResult indicating the operation result (Ok on success).</returns>
        [HttpPost("ReorderTaskOccurrenceInsideGroup")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ReorderTaskOccurrenceInsideGroup(UpdateTaskOccurrenceIndexDto updateTaskOccurrenceIndexDto)
        {
            await _taskTemplateService.ReorderTaskOccurrenceInsideGroup(
                updateTaskOccurrenceIndexDto.TaskOccurrenceId,
                updateTaskOccurrenceIndexDto.CommitDay,
                updateTaskOccurrenceIndexDto.NewIndex);

            return Ok();
        }

        /// <summary>
        /// Retrieves a list of one-time task occurrences.
        /// </summary>
        /// <param name="localDate">The local date (DateOnly) for which to retrieve one-time task occurrences.</param>
        /// <returns>An ActionResult containing a list of TaskOccurrenceDto.</returns>
        [HttpGet("GetOneTimeTaskOccurrences")]
        [ProducesResponseType(typeof(IEnumerable<TaskOccurrenceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        public async Task<ActionResult<IEnumerable<TaskOccurrenceDto>>> GetOneTimeTaskOccurrences([FromQuery, BindRequired] DateOnly localDate)
        {
            var taskOccurrences = await _taskTemplateService.GetActiveTaskOccurrences(false, localDate);
            return Ok(taskOccurrences.Adapt<List<TaskOccurrenceDto>>());
        }

        /// <summary>
        /// Retrieves a list of recurring task occurrences.
        /// </summary>
        /// <param name="localDate">The local date (DateOnly) for which to retrieve recurring task occurrences.</param>
        /// <returns>An ActionResult containing a list of TaskOccurrenceDto.</returns>
        [HttpGet("GetRecurringTaskOccurrences")]
        [ProducesResponseType(typeof(IEnumerable<TaskOccurrenceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        public async Task<ActionResult<IEnumerable<TaskOccurrenceDto>>> GetRecurringTaskOccurrences([FromQuery, BindRequired] DateOnly localDate)
        {
            var taskOccurrences = await _taskTemplateService.GetActiveTaskOccurrences(true, localDate);
            return Ok(taskOccurrences.Adapt<List<TaskOccurrenceDto>>());
        }

        /// <summary>
        /// Retrieves committed task occurrences for the next week, grouped by day.
        /// </summary>
        /// <param name="localDate">The local date (DateOnly) from which the next week is calculated for committed occurrences.</param>
        /// <returns>An ActionResult containing a list of WeekDayDto grouped by day.</returns>
        [HttpGet("GetCommittedTaskOccurrencesForNextWeek")]
        [ProducesResponseType(typeof(IEnumerable<WeekDayDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
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
