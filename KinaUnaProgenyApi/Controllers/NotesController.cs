using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ITimelineService _timelineService;
        private readonly INoteService _noteService;
        private readonly IProgenyService _progenyService;
        private readonly AzureNotifications _azureNotifications;
        private readonly ImageStore _imageStore;

        public NotesController(AzureNotifications azureNotifications, ImageStore imageStore, IUserInfoService userInfoService, IUserAccessService userAccessService, ITimelineService timelineService,
            INoteService noteService, IProgenyService progenyService)
        {
            _azureNotifications = azureNotifications;
            _imageStore = imageStore;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _noteService = noteService;
            _progenyService = progenyService;
        }

        // GET api/notes/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail); 
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Note> notesList = await _noteService.GetNotesList(id);
                notesList = notesList.Where(n => n.AccessLevel >= accessLevel).ToList();
                if (notesList.Any())
                {
                    foreach (Note note in notesList)
                    {
                        note.Content = _imageStore.UpdateBlobLinks(note.Content);
                    }
                    return Ok(notesList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/notes/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoteItem(int id)
        {
            Note result = await _noteService.GetNote(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                result.Content = _imageStore.UpdateBlobLinks(result.Content);
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/notes
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Note value)
        {
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add notes for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Note noteItem = new Note();
            noteItem.AccessLevel = value.AccessLevel;
            noteItem.Owner = value.Owner;
            noteItem.Content = value.Content;
            noteItem.Category = value.Category;
            noteItem.ProgenyId = value.ProgenyId;
            noteItem.Title = value.Title;
            noteItem.CreatedDate = value.CreatedDate;

            noteItem = await _noteService.AddNote(noteItem);
            
            // Add to Timeline.
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = noteItem.ProgenyId;
            tItem.AccessLevel = noteItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Note;
            tItem.ItemId = noteItem.NoteId.ToString();
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            if (userinfo != null)
            {
                tItem.CreatedBy = userinfo.UserId;
            }
            tItem.CreatedTime = noteItem.CreatedDate;
            tItem.ProgenyTime = noteItem.CreatedDate;

            _ = await _timelineService.AddTimeLineItem(tItem);

            string title = "Note added for " + prog.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName +
                                 " added a new note for " + prog.NickName;

                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(noteItem);
        }

        // PUT api/notes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Note value)
        {
            // Check if child exists.
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit notes for this child.
                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Note noteItem = await _noteService.GetNote(id);
            if (noteItem == null)
            {
                return NotFound();
            }

            noteItem.AccessLevel = value.AccessLevel;
            noteItem.Owner = value.Owner;
            noteItem.Content = value.Content;
            noteItem.Category = value.Category;
            noteItem.ProgenyId = value.ProgenyId;
            noteItem.Title = value.Title;
            noteItem.CreatedDate = value.CreatedDate;

            noteItem = await _noteService.UpdateNote(noteItem);
            
            // Update Timeline.
            TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note);
            if (tItem != null)
            {
                tItem.ProgenyTime = noteItem.CreatedDate;
                tItem.AccessLevel = noteItem.AccessLevel;
                _ = await _timelineService.UpdateTimeLineItem(tItem);
            }

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Note edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a note for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            return Ok(noteItem);
        }

        // DELETE api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Note noteItem = await _noteService.GetNote(id);
            if (noteItem != null)
            {
                // Check if child exists.
                Progeny prog = await _progenyService.GetProgeny(noteItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete notes for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note);
                if (tItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(tItem);
                }

                _ = await _noteService.DeleteNote(noteItem);

                UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string title = "Note deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a note for " + prog.NickName + ". Note: " + noteItem.Title;
                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await _noteService.GetNote(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    result.Content = _imageStore.UpdateBlobLinks(result.Content);
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetNotesListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Note> allItems = await _noteService.GetNotesList(progenyId);
            allItems = allItems.OrderBy(v => v.CreatedDate).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int noteCounter = 1;
            int notesCount = allItems.Count;
            foreach (Note note in allItems)
            {
                if (sortBy == 1)
                {
                    note.NoteNumber = notesCount - noteCounter + 1;
                }
                else
                {
                    note.NoteNumber = noteCounter;
                }

                noteCounter++;
            }

            List<Note> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Note note in itemsOnPage)
            {
                note.Content = _imageStore.UpdateBlobLinks(note.Content);
            }

            NotesListPage model = new NotesListPage();
            model.NotesList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }
    }
}
