using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Validation.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Notes.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="imageStore"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="timelineService"></param>
    /// <param name="noteService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
        /// <summary>
        /// Retrieves all Notes for a given Progeny for a user with a given access level.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Note items for.</param>
        /// <returns>List of Note items.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<Note> notesList = await noteService.GetNotesList(id, accessLevelResult.Value);
            
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
            Note note = await noteService.GetNote(id);
            if (note == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(note.ProgenyId, userEmail, note.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

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
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(progenyId, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Note> allItems = await noteService.GetNotesList(progenyId, accessLevelResult.Value);
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
