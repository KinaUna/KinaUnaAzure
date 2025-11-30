using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.CacheManagement;
using KinaUnaProgenyApi.Services.AccessManagementService;
using KinaUnaProgenyApi.Services.CacheServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace KinaUnaProgenyApi.Services
{
    public class NoteService : INoteService
    {
        private readonly ProgenyDbContext _context;
        private readonly IAccessManagementService _accessManagementService;
        private readonly IDistributedCache _cache;
        private readonly IKinaUnaCacheService _kinaUnaCacheService;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();

        public NoteService(ProgenyDbContext context, IDistributedCache cache, IAccessManagementService accessManagementService, IKinaUnaCacheService kinaUnaCacheService)
        {
            _context = context;
            _accessManagementService = accessManagementService;
            _cache = cache;
            _kinaUnaCacheService = kinaUnaCacheService;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Note by NoteId.
        /// </summary>
        /// <param name="id">The NoteId of the Note to get.</param>
        /// <param name="currentUserInfo">The UserInfo of the current user. To check permissions.</param>
        /// <returns>The Note with the given NoteId. Null if the Note doesn't exist.</returns>
        public async Task<Note> GetNote(int id, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Note, id, currentUserInfo, PermissionLevel.View))
            {
                return null;
            }

            Note note = await GetNoteFromCache(id);
            if (note == null || note.NoteId == 0)
            {
                note = await SetNoteInCache(id);
            }
            if (note == null || note.NoteId == 0)
            {
                return null;
            }
            
            note.ItemPerMission = await _accessManagementService.GetItemPermissionForUser(KinaUnaTypes.TimeLineType.Note, note.NoteId, note.ProgenyId, 0, currentUserInfo);
            
            return note;
        }

        /// <summary>
        /// Gets a Note by NoteId from the cache.
        /// </summary>
        /// <param name="id">The NoteId of the Note to get.</param>
        /// <returns>The Note with the given NoteId. Null if the Note isn't found in the cache.</returns>
        private async Task<Note> GetNoteFromCache(int id)
        {
            string cachedNote = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id);
            if (string.IsNullOrEmpty(cachedNote))
            {
                return null;
            }

            Note note = JsonSerializer.Deserialize<Note>(cachedNote, JsonSerializerOptions.Web);
            return note;
        }

        /// <summary>
        /// Gets a Note by NoteId from the database and adds it to the cache.
        /// Also updates the NotesList for the Progeny that the Note belongs to in the cache.
        /// </summary>
        /// <param name="id">The NoteId of the Note to get and set.</param>
        /// <returns>The Note with the given NoteId. Null if the Note doesn't exist.</returns>
        private async Task<Note> SetNoteInCache(int id)
        {
            Note note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            if (note == null) return null;

            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id, JsonSerializer.Serialize(note, JsonSerializerOptions.Web), _cacheOptionsSliding);

            _ = await SetNotesListInCache(note.ProgenyId);

            return note;
        }

        /// <summary>
        /// Adds a new Note to the database and the cache.
        /// </summary>
        /// <param name="note">The Note object to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Note object.</returns>
        public async Task<Note> AddNote(Note note, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasProgenyPermission(note.ProgenyId, currentUserInfo, PermissionLevel.Add))
            {
                return null;
            }

            Note noteToAdd = new();
            noteToAdd.CopyPropertiesForAdd(note);
            
            _ = _context.NotesDb.Add(noteToAdd);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.AddItemPermissions(KinaUnaTypes.TimeLineType.Note, noteToAdd.NoteId, noteToAdd.ProgenyId, 0, noteToAdd.ItemPermissionsDtoList, currentUserInfo);

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(noteToAdd.ProgenyId, 0, KinaUnaTypes.TimeLineType.Note);

            _ = await SetNoteInCache(noteToAdd.NoteId);
            
            return noteToAdd;
        }

        /// <summary>
        /// Updates a Note in the database and the cache.
        /// </summary>
        /// <param name="note">Note object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Note object.</returns>
        public async Task<Note> UpdateNote(Note note, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Note, note.NoteId, currentUserInfo, PermissionLevel.Edit))
            {
                return null;
            }

            Note noteToUpdate = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == note.NoteId);
            if (noteToUpdate == null) return null;

            noteToUpdate.CopyPropertiesForUpdate(note);

            _ = _context.NotesDb.Update(noteToUpdate);
            _ = await _context.SaveChangesAsync();

            await _accessManagementService.UpdateItemPermissions(KinaUnaTypes.TimeLineType.Note, noteToUpdate.NoteId, noteToUpdate.ProgenyId, 0, noteToUpdate.ItemPermissionsDtoList, currentUserInfo);

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(noteToUpdate.ProgenyId, 0, KinaUnaTypes.TimeLineType.Note);

            _ = await SetNoteInCache(noteToUpdate.NoteId);

            return noteToUpdate;
        }

        /// <summary>
        /// Deletes a Note from the database and the cache.
        /// </summary>
        /// <param name="note">The Note to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Note object.</returns>
        public async Task<Note> DeleteNote(Note note, UserInfo currentUserInfo)
        {
            if (!await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Note, note.NoteId, currentUserInfo, PermissionLevel.Admin))
            {
                return null;
            }

            Note noteToDelete = await _context.NotesDb.SingleOrDefaultAsync(n => n.NoteId == note.NoteId);
            if (noteToDelete == null)
            {
                await RemoveNoteFromCache(note.NoteId, note.ProgenyId);
                return null;
            }

            _ = _context.NotesDb.Remove(noteToDelete);
            _ = await _context.SaveChangesAsync();

            // Remove all associated permissions.
            List<TimelineItemPermission> timelineItemPermissionsList = await _accessManagementService.GetTimelineItemPermissionsList(KinaUnaTypes.TimeLineType.Contact, noteToDelete.NoteId, currentUserInfo);
            foreach (TimelineItemPermission permission in timelineItemPermissionsList)
            {
                await _accessManagementService.RevokeItemPermission(permission, currentUserInfo);
            }

            _kinaUnaCacheService.SetProgenyOrFamilyTimelineUpdatedCache(note.ProgenyId, 0, KinaUnaTypes.TimeLineType.Note);

            await RemoveNoteFromCache(note.NoteId, note.ProgenyId);

            return note;
        }

        /// <summary>
        /// Removes a Note from the cache and updates the NotesList for the Progeny that the Note belongs to.
        /// </summary>
        /// <param name="id">The NoteId of the Note to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Note belongs to.</param>
        /// <returns></returns>
        private async Task RemoveNoteFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "note" + id);

            _ = await SetNotesListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Notes for a Progeny.
        /// First tries to get the list from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Notes for.</param>
        /// <param name="currentUserInfo">The UserInfo of the current user. To check permissions.</param>
        /// <returns>List of Note objects.</returns>
        public async Task<List<Note>> GetNotesList(int progenyId, UserInfo currentUserInfo)
        {
            NotesListCacheEntry cacheEntry = _kinaUnaCacheService.GetNotesListCache(currentUserInfo.UserId, progenyId);
            TimelineUpdatedCacheEntry timelineUpdatedCacheEntry = _kinaUnaCacheService.GetProgenyOrFamilyTimelineUpdatedCache(progenyId, 0, KinaUnaTypes.TimeLineType.Note);
            if (cacheEntry != null && timelineUpdatedCacheEntry != null)
            {
                if (cacheEntry.UpdateTime >= timelineUpdatedCacheEntry.UpdateTime)
                {
                    return cacheEntry.NotesList.ToList();
                }
            }

            Note[] notesList = await GetNotesListFromCache(progenyId);
            if (notesList.Length == 0)
            {
                notesList = await SetNotesListInCache(progenyId);
            }

            List<Note> accessibleNotes = [];
            foreach (Note note in notesList)
            {
                if (await _accessManagementService.HasItemPermission(KinaUnaTypes.TimeLineType.Note, note.NoteId, currentUserInfo, PermissionLevel.View))
                {
                    accessibleNotes.Add(note);
                }
            }

            _kinaUnaCacheService.SetNotesListCache(currentUserInfo.UserId, progenyId, accessibleNotes.ToArray());

            return accessibleNotes;
        }

        /// <summary>
        /// Gets a list of all Notes for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Notes for.</param>
        /// <returns>List of Note objects.</returns>
        private async Task<Note[]> GetNotesListFromCache(int progenyId)
        {
            Note[] notesList = [];
            string cachedNotesList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonSerializer.Deserialize<Note[]>(cachedNotesList, JsonSerializerOptions.Web);
            }

            return notesList;
        }

        /// <summary>
        /// Gets a list of all Notes for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set the List of Notes for.</param>
        /// <returns>List of Notes objects.</returns>
        private async Task<Note[]> SetNotesListInCache(int progenyId)
        {
            Note[] notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToArrayAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId, JsonSerializer.Serialize(notesList, JsonSerializerOptions.Web), _cacheOptionsSliding);

            return notesList;
        }

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
        public async Task<List<Note>> GetNotesWithCategory(int progenyId, string category, UserInfo currentUserInfo)
        {
            List<Note> allItems = await GetNotesList(progenyId, currentUserInfo);
            if (!string.IsNullOrEmpty(category))
            {
                allItems = [.. allItems.Where(n => n.Category != null && n.Category.Contains(category, StringComparison.CurrentCultureIgnoreCase))];
            }
            return allItems;
        }
    }
}
