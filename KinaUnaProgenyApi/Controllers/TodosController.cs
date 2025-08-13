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
    /// Provides API endpoints for managing to-do items associated with progenies.
    /// </summary>
    /// <remarks>This controller includes methods for retrieving, creating, updating, and deleting to-do
    /// items. It enforces user access policies to ensure that only authorized users can perform operations on to-do
    /// items. The controller also integrates with notification services to inform users about changes to to-do
    /// items.</remarks>
    /// <param name="progenyService"></param>
    /// <param name="userAccessService"></param>
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
        IUserAccessService userAccessService,
        ITodosService todosService,
        IUserInfoService userInfoService,
        ITimelineService timelineService,
        IAzureNotifications azureNotifications,
        IWebNotificationsService webNotificationsService) : ControllerBase
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

            if (request.Skip <= 0) request.Skip = (request.CurrentPageNumber -1) * request.NumberOfItems;

            List<TodoItem> todoItems = [];

            if (progenyList.Count == 0) return NotFound();
            foreach (Progeny progeny in progenyList)
            {
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progeny.Id, userEmail);
                List<TodoItem> progenyTodos = await todosService.GetTodosForProgeny(progeny.Id, userAccess.AccessLevel, request);
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
            TodoItem result = await todosService.GetTodoItem(id);
            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(result.ProgenyId, userEmail, result.AccessLevel);

            if (accessLevelResult.IsSuccess) return Ok(result);

            return accessLevelResult.ToActionResult();

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

            TodoItem todoItem = await todosService.AddTodoItem(value);

            if (todoItem == null)
            {
                return BadRequest();
            }

            // Add TimeLineItem for the new TodoItem
            TimeLineItem timeLineItem = todoItem.ToNewTimeLineItem();
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            // Send notifications about the new TodoItem
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            await NotifyTodoItemAdded(progeny, userInfo, timeLineItem, todoItem);

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
            TodoItem todoItem = await todosService.GetTodoItem(id);
            if (todoItem == null)
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

            todoItem = await todosService.UpdateTodoItem(value);

            // Update TimeLineItem for the TodoItem
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(todoItem.TodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem);

            if (timeLineItem == null || !timeLineItem.CopyTodoItemPropertiesForUpdate(todoItem)) return Ok(todoItem);

            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

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
            TodoItem todoItem = await todosService.GetTodoItem(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            // Check if the user has access to the Progeny associated with the TodoItem
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            Progeny progeny = await progenyService.GetProgeny(todoItem.ProgenyId);
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

            // Check if the TodoItem has a TimeLineItem and delete it
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(todoItem.TodoItemId.ToString(), (int)KinaUnaTypes.TimeLineType.TodoItem);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            todoItem.ModifiedBy = User.GetUserId();
            bool isDeleted = await todosService.DeleteTodoItem(todoItem);
            if (!isDeleted)
            {
                return BadRequest();
            }

            if (timeLineItem == null) return NoContent();

            // Send notifications about the deleted TodoItem
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Todo item deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a todo item for " + progeny.NickName + ". Todo: " + todoItem.Title;

            todoItem.AccessLevel = timeLineItem.AccessLevel = 0; // Set access level to 0 for notifications, to only notify admins.

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendTodoItemNotification(todoItem, userInfo, notificationTitle);

            return NoContent();
        }
    }
}
