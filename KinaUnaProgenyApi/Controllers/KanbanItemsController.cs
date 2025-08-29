using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// Controller for managing Kanban items.
    /// </summary>
    /// <param name="kanbanItemsService">The Kanban items service.</param>
    /// <param name="kanbanBoardsService">The Kanban boards service.</param>
    /// <param name="todosService">The Todos service.</param>
    /// <param name="userAccessService">The user access service.</param>
    /// <param name="progenyService">The progeny service.</param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class KanbanItemsController(IKanbanItemsService kanbanItemsService,
        IKanbanBoardsService kanbanBoardsService,
        ITodosService todosService,
        IUserAccessService userAccessService,
        IProgenyService progenyService) : Controller
    {
        /// <summary>
        /// Retrieves a Kanban item by its unique identifier.
        /// </summary>
        /// <remarks>The method validates the user's access level to the associated Todo item before
        /// returning the Kanban item.  If the user does not have sufficient access, the response will reflect the
        /// access validation result.</remarks>
        /// <param name="kanbanItemId">The unique identifier of the Kanban item to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing the Kanban item if found and accessible, with the associated TodoItem.  Returns <see
        /// cref="NotFoundResult"/> if the Kanban item does not exist,  <see cref="BadRequestResult"/> if the Kanban
        /// item is not linked to a valid TodoItem,  or an appropriate HTTP response based on the user's access level.</returns>
        [HttpGet]
        [Route("[action]/{kanbanItemId:int}")]
        public async Task<IActionResult> GetKanbanItem(int kanbanItemId)
        {
            KanbanItem kanbanItem = await kanbanItemsService.GetKanbanItemById(kanbanItemId);
            if (kanbanItem == null)
            {
                return NotFound();
            }
            if (kanbanItem.TodoItem == null)
            {
                return BadRequest("The Kanban item is not linked to a valid Todo item.");
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanItem.TodoItem.ProgenyId, userEmail, kanbanItem.TodoItem.AccessLevel);

            if (accessLevelResult.IsSuccess) return Ok(kanbanItem);

            return accessLevelResult.ToActionResult();
        }

        /// <summary>
        /// Retrieves the list of Kanban items associated with a specific Kanban board, filtered by the user's access
        /// level.
        /// </summary>
        /// <remarks>This method validates the user's access level for the specified Kanban board and its
        /// associated items. Only items that the user is authorized to view are included in the response. If the Kanban
        /// board does not exist, a 404 Not Found response is returned. If the user does not have sufficient access to
        /// the board, the appropriate HTTP status code is returned based on the access validation result.</remarks>
        /// <param name="kanbanBoardId">The unique identifier of the Kanban board for which to retrieve items.</param>
        /// <param name="includeDeleted">If set to <see langword="true"/>, items marked as deleted will be included in the results.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of Kanban items that the user has access to, with the associated TodoItem, or an
        /// appropriate HTTP status code if the board is not found or the user lacks sufficient access rights.</returns>
        [HttpGet]
        [Route("[action]/{kanbanBoardId:int}/{includeDeleted:bool}")]
        public async Task<IActionResult> GetKanbanItemsForBoard(int kanbanBoardId, bool includeDeleted = false)
        {
            KanbanBoard kanbanBoard = await kanbanBoardsService.GetKanbanBoardById(kanbanBoardId);
            if (kanbanBoard == null)
            {
                return NotFound();
            }
            
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanBoard.ProgenyId, userEmail, kanbanBoard.AccessLevel);

            if (accessLevelResult.IsSuccess)
            {
                List<KanbanItem> kanbanItems = await kanbanItemsService.GetKanbanItemsForBoard(kanbanBoardId, includeDeleted);
                List<KanbanItem> allowedKanbanItems = [];
                foreach (KanbanItem kanbanItem in kanbanItems)
                {
                    CustomResult<int> itemAccessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanItem.TodoItem.ProgenyId, userEmail, kanbanItem.TodoItem.AccessLevel);
                    if (itemAccessLevelResult.IsSuccess)
                    {
                        allowedKanbanItems.Add(kanbanItem);
                    }
                }

                return Ok(allowedKanbanItems);
            }

            return accessLevelResult.ToActionResult();
        }

        /// <summary>
        /// Creates a new Kanban item and associates it with an existing Todo item.
        /// </summary>
        /// <remarks>The method validates the provided Kanban item, ensuring it is linked to a valid TodoItem
        /// and that the user has  the necessary permissions to create the Kanban item. If the validation fails, an
        /// appropriate HTTP status code  is returned. Otherwise, the Kanban item is saved and returned in the
        /// response.</remarks>
        /// <param name="kanbanItem">The Kanban item to be created. Must include a valid TodoItem ID.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.  Returns <see
        /// cref="BadRequestResult"/> if the input is invalid or the associated Todo item is not found.  Returns <see
        /// cref="UnauthorizedResult"/> if the user does not have the required permissions.  Returns <see
        /// cref="OkObjectResult"/> containing the created Kanban item, with the associated TodoItem, if the operation is successful.</returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] KanbanItem kanbanItem)
        {
            if (kanbanItem == null)
            {
                return BadRequest();
            }
            kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId);
            if (kanbanItem.TodoItem == null)
            {
                return BadRequest("The Kanban item must be linked to a valid Todo item.");
            }

            Progeny progeny = await progenyService.GetProgeny(kanbanItem.TodoItem.ProgenyId);
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
            
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanItem.TodoItem.ProgenyId, userEmail, kanbanItem.TodoItem.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();
            
            KanbanItem savedKanbanItem = await kanbanItemsService.AddKanbanItem(kanbanItem);
            savedKanbanItem.TodoItem = kanbanItem.TodoItem;

            return Ok(savedKanbanItem);
        }

        /// <summary>
        /// Updates an existing Kanban item with the specified ID.
        /// </summary>
        /// <remarks>This method validates the provided Kanban item and ensures that the user has the
        /// necessary permissions to update it.  The Kanban item must be linked to a valid Todo item, and the user must
        /// have administrative access to the associated progeny.</remarks>
        /// <param name="id">The unique identifier of the Kanban item to update.</param>
        /// <param name="kanbanItem">The updated Kanban item data. The <see cref="KanbanItem.KanbanItemId"/> property must match the <paramref
        /// name="id"/> parameter.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="BadRequestResult"/> if the input is invalid, the Kanban item does not exist,
        /// or the item is not linked to a valid Todo item.</description></item> <item><description><see
        /// cref="UnauthorizedResult"/> if the user does not have sufficient permissions to update the Kanban
        /// item.</description></item> <item><description><see cref="OkObjectResult"/> containing the updated Kanban
        /// item, with the associated TodoItem, if the operation is successful.</description></item> </list></returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] KanbanItem kanbanItem)
        {
            if (kanbanItem == null || id != kanbanItem.KanbanItemId)
            {
                return BadRequest();
            }

            KanbanItem existingKanbanItem = await kanbanItemsService.GetKanbanItemById(id);
            if (existingKanbanItem == null)
            {
                return BadRequest();
            }

            if (kanbanItem.TodoItem == null)
            {
                return BadRequest("The Kanban item must be linked to a valid Todo item.");
            }

            Progeny progeny = await progenyService.GetProgeny(kanbanItem.TodoItem.ProgenyId);
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

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanItem.TodoItem.ProgenyId, userEmail, kanbanItem.TodoItem.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            kanbanItem.ModifiedBy = User.GetUserId();
            kanbanItem.ModifiedTime = DateTime.UtcNow;

            KanbanItem resultKanbanItem = await kanbanItemsService.UpdateKanbanItem(kanbanItem);
            resultKanbanItem.TodoItem = kanbanItem.TodoItem;

            return Ok(resultKanbanItem);
        }

        /// <summary>
        /// Deletes the specified Kanban item by its unique identifier.
        /// </summary>
        /// <remarks>This method validates the user's access level and ensures the Kanban item is linked
        /// to a valid TodoItem  before performing the deletion. The user must have administrative access to the
        /// associated progeny to delete the item.</remarks>
        /// <param name="id">The unique identifier of the Kanban item to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the Kanban item does not exist.</description></item>
        /// <item><description><see cref="BadRequestResult"/> if the Kanban item is not linked to a valid Todo item or
        /// if the associated progeny is invalid.</description></item> <item><description><see
        /// cref="UnauthorizedResult"/> if the user does not have administrative access to the associated
        /// progeny.</description></item> <item><description><see cref="OkObjectResult"/> containing the deleted Kanban
        /// item, with the associated TodoItem, if the operation is successful.</description></item> </list></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            KanbanItem existingKanbanItem = await kanbanItemsService.GetKanbanItemById(id);
            if (existingKanbanItem == null)
            {
                return NotFound();
            }
            if (existingKanbanItem.TodoItem == null)
            {
                return BadRequest("The Kanban item is not linked to a valid Todo item.");
            }

            Progeny progeny = await progenyService.GetProgeny(existingKanbanItem.TodoItem.ProgenyId);
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
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(existingKanbanItem.TodoItem.ProgenyId, userEmail, existingKanbanItem.TodoItem.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            KanbanItem deletedKanbanItem = await kanbanItemsService.DeleteKanbanItem(existingKanbanItem);
            deletedKanbanItem.TodoItem = existingKanbanItem.TodoItem;

            return Ok(deletedKanbanItem);
        }

    }
}
