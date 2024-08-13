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
        /// <returns>The Note with the given NoteId. Null if the Note doesn't exist.</returns>
        Task<Note> GetNote(int id);

        /// <summary>
        /// Adds a new Note to the database and the cache.
        /// </summary>
        /// <param name="note">The Note object to add.</param>
        /// <returns>The added Note object.</returns>
        Task<Note> AddNote(Note note);

        /// <summary>
        /// Updates a Note in the database and the cache.
        /// </summary>
        /// <param name="note">Note object with the updated properties.</param>
        /// <returns>The updated Note object.</returns>
        Task<Note> UpdateNote(Note note);

        /// <summary>
        /// Deletes a Note from the database and the cache.
        /// </summary>
        /// <param name="note">The Note to delete.</param>
        /// <returns>The deleted Note object.</returns>
        Task<Note> DeleteNote(Note note);

        /// <summary>
        /// Gets a list of all Notes for a Progeny.
        /// First tries to get the list from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Notes for.</param>
        /// <returns>List of Note objects.</returns>
        Task<List<Note>> GetNotesList(int progenyId);
    }
}
