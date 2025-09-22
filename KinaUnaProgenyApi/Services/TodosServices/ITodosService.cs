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
        /// <returns>A task that represents the asynchronous operation. The task result contains the added <see
        /// cref="TodoItem"/>.</returns>
        Task<TodoItem> AddTodoItem(TodoItem value);
        /// <summary>
        /// Deletes the specified to-do item from the system.
        /// </summary>
        /// <param name="todoItem">The to-do item to delete. Cannot be null.</param>
        /// <param name="hardDelete">A boolean value indicating whether to perform a hard delete.  If <see langword="true"/>, the item is
        /// permanently removed;  otherwise, it is marked as deleted but retained in the system.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is  <see langword="true"/> if the item
        /// was successfully deleted; otherwise,  <see langword="false"/>.</returns>
        Task<bool> DeleteTodoItem(TodoItem todoItem, bool hardDelete = false);
        /// <summary>
        /// Retrieves a to-do item by its unique identifier.
        /// </summary>
        /// <remarks>Use this method to fetch a specific to-do item from the data source. Ensure that the
        /// <paramref name="id"/> provided is valid and corresponds to an existing item.</remarks>
        /// <param name="id">The unique identifier of the to-do item to retrieve. Must be a positive integer.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the <see cref="TodoItem"/> with
        /// the specified identifier, or <see langword="null"/> if no item with the given identifier exists.</returns>
        Task<TodoItem> GetTodoItem(int id);
        /// <summary>
        /// Retrieves a list of to-do items for a specified progeny based on the provided access level and request
        /// parameters.
        /// </summary>
        /// <param name="id">The unique identifier of the progeny for which to retrieve to-do items.</param>
        /// <param name="accessLevel">The access level of the requesting user. Determines the visibility of the to-do items.</param>
        /// <param name="request">An object containing additional filtering and pagination options for the to-do items.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects matching the specified criteria.</returns>
        Task<List<TodoItem>> GetTodosForProgeny(int id, int accessLevel, TodoItemsRequest request);
        /// <summary>
        /// Updates an existing to-do item with new information.
        /// </summary>
        /// <param name="todoItem">The to-do item containing updated information. The <see cref="TodoItem.TodoItemId"/> property must match an existing
        /// item.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated <see
        /// cref="TodoItem"/>.</returns>
        Task<TodoItem> UpdateTodoItem(TodoItem todoItem);
        /// <summary>
        /// Retrieves a list of TodoItems for a specified progeny, filtered by access level.
        /// </summary>
        /// <param name="progenyId">The unique identifier of the progeny for which to retrieve the TodoItems.</param>
        /// <param name="accessLevel">The access level used to filter the TodoItems. Only items with an access level less than or equal to this
        /// value will be included.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects that match the specified progeny ID and access level. If no items are found, the list will be empty.</returns>
        Task<List<TodoItem>> GetTodosList(int progenyId, int accessLevel);

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
