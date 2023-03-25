using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class NoteService : INoteService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public NoteService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<Note> GetNote(int id)
        {
            Note note = await GetNoteFromCache(id);
            if (note == null || note.NoteId == 0)
            {
                note = await SetNoteInCache(id);
            }

            return note;
        }

        private async Task<Note> GetNoteFromCache(int id)
        {
            Note note = new();
            string cachedNote = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id);
            if (!string.IsNullOrEmpty(cachedNote))
            {
                note = JsonConvert.DeserializeObject<Note>(cachedNote);
            }

            return note;
        }

        private async Task<Note> SetNoteInCache(int id)
        {
            Note note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            if (note != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);

                _ = await SetNotesListInCache(note.ProgenyId);
            }

            return note;
        }

        public async Task<Note> AddNote(Note note)
        {
            Note noteToAdd = new();
            noteToAdd.CopyPropertiesForAdd(note);
            noteToAdd.CreatedDate = DateTime.UtcNow;

            _ = _context.NotesDb.Add(noteToAdd);
            _ = await _context.SaveChangesAsync();

            _ = await SetNoteInCache(noteToAdd.NoteId);

            return noteToAdd;
        }



        public async Task<Note> UpdateNote(Note note)
        {
            Note noteToUpdate = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == note.NoteId);
            if (noteToUpdate != null)
            {
                noteToUpdate.CopyPropertiesForUpdate(note);

                _ = _context.NotesDb.Update(noteToUpdate);
                _ = await _context.SaveChangesAsync();

                _ = await SetNoteInCache(noteToUpdate.NoteId);
            }

            return noteToUpdate;
        }

        public async Task<Note> DeleteNote(Note note)
        {
            Note noteToDelete = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == note.NoteId);
            if (noteToDelete != null)
            {
                _ = _context.NotesDb.Remove(noteToDelete);
                _ = await _context.SaveChangesAsync();
            }

            await RemoveNoteFromCache(note.NoteId, note.ProgenyId);

            return note;
        }

        private async Task RemoveNoteFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "note" + id);

            _ = await SetNotesListInCache(progenyId);
        }

        public async Task<List<Note>> GetNotesList(int progenyId)
        {
            List<Note> notesList = await GetNotesListFromCache(progenyId);
            if (!notesList.Any())
            {
                notesList = await SetNotesListInCache(progenyId);
            }

            return notesList;
        }

        private async Task<List<Note>> GetNotesListFromCache(int progenyId)
        {
            List<Note> notesList = new();
            string cachedNotesList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonConvert.DeserializeObject<List<Note>>(cachedNotesList);
            }

            return notesList;
        }

        private async Task<List<Note>> SetNotesListInCache(int progenyId)
        {
            List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);

            return notesList;
        }
    }
}
