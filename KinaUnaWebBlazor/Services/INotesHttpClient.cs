using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public interface INotesHttpClient
    {
        /// <summary>
        /// Gets the Note with the given NoteId.
        /// </summary>
        /// <param name="noteId">int: The Id of the Note (Note.NoteId).</param>
        /// <returns>Note: The Note object.</returns>
        Task<Note> GetNote(int noteId);

        /// <summary>
        /// Adds a new Note.
        /// </summary>
        /// <param name="note">Note: The new Note to add.</param>
        /// <returns>Note</returns>
        Task<Note> AddNote(Note note);

        /// <summary>
        /// Updates a Note. The Note with the same NoteId will be updated.
        /// </summary>
        /// <param name="note">Note: The Note to update.</param>
        /// <returns>Note: The updated Note object.</returns>
        Task<Note> UpdateNote(Note note);

        /// <summary>
        /// Removes the Note with a given NoteId.
        /// </summary>
        /// <param name="noteId">int: The Id of the Note to remove (Note.NoteId).</param>
        /// <returns>bool: True if the Note was successfully removed.</returns>
        Task<bool> DeleteNote(int noteId);

        /// <summary>
        /// Gets a progeny's list of Notes for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <returns>List of Note objects.</returns>
        Task<List<Note>> GetNotesList(int progenyId, int accessLevel);
    }
}
