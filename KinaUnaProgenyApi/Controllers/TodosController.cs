using System;
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
        /// <summary>
        /// Returns a list of to-do items for the specified progenies.
        /// </summary>
        /// <param name="request">TodoItemsRequest with the parameters and filters for retrieving TodoItems.</param>
        /// <returns>TodoItemsResponse containing the list of TodoItems and associated Progeny.</returns>
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
                List<TodoItem> progenyTodos = await todosService.GetTodosForProgeny(progeny.Id, userAccess.AccessLevel, request);
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
        
        /// <summary>
        /// Retrieves a specific to-do item by its unique identifier.
        /// </summary>
        /// <remarks>The method validates the user's access level for the requested to-do item before
        /// returning it. If the user lacks the required access, the response will indicate the appropriate
        /// error.</remarks>
        /// <param name="id">The unique identifier of the to-do item to retrieve. Must be a positive integer.</param>
        /// <returns>An <see cref="IActionResult"/> containing the requested to-do item if found and accessible to the user.
        /// Returns <see cref="NotFoundResult"/> if the item does not exist, or an appropriate HTTP response if the user
        /// does not have sufficient access rights.</returns>
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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TodoItem value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            if (value.DueDate == DateTime.MinValue)
            {
                value.DueDate = DateTime.UtcNow;
            }

            value.CreatedBy = User.GetUserId();

            TodoItem todoItem = await todosService.AddTodoItem(value);

            if (todoItem == null)
            {
                return BadRequest();
            }

            // Add TimeLineItem for the new TodoItem

            // Send notifications about the new TodoItem


            return Ok(todoItem);
        }
    }
}
