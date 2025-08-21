using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;

namespace KinaUnaProgenyApi.Services.TodosServices
{
    /// <summary>
    /// Defines the contract for managing subtasks within a to-do list application.
    /// </summary>
    /// <remarks>This interface provides methods for creating, retrieving, updating, and deleting subtasks
    /// associated with to-do items. Implementations of this interface should ensure thread safety and proper validation
    /// of input parameters.</remarks>
    public interface ISubtasksService
    {
        /// <summary>
        /// Adds a subtask to the current to-do item.
        /// </summary>
        /// <remarks>The added subtask is associated with the current to-do item. Ensure that the provided
        /// subtask is properly initialized before calling this method.</remarks>
        /// <param name="value">The subtask to add, represented as a <see cref="TodoItem"/>. The subtask must not be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the added subtask as a <see
        /// cref="TodoItem"/>.</returns>
        Task<TodoItem> AddSubtask(TodoItem value);
        
        /// <summary>
        /// Creates a response containing details about the subtasks of a to-do item.
        /// </summary>
        /// <remarks>This method processes the provided subtasks and request details to generate a
        /// response tailored to the caller's requirements. Ensure that the <paramref name="subtasks"/> list is
        /// populated with valid <see cref="TodoItem"/> objects before calling this method.</remarks>
        /// <param name="subtasks">A list of <see cref="TodoItem"/> objects representing the subtasks to include in the response. Cannot be
        /// null.</param>
        /// <param name="request">The <see cref="SubtasksRequest"/> object containing the request details, such as filters or additional
        /// parameters. Cannot be null.</param>
        /// <returns>A <see cref="SubtasksResponse"/> object containing the processed subtasks and any relevant metadata.</returns>
        SubtasksResponse CreateSubtaskResponseForTodoItem(List<TodoItem> subtasks, SubtasksRequest request);

        /// <summary>
        /// Deletes the specified subtask from the to-do list.
        /// </summary>
        /// <remarks>Use the <paramref name="hardDelete"/> parameter to control whether the subtask is
        /// permanently removed or soft-deleted.  Soft-deleted subtasks may still be accessible depending on the
        /// application's retention policies.</remarks>
        /// <param name="subtask">The subtask to delete. Cannot be <see langword="null"/>.</param>
        /// <param name="hardDelete">A value indicating whether the subtask should be permanently deleted.  If <see langword="true"/>, the
        /// subtask is permanently removed; otherwise, it is marked as deleted but retained for potential recovery.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the subtask
        /// was successfully deleted; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteSubtask(TodoItem subtask, bool hardDelete = false);

        /// <summary>
        /// Retrieves a subtask with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the subtask to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the  <see cref="TodoItem"/>
        /// representing the subtask, or <see langword="null"/> if no subtask  with the specified identifier exists.</returns>
        Task<TodoItem> GetSubtask(int id);

        /// <summary>
        /// Retrieves the list of subtasks associated with a specified to-do item.
        /// </summary>
        /// <remarks>This method performs an asynchronous operation to fetch subtasks. Ensure that the
        /// provided to-do item ID corresponds to an existing item in the system.</remarks>
        /// <param name="todoItemTodoItemId">The unique identifier of the to-do item for which to retrieve subtasks.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="TodoItem"/>
        /// objects representing the subtasks. If no subtasks are found, the list will be empty.</returns>
        Task<List<TodoItem>> GetSubtasksForTodoItem(int todoItemTodoItemId);

        /// <summary>
        /// Updates an existing subtask with the specified details.
        /// </summary>
        /// <param name="value">The <see cref="TodoItem"/> containing the updated details of the subtask. The subtask must already exist.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains the updated
        /// <see cref="TodoItem"/>.</returns>
        Task<TodoItem> UpdateSubtask(TodoItem value);
    }
}
