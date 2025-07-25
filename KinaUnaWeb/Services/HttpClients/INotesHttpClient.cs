using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to interact with the Notes API.
    /// </summary>
    public interface INotesHttpClient
    {
        /// <summary>
        /// Gets the Note with the given NoteId.
        /// </summary>
        /// <param name="noteId">The Id of the Note to get.</param>
        /// <returns>The Note object with the given NoteId. If the Note cannot be found, a Note with NoteId = 0 is returned.</returns>
        Task<Note> GetNote(int noteId);

        /// <summary>
        /// Adds a new Note.
        /// </summary>
        /// <param name="note">The new Note object to add.</param>
        /// <returns>The added Note object.</returns>
        Task<Note> AddNote(Note note);

        /// <summary>
        /// Updates a Note. The Note with the same NoteId will be updated.
        /// </summary>
        /// <param name="note">The Note with the updated properties.</param>
        /// <returns>The updated Note object.</returns>
        Task<Note> UpdateNote(Note note);

        /// <summary>
        /// Deletes the Note with a given NoteId.
        /// </summary>
        /// <param name="noteId">The NoteId of the Note to remove.</param>
        /// <returns>bool: True if the Note was successfully removed.</returns>
        Task<bool> DeleteNote(int noteId);

        /// <summary>
        /// Gets the list of all Notes for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny.</param>
        /// <returns>List of Note objects.</returns>
        Task<List<Note>> GetNotesList(int progenyId);
    }
}
