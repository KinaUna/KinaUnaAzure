using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class NoteService: INoteService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public NoteService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Note> GetNote(int id)
        {
            Note note;
            string cachedNote = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id);
            if (!string.IsNullOrEmpty(cachedNote))
            {
                note = JsonConvert.DeserializeObject<Note>(cachedNote);
            }
            else
            {
                note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);
            }

            return note;
        }

        public async Task<Note> AddNote(Note note)
        {
            _ = _context.NotesDb.Add(note);
            _ = await _context.SaveChangesAsync();
            _ = await SetNote(note.NoteId);
            return note;
        }

        public async Task<Note> SetNote(int id)
        {
            Note note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            if (note != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);

                List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(c => c.ProgenyId == note.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + note.ProgenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
            }

            return note;
        }

        public async Task<Note> UpdateNote(Note note)
        {
            _ = _context.NotesDb.Update(note);
            _ = await _context.SaveChangesAsync();
            _ = await SetNote(note.NoteId);
            return note;
        }

        public async Task<Note> DeleteNote(Note note)
        {
            await RemoveNote(note.NoteId, note.ProgenyId);
            _ = _context.NotesDb.Remove(note);
            _ = await _context.SaveChangesAsync();

            return note;
        }

        public async Task RemoveNote(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "note" + id);

            List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
        }

        public async Task<List<Note>> GetNotesList(int progenyId)
        {
            List<Note> notesList;
            string cachedNotesList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonConvert.DeserializeObject<List<Note>>(cachedNotesList);
            }
            else
            {
                notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
            }

            return notesList;
        }
    }
}
