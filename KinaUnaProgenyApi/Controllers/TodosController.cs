using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Services.TodosServices;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TodosController(IProgenyService progenyService, IUserAccessService userAccessService, ITodosService todosService) : ControllerBase
    {

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Progenies([FromBody] TodoItemsRequest request)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            List<Progeny> progenyList = [];
            foreach (int progenyId in request.ProgenyIds)
            {
                Progeny progeny = await progenyService.GetProgeny(progenyId);
                if (progeny != null)
                {
                    UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                    if (userAccess != null)
                    {
                        progenyList.Add(progeny);
                    }
                }
            }

            List<TodoItem> todoItems = [];

            if (progenyList.Count == 0) return NotFound();
            foreach (Progeny progeny in progenyList)
            {
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progeny.Id, userEmail);
                List<TodoItem> progenyTodos = await todosService.GetTodosForProgeny(progeny.Id, userAccess.AccessLevel, request.StartDate, request.EndDate);
                todoItems.AddRange(progenyTodos);
            }

            TodoItemsResponse todoItemsResponse = new()
            {
                TodoItems = todoItems,
                ProgenyList = progenyList,
                TodoItemsRequest = request
            };

            return Ok(todoItemsResponse);
        }
        
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTodoItem(int id)
        {
            TodoItem result = await todosService.GetTodoItem(id);
            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(result.ProgenyId, userEmail, result.AccessLevel);
            
            if (accessLevelResult.IsSuccess) return Ok(result);

            return accessLevelResult.ToActionResult();

        }
    }
}
