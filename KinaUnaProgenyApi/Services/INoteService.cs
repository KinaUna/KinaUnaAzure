using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaProgenyApi.Services
{
    public interface INoteService
    {
        Task<Note> GetNote(int id);
        Task<Note> AddNote(Note note);
        Task<Note> UpdateNote(Note note);
        Task<Note> DeleteNote(Note note);
        Task<List<Note>> GetNotesList(int progenyId);
    }
}
