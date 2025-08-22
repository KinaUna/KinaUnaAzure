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
    /// <summary>
    /// Provides endpoints for managing subtasks associated with Todo items.
    /// </summary>
    /// <remarks>This controller allows users to perform operations such as retrieving, creating, updating,
    /// and deleting subtasks. Access to these endpoints is restricted based on user roles and permissions, as defined
    /// by the "UserOrClient" policy.</remarks>
    /// <param name="userAccessService">User access service to validate user permissions.</param>
    /// <param name="subtasksService">Service for managing subtasks.</param>
    /// <param name="todosService">TodoItems service to manage Todo items.</param>
    /// <param name="progenyService">Provides access to Progeny data.</param>
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
        /// <summary>
        /// Retrieves the subtasks associated with a specified parent TodoItem.
        /// </summary>
        /// <remarks>This method validates the existence of the parent TodoItem and its association with
        /// the specified Progeny. It also ensures that the user has the appropriate access level to view the subtasks.
        /// The response includes the subtasks with their associated Progeny information.</remarks>
        /// <param name="request">The request containing the parent TodoItem ID and associated Progeny ID.</param>
        /// <returns>An <see cref="IActionResult"/> containing the list of subtasks for the specified parent TodoItem if the
        /// request is valid. Returns <see cref="NotFoundResult"/> if the parent TodoItem or Progeny does not exist, or
        /// <see cref="BadRequestResult"/> if the parent TodoItem does not belong to the specified Progeny.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetSubtasksForTodoItem([FromBody] SubtasksRequest request)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            
            // Check if the TodoItem exists
            TodoItem existingTodoItem = await todosService.GetTodoItem(request.ParentTodoItemId);
            if (existingTodoItem == null)
            {
                return NotFound("TodoItem not found.");
            }

            request.ProgenyId = existingTodoItem.ProgenyId;
            
            // Check if the user has access to the TodoItem
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(request.ProgenyId, userEmail, existingTodoItem.AccessLevel);
            if (accessLevelResult.IsFailure)
            {
                return accessLevelResult.ToActionResult();
            }
            
            Progeny progeny = await progenyService.GetProgeny(request.ProgenyId);
            if (progeny == null)
            {
                return NotFound("Progeny not found.");
            }

            
            List<TodoItem> subtasks = await subtasksService.GetSubtasksForTodoItem(request.ParentTodoItemId);

            foreach (TodoItem subtask in subtasks)
            {
                subtask.Progeny = progeny; // Ensure the Progeny is set for each subtask
            }

            SubtasksResponse subtasksResponse = subtasksService.CreateSubtaskResponseForTodoItem(subtasks, request);

            // Return the list of subtasks
            return Ok(subtasksResponse);
        }
        
        /// <summary>
        /// Retrieves a subtask by its unique identifier.
        /// </summary>
        /// <remarks>This method validates the user's access level before returning the subtask.  If the
        /// user does not have sufficient access rights, the response will indicate the access issue.</remarks>
        /// <param name="id">The unique identifier of the subtask to retrieve. Must be a positive integer.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.  Returns <see
        /// cref="OkObjectResult"/> with the subtask if found and the user has sufficient access rights.  Returns <see
        /// cref="NotFoundResult"/> if the subtask does not exist.  Returns an appropriate error response if the user
        /// lacks the required access level.</returns>
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
        
        /// <summary>
        /// Creates a new to-do item and associates it with the specified progeny.
        /// </summary>
        /// <remarks>The user must have administrative access to the specified progeny to create a to-do
        /// item.  If the <paramref name="value"/> does not include a unique identifier (<see cref="TodoItem.UId"/>),
        /// one will be generated automatically.</remarks>
        /// <param name="value">The to-do item to create. The item must include the associated progeny ID.</param>
        /// <returns>An <see cref="IActionResult"/> representing the result of the operation.  Returns <see
        /// cref="UnauthorizedResult"/> if the user is not authorized to modify the specified progeny. Returns <see
        /// cref="BadRequestResult"/> if the progeny does not exist, the to-do item is invalid, or the creation fails.
        /// Returns <see cref="OkObjectResult"/> containing the created to-do item if the operation is successful.</returns>
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

            TodoItem parentTodoItem = await todosService.GetTodoItem(value.ParentTodoItemId);
            if (parentTodoItem == null)
            {
                return BadRequest("Parent TodoItem not found");
            }
            // Ensure the subtask inherits the access level of the parent TodoItem
            value.AccessLevel = parentTodoItem.AccessLevel;
            value.ProgenyId = parentTodoItem.ProgenyId;

            TodoItem subtask = await subtasksService.AddSubtask(value);

            if (subtask == null)
            {
                return BadRequest();
            }
            
            return Ok(subtask);
        }

        /// <summary>
        /// Updates an existing subtask with the specified ID using the provided data.
        /// </summary>
        /// <remarks>This method requires the user to have administrative permissions for the progeny
        /// associated with the subtask. If the <paramref name="value"/> does not have a unique identifier (<c>UId</c>),
        /// a new GUID will be generated.</remarks>
        /// <param name="id">The unique identifier of the subtask to update.</param>
        /// <param name="value">The updated <see cref="TodoItem"/> data to apply to the subtask.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the subtask with the specified ID does not
        /// exist.</description></item> <item><description><see cref="UnauthorizedResult"/> if the user is not
        /// authorized to modify the subtask.</description></item> <item><description><see cref="BadRequestResult"/> if
        /// the provided data is invalid or the associated progeny is not found.</description></item>
        /// <item><description><see cref="OkObjectResult"/> containing the updated subtask if the operation is
        /// successful.</description></item> </list></returns>
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

            TodoItem parentTodoItem = await todosService.GetTodoItem(value.ParentTodoItemId);
            if (parentTodoItem == null)
            {
                return BadRequest("Parent TodoItem not found.");
            }

            // Ensure the subtask inherits the access level of the parent TodoItem
            value.AccessLevel = parentTodoItem.AccessLevel;
            value.ProgenyId = parentTodoItem.ProgenyId;

            subtask = await subtasksService.UpdateSubtask(value);

            return Ok(subtask);
        }

        /// <summary>
        /// Deletes a subtask with the specified identifier.
        /// </summary>
        /// <remarks>This method requires the user to have administrative access to the associated progeny
        /// of the subtask. If the user does not have the necessary permissions, the operation will return an <see
        /// cref="UnauthorizedResult"/>.</remarks>
        /// <param name="id">The unique identifier of the subtask to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the subtask does not exist.</description></item>
        /// <item><description><see cref="UnauthorizedResult"/> if the user does not have permission to delete the
        /// subtask.</description></item> <item><description><see cref="BadRequestResult"/> if the request is invalid or
        /// the deletion fails.</description></item> <item><description><see cref="NoContentResult"/> if the subtask is
        /// successfully deleted.</description></item> </list></returns>
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
