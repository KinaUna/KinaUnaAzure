using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface INoteService
    {
        /// <summary>
        /// Gets a Note by NoteId.
        /// </summary>
        /// <param name="id">The NoteId of the Note to get.</param>
        /// <param name="currentUserInfo">The UserInfo of the current user. To check permissions.</param>
        /// <returns>The Note with the given NoteId. Null if the Note doesn't exist.</returns>
        Task<Note> GetNote(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Note to the database and the cache.
        /// </summary>
        /// <param name="note">The Note object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Note object.</returns>
        Task<Note> AddNote(Note note, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Note in the database and the cache.
        /// </summary>
        /// <param name="note">Note object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Note object.</returns>
        Task<Note> UpdateNote(Note note, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Note from the database and the cache.
        /// </summary>
        /// <param name="note">The Note to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Note object.</returns>
        Task<Note> DeleteNote(Note note, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Notes for a Progeny.
        /// First tries to get the list from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Notes for.</param>
        /// <param name="currentUserInfo">The UserInfo of the current user. To check permissions.</param>
        /// <returns>List of Note objects.</returns>
        Task<List<Note>> GetNotesList(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Retrieves a list of notes for the specified progeny, filtered by category.
        /// </summary>
        /// <remarks>The category comparison is case-insensitive and matches notes whose category contains
        /// the specified string.</remarks>
        /// <param name="progenyId">The unique identifier of the progeny whose notes are to be retrieved.</param>
        /// <param name="category">The category to filter the notes by. If <see langword="null"/> or empty, all notes for the specified progeny
        /// are returned.</param>
        /// <param name="currentUserInfo">The user information of the caller, used to determine access permissions.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of <see cref="Note"/>
        /// objects associated with the specified progeny. If a category is provided, only notes matching the category
        /// are included.</returns>
        Task<List<Note>> GetNotesWithCategory(int progenyId, string category, UserInfo currentUserInfo);
    }
}
