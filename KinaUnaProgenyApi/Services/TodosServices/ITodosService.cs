using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    public interface ITodosService
    {
        /// <summary>
        /// Adds a new to-do item to the collection.
        /// </summary>
        /// <remarks>The added to-do item is persisted and may be retrieved later through appropriate
        /// query methods.</remarks>
        /// <param name="value">The <see cref="TodoItem"/> to add. This parameter cannot be null.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see
        /// cref="TodoItem"/>.</returns>
        Task<TodoItem> AddTodoItem(TodoItem value, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes the specified to-do item from the system.
        /// </summary>
        /// <param name="todoItem">The to-do item to delete. Cannot be null.</param>
        /// <param name="currentUserInfo"></param>
        /// <param name="hardDelete">A boolean value indicating whether to perform a hard delete.  If <see langword="true"/>, the item is
        ///     permanently removed;  otherwise, it is marked as deleted but retained in the system.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is  <see langword="true"/> if the item
        /// was successfully deleted; otherwise,  <see langword="false"/>.</returns>
        Task<bool> DeleteTodoItem(TodoItem todoItem, UserInfo currentUserInfo, bool hardDelete = false);

        /// <summary>
        /// Retrieves a to-do item by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the to-do item to retrieve.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A <see cref="TodoItem"/> representing the to-do item with the specified identifier,  or <see
        /// langword="null"/> if no matching item is found.</returns>
        Task<TodoItem> GetTodoItem(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a filtered and paginated list of to-do items for a specific progeny or family.
        /// </summary>
        /// <remarks>The method applies the following filters and operations in sequence: <list
        /// type="bullet"> <item><description>Filters by the progeny ID and permissions.</description></item>
        /// <item><description>Filters by the specified date range, if provided.</description></item>
        /// <item><description>Filters by tags, context, and status, if specified in the request.</description></item>
        /// <item><description>Sorts the results by due date (newest first) and then by creation
        /// time.</description></item> <item><description>Applies pagination based on the skip and take values in the
        /// request.</description></item> </list></remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which to retrieve to-do items.</param>
        /// <param name="familyId">The unique identifier of the family for which to retrieve to-do items.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <param name="request">An object containing filtering, sorting, and pagination options for the to-do items.  This includes date
        /// ranges, tags, context, status, and pagination parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects  that match the specified filters and pagination settings.</returns>
        Task<List<TodoItem>> GetTodosForProgenyOrFamily(int progenyId, int familyId, UserInfo currentUserInfo, TodoItemsRequest request);

        /// <summary>
        /// Updates an existing to-do item with new information.
        /// </summary>
        /// <param name="todoItem">The to-do item containing updated information. The <see cref="TodoItem.TodoItemId"/> property must match an existing
        ///     item.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see
        /// cref="TodoItem"/>.</returns>
        Task<TodoItem> UpdateTodoItem(TodoItem todoItem, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of to-do items for a specified progeny or family, filtered by access level.
        /// </summary>
        /// <remarks>To-do items marked as deleted are excluded from the results. The method uses a
        /// no-tracking query to improve performance  when the returned data does not need to be tracked by the database
        /// context.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny for which to retrieve to-do items.</param>
        /// <param name="familyId">The unique identifier of the family for which to retrieve to-do items.</param>
        /// <param name="currentUserInfo">The UserInfo object for the current user, to check permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects  that match the specified criteria. The list will be empty if no matching items are found.</returns>
        Task<List<TodoItem>> GetTodosList(int progenyId, int familyId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a paginated list of TodoItems for progenies based on the specified request parameters.
        /// </summary>
        /// <remarks>The method processes the provided list of TodoItems and applies the pagination and
        /// filtering criteria defined in the <paramref name="request"/>. The response includes the filtered TodoItems
        /// and additional metadata such as the total number of items and the current page index.</remarks>
        /// <param name="todoItemsForProgenies">A list of TodoItems associated with progenies to be filtered and paginated.</param>
        /// <param name="request">The request parameters that specify pagination and filtering options.</param>
        /// <returns>A <see cref="TodoItemsResponse"/> containing the paginated list of TodoItems and associated metadata.</returns>
        TodoItemsResponse CreateTodoItemsResponseForTodoPage(List<TodoItem> todoItemsForProgenies, TodoItemsRequest request);
    }
}
