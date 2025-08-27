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

        [HttpGet]
        [Route("[action]/{kanbanBoardId:int}")]
        public async Task<IActionResult> GetKanbanItemsForBoard(int kanbanBoardId)
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
                List<KanbanItem> kanbanItems = await kanbanItemsService.GetKanbanItemsForBoard(kanbanBoardId);
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

            return Ok(savedKanbanItem);
        }

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
            return Ok(resultKanbanItem);
        }

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
            return Ok(deletedKanbanItem);
        }

    }
}
