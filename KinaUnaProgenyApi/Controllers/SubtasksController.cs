using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
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
    public class SubtasksController(
        IUserAccessService userAccessService,
        ISubtasksService subtasksService,
        ITodosService todosService,
        IProgenyService progenyService) : ControllerBase
    {
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetSubtasksForTodoItem([FromBody] TodoItem todoItem)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (todoItem == null || todoItem.ProgenyId <= 0)
            {
                return BadRequest("Invalid TodoItem data.");
            }
            // Check if the TodoItem exists
            TodoItem existingTodoItem = await todosService.GetTodoItem(todoItem.TodoItemId);
            if (existingTodoItem == null)
            {
                return NotFound("TodoItem not found.");
            }
            // Check if the TodoItem belongs to the Progeny
            if (existingTodoItem.ProgenyId != todoItem.ProgenyId)
            {
                return BadRequest("TodoItem does not belong to the specified Progeny.");
            }

            // Check if the user has access to the TodoItem
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(todoItem.ProgenyId, userEmail, existingTodoItem.AccessLevel);
            if (accessLevelResult.IsFailure)
            {
                return accessLevelResult.ToActionResult();
            }
            
            Progeny progeny = await progenyService.GetProgeny(todoItem.ProgenyId);
            if (progeny == null)
            {
                return NotFound("Progeny not found.");
            }

            
            List<TodoItem> subtasks = await subtasksService.GetSubtasksForTodoItem(todoItem.TodoItemId);

            foreach (TodoItem subtask in subtasks)
            {
                subtask.Progeny = progeny; // Ensure the Progeny is set for each subtask
            }

            // Return the list of subtasks
            return Ok(subtasks);
        }
        
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSubtask(int id)
        {
            TodoItem result = await subtasksService.GetSubtask(id);
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

            value.CreatedBy = User.GetUserId();
            if (string.IsNullOrWhiteSpace(value.UId))
            {
                value.UId = Guid.NewGuid().ToString();
            }

            TodoItem subtask = await subtasksService.AddSubtask(value);

            if (subtask == null)
            {
                return BadRequest();
            }
            
            return Ok(subtask);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] TodoItem value)
        {
            TodoItem subtask = await subtasksService.GetSubtask(id);
            if (subtask == null)
            {
                return NotFound();
            }

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

            value.ModifiedBy = User.GetUserId();
            if (string.IsNullOrWhiteSpace(value.UId))
            {
                value.UId = Guid.NewGuid().ToString();
            }

            subtask = await subtasksService.UpdateSubtask(value);

            return Ok(subtask);
        }

        
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            TodoItem subtask = await subtasksService.GetSubtask(id);
            if (subtask == null)
            {
                return NotFound();
            }

            // Check if the user has access to the Progeny associated with the TodoItem
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            Progeny progeny = await progenyService.GetProgeny(subtask.ProgenyId);
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
            
            subtask.ModifiedBy = User.GetUserId();
            bool isDeleted = await subtasksService.DeleteSubtask(subtask);
            if (!isDeleted)
            {
                return BadRequest();
            }
            return NoContent();
        }
    }
}
