using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUnaProgenyApi.Services;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// Provides endpoints for managing Kanban boards, including retrieving, creating, updating, and deleting boards.
    /// </summary>
    /// <remarks>This controller handles operations related to Kanban boards, such as fetching a specific
    /// board by its ID,  creating new boards, updating existing boards, and deleting boards. Access to these endpoints
    /// is restricted  based on user roles and permissions, as defined by the "UserOrClient" authorization policy.  The
    /// controller ensures that users have the appropriate access level to perform operations on a Kanban board  and
    /// validates their permissions against the associated progeny data.</remarks>
    /// <param name="kanbanBoardsService">The service for managing Kanban boards.</param>
    /// <param name="userAccessService">The service for validating user access levels.</param>
    /// <param name="progenyService">The service for managing progeny data.</param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class KanbanBoardsController(IKanbanBoardsService kanbanBoardsService, IUserAccessService userAccessService, IProgenyService progenyService) : ControllerBase
    {
        /// <summary>
        /// Retrieves a Kanban board by its unique identifier.
        /// </summary>
        /// <remarks>This method validates the user's access level before returning the Kanban board.  If
        /// the board is not found, a 404 Not Found response is returned. If the user does not  have sufficient access,
        /// the response will reflect the access validation result.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the Kanban board if found and accessible,  or an appropriate HTTP
        /// response indicating the result of the operation.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetKanbanBoard(int kanbanBoardId)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsService.GetKanbanBoardById(kanbanBoardId);
            if (kanbanBoard == null)
            {
                return NotFound();
            }
            
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);

            if (accessLevelResult.IsSuccess) return Ok(kanbanBoard);

            return accessLevelResult.ToActionResult();
        }

        /// <summary>
        /// Creates a new Kanban board and saves it to the system.
        /// </summary>
        /// <remarks>The user must have administrative access to the specified progeny to create a Kanban
        /// board.  The access level is validated before saving the Kanban board. If the validation fails, the
        /// appropriate error response is returned.</remarks>
        /// <param name="kanbanBoard">The Kanban board to be created. Must include valid data, including the associated progeny ID and access
        /// level.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.  Returns <see
        /// cref="BadRequestResult"/> if the input is invalid or the associated progeny does not exist.  Returns <see
        /// cref="UnauthorizedResult"/> if the user does not have administrative rights for the specified progeny. 
        /// Returns <see cref="OkObjectResult"/> containing the saved Kanban board if the operation is successful.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] KanbanBoard kanbanBoard)
        {
            if (kanbanBoard == null)
            {
                return BadRequest();
            }

            Progeny progeny = await progenyService.GetProgeny(kanbanBoard.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            KanbanBoard savedKanbanItem = await kanbanBoardsService.AddKanbanBoard(kanbanBoard);

            return Ok(savedKanbanItem);
        }

        /// <summary>
        /// Updates an existing Kanban board with the specified ID using the provided data.
        /// </summary>
        /// <remarks>This method validates the user's access level and ensures that the Kanban board
        /// exists before updating it.  The user must have administrative permissions for the associated progeny to
        /// perform this operation.</remarks>
        /// <param name="id">The unique identifier of the Kanban board to update.</param>
        /// <param name="kanbanBoard">The updated Kanban board data. The <see cref="KanbanBoard.KanbanBoardId"/> property must match the <paramref
        /// name="id"/>.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation.  Returns <see
        /// cref="BadRequestResult"/> if the input data is invalid, the Kanban board does not exist,  or the user does
        /// not have the required permissions.  Returns <see cref="UnauthorizedResult"/> if the user is not authorized
        /// to update the Kanban board.  Returns <see cref="OkObjectResult"/> containing the updated Kanban board on
        /// success.</returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] KanbanBoard kanbanBoard)
        {
            if (kanbanBoard == null || id != kanbanBoard.KanbanBoardId)
            {
                return BadRequest();
            }

            KanbanBoard existingKanbanBoard = await kanbanBoardsService.GetKanbanBoardById(id);
            if (existingKanbanBoard == null)
            {
                return BadRequest();
            }

            Progeny progeny = await progenyService.GetProgeny(existingKanbanBoard.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            kanbanBoard.ModifiedBy = User.GetUserId();
            kanbanBoard.ModifiedTime = DateTime.UtcNow;

            KanbanBoard resultKanbanItem = await kanbanBoardsService.UpdateKanbanBoard(kanbanBoard);
            return Ok(resultKanbanItem);
        }

        /// <summary>
        /// Deletes a Kanban board with the specified ID.
        /// </summary>
        /// <remarks>This method requires the user to have administrative access to the progeny associated
        /// with the Kanban board. The user's email is used to validate access permissions.</remarks>
        /// <param name="id">The unique identifier of the Kanban board to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the Kanban board with the specified ID does not
        /// exist.</description></item> <item><description><see cref="UnauthorizedResult"/> if the user does not have
        /// administrative permissions for the associated progeny.</description></item> <item><description><see
        /// cref="BadRequestResult"/> if the associated progeny could not be retrieved.</description></item>
        /// <item><description><see cref="OkObjectResult"/> containing the deleted Kanban board if the operation is
        /// successful.</description></item> </list></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            KanbanBoard existingKanbanBoard = await kanbanBoardsService.GetKanbanBoardById(id);
            if (existingKanbanBoard == null)
            {
                return NotFound();
            }
            
            Progeny progeny = await progenyService.GetProgeny(existingKanbanBoard.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return BadRequest();
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(existingKanbanBoard.ProgenyId, userEmail, existingKanbanBoard.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            KanbanBoard deletedKanbanItem = await kanbanBoardsService.DeleteKanbanBoard(existingKanbanBoard);
            return Ok(deletedKanbanItem);
        }
    }
}
