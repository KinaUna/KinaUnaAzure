using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.TodosServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// Provides API endpoints for managing to-do items associated with progenies.
    /// </summary>
    /// <remarks>This controller includes methods for retrieving, creating, updating, and deleting to-do
    /// items. It enforces user access policies to ensure that only authorized users can perform operations on to-do
    /// items. The controller also integrates with notification services to inform users about changes to to-do
    /// items.</remarks>
    /// <param name="progenyService"></param>
    /// <param name="todosService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="timelineService"></param>
    /// <param name="azureNotifications"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TodosController(
        IProgenyService progenyService,
        ITodosService todosService,
        IUserInfoService userInfoService,
        ITimelineService timelineService,
        IAzureNotifications azureNotifications,
        IWebNotificationsService webNotificationsService,
        IAccessManagementService accessManagementService) : ControllerBase
    {
        /// <summary>
        /// Returns a list of to-do items for the specified progenies.
        /// </summary>
        /// <param name="request">TodoItemsRequest with the parameters and filters for retrieving TodoItems.</param>
        /// <returns>TodoItemsResponse containing the list of TodoItems and associated Progeny.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetProgeniesTodoItemsList([FromBody] TodoItemsRequest request)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            if (request.Skip < 0) request.Skip = 0;

            List<TodoItem> todoItems = [];

            foreach (int progenyId in request.ProgenyIds)
            {
                List<TodoItem> progenyTodos = await todosService.GetTodosForProgeny(progenyId, currentUserInfo, request);
                todoItems.AddRange(progenyTodos);
            }

            TodoItemsResponse todoItemsResponse = todosService.CreateTodoItemsResponseForTodoPage(todoItems, request);
            
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TodoItem result = await todosService.GetTodoItem(id, currentUserInfo);
            if (result == null) return NotFound();
            
            return Ok(result);
        }

        /// <summary>
        /// Creates a new to-do item for the specified progeny and adds it to the system.
        /// </summary>
        /// <remarks>This method performs the following steps: <list type="number">
        /// <item><description>Validates that the user is authorized to add a to-do item for the specified
        /// progeny.</description></item> <item><description>Sets the <see cref="TodoItem.DueDate"/> to the current UTC
        /// time if it is not provided.</description></item> <item><description>Sets the <see
        /// cref="TodoItem.CreatedBy"/> property to the ID of the current user.</description></item>
        /// <item><description>Creates the to-do item and adds it to the system.</description></item>
        /// <item><description>Creates a timeline entry for the new to-do item and sends notifications to relevant
        /// users.</description></item> </list></remarks>
        /// <param name="value">The <see cref="TodoItem"/> object containing the details of the to-do item to be created.  The <see
        /// cref="TodoItem.ProgenyId"/> property must specify the ID of the progeny associated with the to-do item.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="UnauthorizedResult"/> if the user is not authorized to add a to-do item for
        /// the specified progeny.</description></item> <item><description><see cref="BadRequestResult"/> if the
        /// provided <paramref name="value"/> is invalid or the to-do item could not be created.</description></item>
        /// <item><description><see cref="OkObjectResult"/> containing the created <see cref="TodoItem"/> if the
        /// operation is successful.</description></item> </list></returns>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TodoItem value)
        {
            // Either ProgenyId or FamilyId must be set, but not both.
            if (value.ProgenyId > 0 && value.FamilyId > 0)
            {
                return BadRequest("A TodoItem must have either a ProgenyId or a FamilyId set, but not both.");
            }

            if (value.ProgenyId == 0 && value.FamilyId == 0)
            {
                return BadRequest("A TodoItem must have either a ProgenyId or a FamilyId set.");
            }

            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            if (value.ProgenyId > 0)
            {
                if (!await accessManagementService.HasProgenyPermission(value.ProgenyId, currentUserInfo, PermissionLevel.Add))
                {
                    return Unauthorized();
                }
            }

            if (value.FamilyId > 0)
            {
                if (!await accessManagementService.HasFamilyPermission(value.FamilyId, currentUserInfo, PermissionLevel.Add))
                {
                    return Unauthorized();
                }
            }
            
            
            value.CreatedBy = User.GetUserId();
            if (string.IsNullOrWhiteSpace(value.UId))
            {
                value.UId = Guid.NewGuid().ToString();
            }

            TodoItem todoItem = await todosService.AddTodoItem(value, currentUserInfo);
            if (todoItem == null)
            {
                return Unauthorized();
            }

            // Add TimeLineItem for the new TodoItem
            TimeLineItem timeLineItem = todoItem.ToNewTimeLineItem();
            _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);

            // Send notifications about the new TodoItem
            if (todoItem.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(value.ProgenyId, currentUserInfo);
                await NotifyTodoItemAdded(progeny, currentUserInfo, timeLineItem, todoItem);
            }
            // Todo: Send notification for family too.

            return Ok(todoItem);
        }

        /// <summary>
        /// Updates an existing to-do item with the specified ID.
        /// </summary>
        /// <remarks>This method performs the following steps: <list type="number">
        /// <item><description>Retrieves the to-do item by its ID.</description></item> <item><description>Validates the
        /// user's authorization to update the item based on the associated progeny.</description></item>
        /// <item><description>Updates the to-do item and its associated timeline entry, if
        /// applicable.</description></item> </list></remarks>
        /// <param name="id">The unique identifier of the to-do item to update. Must be a positive integer.</param>
        /// <param name="value">The updated <see cref="TodoItem"/> object containing the new values for the to-do item.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the to-do item with the specified ID does not
        /// exist.</description></item> <item><description><see cref="UnauthorizedResult"/> if the current user is not
        /// authorized to update the to-do item.</description></item> <item><description><see cref="BadRequestResult"/>
        /// if the associated progeny information is invalid.</description></item> <item><description><see
        /// cref="OkObjectResult"/> containing the updated to-do item if the operation is
        /// successful.</description></item> </list></returns>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] TodoItem value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TodoItem todoItem = await todosService.GetTodoItem(id, currentUserInfo);
            if (todoItem == null)
            {
                return NotFound();
            }

            if (todoItem.ProgenyId > 0 && todoItem.FamilyId > 0)
            {
                return BadRequest("TodoItems cannot be assigned to both a progeny and a family.");
            }

            if (todoItem.ProgenyId == 0 && todoItem.FamilyId == 0)
            {
                return BadRequest("TodoItems must be assigned to either a progeny or a family.");
            }

            value.ModifiedBy = User.GetUserId();
            if (string.IsNullOrWhiteSpace(value.UId))
            {
                value.UId = Guid.NewGuid().ToString();
            }

            todoItem = await todosService.UpdateTodoItem(value, currentUserInfo);
            if (todoItem == null || todoItem.TodoItemId == 0)
            {
                return Unauthorized();
            }

            // Update TimeLineItem for the TodoItem
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(todoItem.TodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, currentUserInfo);

            if (timeLineItem == null || !timeLineItem.CopyTodoItemPropertiesForUpdate(todoItem)) return Ok(todoItem);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);

            return Ok(todoItem);
        }

        /// <summary>
        /// Sends notifications when a new TodoItem is added for a specific progeny.
        /// </summary>
        /// <remarks>This method sends both an Azure notification and a web notification to inform
        /// relevant users about the addition of a new TodoItem. The notifications include details about the progeny
        /// and the user who added the item.</remarks>
        /// <param name="progeny">The progeny for whom the TodoItem was added. Cannot be null.</param>
        /// <param name="userInfo">The user who added the TodoItem. Cannot be null.</param>
        /// <param name="timeLineItem">The timeline item associated with the TodoItem. Cannot be null.</param>
        /// <param name="todoItem">The TodoItem that was added. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous notification operation.</returns>
        private async Task NotifyTodoItemAdded(Progeny progeny, UserInfo userInfo, TimeLineItem timeLineItem, TodoItem todoItem)
        {
            string notificationTitle = "Todo item added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new todo item for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);

            await webNotificationsService.SendTodoItemNotification(todoItem, userInfo, notificationTitle);
        }

        /// <summary>
        /// Deletes a specified to-do item and its associated timeline item, if any, after verifying user permissions.
        /// </summary>
        /// <remarks>This method performs the following steps: <list type="number">
        /// <item><description>Retrieves the to-do item by its ID.</description></item> <item><description>Validates
        /// that the user has administrative access to the associated progeny.</description></item>
        /// <item><description>Deletes the associated timeline item, if one exists.</description></item>
        /// <item><description>Deletes the to-do item and sends notifications to relevant users.</description></item>
        /// </list> The method ensures that only authorized users can delete to-do items and their associated
        /// data.</remarks>
        /// <param name="id">The unique identifier of the to-do item to delete. Must be a positive integer.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the operation: <list type="bullet">
        /// <item><description><see cref="NotFoundResult"/> if the to-do item does not exist.</description></item>
        /// <item><description><see cref="UnauthorizedResult"/> if the user does not have permission to delete the to-do
        /// item.</description></item> <item><description><see cref="BadRequestResult"/> if the request is invalid or
        /// the deletion fails.</description></item> <item><description><see cref="NoContentResult"/> if the to-do item
        /// is successfully deleted.</description></item> </list></returns>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TodoItem todoItem = await todosService.GetTodoItem(id, currentUserInfo);
            if (todoItem == null)
            {
                return NotFound();
            }
            
            

            todoItem.ModifiedBy = User.GetUserId();
            bool isDeleted = await todosService.DeleteTodoItem(todoItem, currentUserInfo);
            if (!isDeleted)
            {
                return BadRequest();
            }

            // Check if the TodoItem has a TimeLineItem and delete it
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(todoItem.TodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }

            if (timeLineItem == null) return NoContent();

            // Send notifications about the deleted TodoItem
            if (todoItem.ProgenyId > 0)
            {
                Progeny progeny = await progenyService.GetProgeny(todoItem.ProgenyId, currentUserInfo);
                string notificationTitle = "Todo item deleted for " + progeny.NickName;
                string notificationMessage = currentUserInfo.FullName() + " deleted a todo item for " + progeny.NickName + ". Todo: " + todoItem.Title;

                todoItem.AccessLevel = timeLineItem.AccessLevel = 0; // Set access level to 0 for notifications, to only notify admins.

                await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, currentUserInfo.ProfilePicture);
                await webNotificationsService.SendTodoItemNotification(todoItem, currentUserInfo, notificationTitle);
            }
            // Todo: Send notification for family too.

            return NoContent();
        }
    }
}
