using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.KanbanServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.FamiliesServices;

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
    /// <param name="progenyService">The service for managing progeny data.</param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class KanbanBoardsController(IKanbanBoardsService kanbanBoardsService, IProgenyService progenyService, IFamiliesService familiesService,
        IUserInfoService userInfoService, IAccessManagementService accessManagementService) : ControllerBase
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
        [HttpGet]
        [Route("[action]/{kanbanBoardId:int}")]
        public async Task<IActionResult> GetKanbanBoard(int kanbanBoardId)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            KanbanBoard kanbanBoard = await kanbanBoardsService.GetKanbanBoardById(kanbanBoardId, currentUserInfo);
            if (kanbanBoard == null || kanbanBoard.KanbanBoardId == 0)
            {
                return NotFound();
            }
            
            return Ok(kanbanBoard);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetProgeniesKanbanBoardsList([FromBody] KanbanBoardsRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<Progeny> progenyList = [];
            List<Family> familyList = [];
            foreach (int progenyId in request.ProgenyIds)
            {
                Progeny progeny = await progenyService.GetProgeny(progenyId, currentUserInfo);
                if (progeny != null)
                {
                    progenyList.Add(progeny);
                }
            }

            foreach (int familyId in request.FamilyIds)
            {
                Family family = await familiesService.GetFamilyById(familyId, currentUserInfo);
                if (family != null)
                {
                    familyList.Add(family);
                }
            }

            if (request.Skip < 0) request.Skip = 0;

            List<KanbanBoard> kanbanBoards = [];
            if (progenyList.Count == 0) return NotFound();
            foreach (Progeny progeny in progenyList)
            {
                List<KanbanBoard> progenyKanbanBoards = await kanbanBoardsService.GetKanbanBoardsForProgenyOrFamily(progeny.Id, 0, currentUserInfo, request);
                kanbanBoards.AddRange(progenyKanbanBoards);
            }
            foreach (Family family in familyList)
            {
                List<KanbanBoard> familyKanbanBoards = await kanbanBoardsService.GetKanbanBoardsForProgenyOrFamily(0, family.FamilyId, currentUserInfo, request);
                kanbanBoards.AddRange(familyKanbanBoards);
            }

            KanbanBoardsResponse kanbanBoardsResponse = kanbanBoardsService.CreateKanbanBoardsResponse(kanbanBoards, request);

            return Ok(kanbanBoardsResponse);
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
            // Either ProgenyId or FamilyId must be set, but not both.
            if (kanbanBoard.ProgenyId > 0 && kanbanBoard.FamilyId > 0)
            {
                return BadRequest("A Kanban board must have either a ProgenyId or a FamilyId set, but not both.");
            }
            if (kanbanBoard.ProgenyId == 0 && kanbanBoard.FamilyId == 0)
            {
                return BadRequest("A Kanban board must have either a ProgenyId or a FamilyId set.");
            }

            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (kanbanBoard.ProgenyId > 0)
            {
                if (!await accessManagementService.HasProgenyPermission(kanbanBoard.ProgenyId, currentUserInfo, PermissionLevel.Add)) 
                {
                    return Unauthorized();
                }
            }
            
            if (kanbanBoard.FamilyId > 0)
            {
                if (!await accessManagementService.HasFamilyPermission(kanbanBoard.FamilyId, currentUserInfo, PermissionLevel.Add)) 
                {
                    return Unauthorized();
                }
            }
            
            KanbanBoard savedKanbanItem = await kanbanBoardsService.AddKanbanBoard(kanbanBoard, currentUserInfo);
            if (savedKanbanItem == null)
            {
                return Unauthorized();
            }

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
            // Either ProgenyId or FamilyId must be set, but not both.
            if (kanbanBoard == null || id != kanbanBoard.KanbanBoardId || (kanbanBoard.ProgenyId > 0 && kanbanBoard.FamilyId > 0))
            {
                return BadRequest();
            }

            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            kanbanBoard.ModifiedBy = User.GetUserId();
            kanbanBoard.ModifiedTime = DateTime.UtcNow;

            KanbanBoard resultKanbanItem = await kanbanBoardsService.UpdateKanbanBoard(kanbanBoard, currentUserInfo);
            if (resultKanbanItem == null)
            {
                return Unauthorized();
            }

            return Ok(resultKanbanItem);
        }

        /// <summary>
        /// Deletes a Kanban board with the specified ID.
        /// </summary>
        /// <remarks>This method requires the user to have administrative access to the progeny associated
        /// with the Kanban board. The user's email is used to validate access permissions.</remarks>
        /// <param name="id">The unique identifier of the Kanban board to delete.</param>
        /// <param name="hardDelete">If set to <see langword="true"/>, the Kanban board is permanently removed from the database; otherwise, it is marked as deleted.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the Kanban board with the specified ID does not
        /// exist.</description></item> <item><description><see cref="UnauthorizedResult"/> if the user does not have
        /// administrative permissions for the associated progeny.</description></item> <item><description><see
        /// cref="BadRequestResult"/> if the associated progeny could not be retrieved.</description></item>
        /// <item><description><see cref="OkObjectResult"/> containing the deleted Kanban board if the operation is
        /// successful.</description></item> </list></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] bool hardDelete = false)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            KanbanBoard existingKanbanBoard = await kanbanBoardsService.GetKanbanBoardById(id, currentUserInfo);
            if (existingKanbanBoard == null || existingKanbanBoard.KanbanBoardId == 0)
            {
                return NotFound();
            }
            
            KanbanBoard deletedKanbanItem = await kanbanBoardsService.DeleteKanbanBoard(existingKanbanBoard, currentUserInfo, hardDelete);
            if (deletedKanbanItem == null)
            {
                return Unauthorized();
            }

            return Ok(deletedKanbanItem);
        }
    }
}
