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
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            value.Owner = User.GetUserId();

            Note noteItem = await _noteService.AddNote(value);
            
            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.CopyNotePropertiesForAdd(noteItem);
            _ = await _timelineService.AddTimeLineItem(timeLineItem);
            
            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            string notificationTitle = "Note added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new note for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);

            return Ok(noteItem);
        }

        // PUT api/notes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Note value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
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
            
            noteItem = await _noteService.UpdateNote(value);
            
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note);
            if (timeLineItem != null)
            {
                timeLineItem.CopyNotePropertiesForUpdate(noteItem);
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);
            }

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Note edited for " + progeny.NickName;
            string message = userinfo.FullName() + " edited a note for " + progeny.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, timeLineItem, userinfo.ProfilePicture);

            return Ok(noteItem);
        }

        // DELETE api/notes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Note noteItem = await _noteService.GetNote(id);
            if (noteItem != null)
            {
                Progeny progeny = await _progenyService.GetProgeny(noteItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (progeny != null)
                {
                    if (!progeny.IsInAdminList(userEmail))
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
                string title = "Note deleted for " + progeny.NickName;
                string message = userinfo.FullName() + " deleted a note for " + progeny.NickName + ". Note: " + noteItem.Title;
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
