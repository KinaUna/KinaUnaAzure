using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Notes.
    /// </summary>
    /// <param name="imageStore"></param>
    /// <param name="userInfoService"></param>
    /// <param name="timelineService"></param>
    /// <param name="noteService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController(
        IImageStore imageStore,
        IUserInfoService userInfoService,
        ITimelineService timelineService,
        INoteService noteService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves all Notes for a given Progeny for a user with a given access level.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Note items for.</param>
        /// <returns>List of Note items.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<Note> notesList = await noteService.GetNotesList(id, currentUserInfo);
            
            foreach (Note note in notesList)
            {
                note.Content = imageStore.UpdateBlobLinks(note.Content, note.NoteId);
            }

            return Ok(notesList);
        }

        /// <summary>
        /// Retrieves the Note entity with a given id.
        /// </summary>
        /// <param name="id">The NoteId of the Note entity to get.</param>
        /// <returns>The Note object with the provided NoteId.</returns>
        // GET api/notes/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetNoteItem(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Note note = await noteService.GetNote(id, currentUserInfo);
            if (note == null) return NotFound();
            
            note.Content = imageStore.UpdateBlobLinks(note.Content, note.NoteId);
            return Ok(note);

        }

        /// <summary>
        /// Adds a new Note entity to the database.
        /// Then adds a TimeLineItem for the Note.
        /// Then sends notifications to users who have access to the Note.
        /// </summary>
        /// <param name="value">The Note object to add.</param>
        /// <returns>The added Note object.</returns>
        // POST api/notes
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Note value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId, currentUserInfo);
            
            value.Owner = User.GetUserId();

            value.CreatedBy = User.GetUserId();
            value.ModifiedBy = User.GetUserId();

            Note noteItem = await noteService.AddNote(value, currentUserInfo);
            if (noteItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyNotePropertiesForAdd(noteItem);
            _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);
            
            string notificationTitle = "Note added for " + progeny.NickName;
            await webNotificationsService.SendNoteNotification(noteItem, currentUserInfo, notificationTitle);

            noteItem = await noteService.GetNote(noteItem.NoteId, currentUserInfo);

            return Ok(noteItem);
        }

        /// <summary>
        /// Updates an existing Note entity in the database.
        /// Then updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The NoteId of the Note entity to update.</param>
        /// <param name="value">Note object with the properties to update.</param>
        /// <returns>The updated Note object.</returns>
        // PUT api/notes/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Note value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            Note noteItem = await noteService.GetNote(id, currentUserInfo);
            if (noteItem == null)
            {
                return NotFound();
            }

            value.ModifiedBy = User.GetUserId();

            noteItem = await noteService.UpdateNote(value, currentUserInfo);
            if (noteItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, currentUserInfo);
            if (timeLineItem == null) return Ok(noteItem);

            timeLineItem.CopyNotePropertiesForUpdate(noteItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);

            noteItem = await noteService.GetNote(noteItem.NoteId, currentUserInfo);

            return Ok(noteItem);
        }

        /// <summary>
        /// Deletes a Note entity from the database.
        /// </summary>
        /// <param name="id">The NoteId of the entity to delete.</param>
        /// <returns>NoContentResult if the deletion was successful. NotFoundResult if the Note doesn't exist. UnauthorizedResult if the user doesn't have admin access for the Progeny.</returns>
        // DELETE api/notes/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Note noteItem = await noteService.GetNote(id, currentUserInfo);
            if (noteItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(noteItem.ProgenyId, currentUserInfo);
            

            

            noteItem.ModifiedBy = User.GetUserId();

            Note deletedNote = await noteService.DeleteNote(noteItem, currentUserInfo);
            if (deletedNote == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(noteItem.NoteId.ToString(), (int)KinaUnaTypes.TimeLineType.Note, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }
            if (timeLineItem == null) return NoContent();
            
            string notificationTitle = "Note deleted for " + progeny.NickName;
            
            noteItem.AccessLevel = timeLineItem.AccessLevel = 0;
            await webNotificationsService.SendNoteNotification(noteItem, currentUserInfo, notificationTitle);

            return NoContent();
        }
        
        /// <summary>
        /// Retrieves the list of Note items to display on a Notes page for a given Progeny.
        /// </summary>
        /// <param name="pageSize">The number of Note items per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Notes for.</param>
        /// <param name="sortBy">int: Sort order for the Note items. 0 = oldest first, 1 = newest first.</param>
        /// <returns>List of Measurement items.</returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetNotesListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int sortBy = 1)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Note> allItems = await noteService.GetNotesList(progenyId, currentUserInfo);
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

            List<Note> itemsOnPage = [.. allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)];

            foreach (Note note in itemsOnPage)
            {
                note.Content = imageStore.UpdateBlobLinks(note.Content, note.NoteId);
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
