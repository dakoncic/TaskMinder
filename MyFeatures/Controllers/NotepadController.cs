using Core.DomainModels;
using Core.Interfaces;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MyFeatures.DTO;
using System.Net.Mime;

namespace MyFeatures.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotepadController : ControllerBase
    {
        private readonly INotepadService _notepadService;

        public NotepadController(INotepadService notepadService)
        {
            _notepadService = notepadService;
        }

        /// <summary>
        /// Retrieves all notepads.
        /// </summary>
        /// <returns>An ActionResult containing a list of NotepadDto objects.</returns>
        [HttpGet("GetAllNotepads")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<NotepadDto>))]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
        public async Task<ActionResult<IEnumerable<NotepadDto>>> GetAllNotepads()
        {
            var notepads = await _notepadService.GetAll();
            var notepadDtos = notepads.Adapt<List<NotepadDto>>();

            return Ok(notepadDtos);
        }

        /// <summary>
        /// Creates a new notepad.
        /// </summary>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPost("CreateNotepad")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NotepadDto>> CreateNotepad()
        {
            await _notepadService.Create();

            return Ok();
        }

        /// <summary>
        /// Updates an existing notepad.
        /// </summary>
        /// <param name="id">The ID of the notepad to update.</param>
        /// <param name="notepadDto">The data transfer object containing the updated notepad details.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpPut("UpdateNotepad/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
        public async Task<ActionResult<NotepadDto>> UpdateNotepad(int id, NotepadDto notepadDto)
        {
            var notepadDomain = notepadDto.Adapt<Notepad>();
            await _notepadService.Update(id, notepadDomain);

            return Ok();
        }

        /// <summary>
        /// Deletes a notepad.
        /// </summary>
        /// <param name="id">The ID of the notepad to delete.</param>
        /// <returns>An ActionResult representing the result of the operation.</returns>
        [HttpDelete("DeleteNotepad/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, MediaTypeNames.Application.Json)]
        public async Task<IActionResult> DeleteNotepad(int id)
        {
            await _notepadService.Delete(id);
            return Ok();
        }
    }
}
