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
    public class TodosController(IProgenyService progenyService, IUserAccessService userAccessService,
        ITodosService todosService, IUserInfoService userInfoService, ITimelineService timelineService,
        IAzureNotifications azureNotifications, IWebNotificationsService webNotificationsService) : ControllerBase
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
        /// Sends notifications when a new todo item is added for a specific progeny.
        /// </summary>
        /// <remarks>This method sends both an Azure notification and a web notification to inform
        /// relevant users about the addition of a new TodoItem. The notifications include details about the progeny
        /// and the user who added the item.</remarks>
        /// <param name="progeny">The progeny for whom the TodoItem was added. Cannot be null.</param>
        /// <param name="userInfo">The user who added the TodoItem. Cannot be null.</param>
        /// <param name="timeLineItem">The timeline item associated with the TodoItem. Cannot be null.</param>
        /// <param name="todoItem">The TodoItem that was added. Cannot be null.</param>
        /// <returns></returns>
        private async Task NotifyTodoItemAdded(Progeny progeny, UserInfo userInfo, TimeLineItem timeLineItem, TodoItem todoItem)
        {
            string notificationTitle = "Todo item added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new todo item for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);

            await webNotificationsService.SendTodoItemNotification(todoItem, userInfo, notificationTitle);
        }
    }
}
