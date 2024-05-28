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
    public class NotesController(
        IAzureNotifications azureNotifications,
        IImageStore imageStore,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ITimelineService timelineService,
        INoteService noteService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/notes/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Note> notesList = await noteService.GetNotesList(id);
            notesList = notesList.Where(n => n.AccessLevel >= accessLevel).ToList();
            if (notesList.Count == 0) return NotFound();

            foreach (Note note in notesList)
            {
                note.Content = imageStore.UpdateBlobLinks(note.Content);
            }
            return Ok(notesList);

        }

        // GET api/notes/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNoteItem(int id)
        {
            Note result = await noteService.GetNote(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            result.Content = imageStore.UpdateBlobLinks(result.Content);
            return Ok(result);

        }

        // POST api/notes
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Note value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            Note noteItem = await noteService.AddNote(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyNotePropertiesForAdd(noteItem);
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Note added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new note for " + progeny.NickName;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendNoteNotification(noteItem, userInfo, notificationTitle);

            return Ok(noteItem);
        }

        // PUT api/notes/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Note value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            Note noteItem = await noteService.GetNote(id);
            if (noteItem == null)
            {
                return NotFound();
            }

            noteItem = await noteService.UpdateNote(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note);
            if (timeLineItem == null) return Ok(noteItem);

            timeLineItem.CopyNotePropertiesForUpdate(noteItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Note edited for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " edited a note for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendNoteNotification(noteItem, userInfo, notificationTitle);

            return Ok(noteItem);
        }

        // DELETE api/notes/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Note noteItem = await noteService.GetNote(id);
            if (noteItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(noteItem.ProgenyId);
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

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            _ = await noteService.DeleteNote(noteItem);

            if (timeLineItem == null) return NoContent();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Note deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a note for " + progeny.NickName + ". Note: " + noteItem.Title;

            noteItem.AccessLevel = timeLineItem.AccessLevel = 0;
            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendNoteNotification(noteItem, userInfo, notificationTitle);

            return NoContent();

        }

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetNoteMobile(int id)
        {
            Note result = await noteService.GetNote(id);

            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess == null && result.ProgenyId != Constants.DefaultChildId) return Unauthorized();

            result.Content = imageStore.UpdateBlobLinks(result.Content);
            return Ok(result);

        }

        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetNotesListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Note> allItems = await noteService.GetNotesList(progenyId);
            allItems = [.. allItems.OrderBy(v => v.CreatedDate)];

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
                note.Content = imageStore.UpdateBlobLinks(note.Content);
            }

            NotesListPage model = new()
            {
                NotesList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }
    }
}
