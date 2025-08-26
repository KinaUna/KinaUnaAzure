using System;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services.KanbanServices;
using KinaUnaProgenyApi.Services.TodosServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class KanbanItemsController(IKanbanItemsService kanbanItemsService, ITodosService todosService, IUserAccessService userAccessService) : Controller
    {
        [HttpPost]
        [Route("[action]")]
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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] KanbanItem kanbanItem)
        {
            if (kanbanItem == null)
            {
                return BadRequest();
            }
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            kanbanItem.TodoItem = await todosService.GetTodoItem(kanbanItem.TodoItemId);
            if (kanbanItem.TodoItem == null)
            {
                return BadRequest("The Kanban item must be linked to a valid Todo item.");
            }

            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanItem.TodoItem.ProgenyId, userEmail, kanbanItem.TodoItem.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();
            
            KanbanItem savedKanbanItem = await kanbanItemsService.AddKanbanItem(kanbanItem);

            return Ok(savedKanbanItem);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Update([FromBody] KanbanItem kanbanItem)
        {
            KanbanItem existingKanbanItem = await kanbanItemsService.GetKanbanItemById(kanbanItem.KanbanItemId);
            if (existingKanbanItem == null)
            {
                return BadRequest();
            }

            if (kanbanItem.TodoItem == null)
            {
                return BadRequest("The Kanban item must be linked to a valid Todo item.");
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(kanbanItem.TodoItem.ProgenyId, userEmail, kanbanItem.TodoItem.AccessLevel);
            if (!accessLevelResult.IsSuccess) return accessLevelResult.ToActionResult();

            kanbanItem.ModifiedBy = User.GetUserId();
            kanbanItem.ModifiedTime = DateTime.UtcNow;

            KanbanItem resultKanbanItem = await kanbanItemsService.UpdateKanbanItem(kanbanItem);
            return Ok(resultKanbanItem);
        }

    }
}
