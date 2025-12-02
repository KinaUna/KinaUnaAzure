using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Models.TypeScriptModels.Notes;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class NotesController(
        IProgenyHttpClient progenyHttpClient,
        INotesHttpClient notesHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        IViewModelSetupService viewModelSetupService,
        ImageStore imageStore)
        : Controller
    {
        /// <summary>
        /// Notes Index page. Shows a paginated list of all notes for a Progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show Notes for.</param>
        /// <param name="page">Current page number.</param>
        /// <param name="sort">Sort order, 0 = oldest first, 1 = newest first.</param>
        /// <param name="itemsPerPage">Number of Notes per page.</param>
        /// <param name="noteId">The Id of the Note to show. If 0 no Note popup is shown.</param>
        /// <returns></returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int page = 0, int sort = 1, int itemsPerPage = 10, int noteId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId, 0, false);
            NotesListViewModel model = new(baseModel)
            {
                NotesPageParameters =
                {
                    CurrentPageNumber = page,
                    Sort = sort,
                    ItemsPerPage = itemsPerPage
                },
                NoteId = noteId
            };

            return View(model);

        }

        /// <summary>
        /// Page or Partial view to show a single Note.
        /// </summary>
        /// <param name="noteId">The NoteId of the Note to show.</param>
        /// <param name="partialView">If True returns a partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with NoteViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> ViewNote(int noteId, bool partialView = false)
        {
            Note note = await notesHttpClient.GetNote(noteId);
            if (note == null || note.NoteId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId, 0, false);
            NoteViewModel model = new(baseModel)
            {
                NoteItem = note
            };

            model.NoteItem.Progeny = model.CurrentProgeny;
            model.NoteItem.Progeny.PictureLink = model.NoteItem.Progeny.GetProfilePictureUrl();
            UserInfo noteUserInfo = await userInfosHttpClient.GetSimpleUserInfoByUserId(model.NoteItem.Owner);
            model.NoteItem.Owner = noteUserInfo.FullName();
            if (partialView)
            {
                return PartialView("_NoteDetailsPartial", model);
            }

            return View(model);
        }

        /// <summary>
        /// HttpPost method for fetching a list of Notes for a Progeny.
        /// </summary>
        /// <param name="parameters">NotesPageParameters.</param>
        /// <returns>Json of NotesPageResponse.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> NotesList([FromBody] NotesPageParameters parameters)
        {
            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (parameters.CurrentPageNumber < 1)
            {
                parameters.CurrentPageNumber = 1;
            }

            if (parameters.ItemsPerPage < 1)
            {
                parameters.ItemsPerPage = 10;
            }
            
            List<Note> notes = [];

            foreach (int progenyId in parameters.Progenies)
            {
                List<Note> progenyNotes = await notesHttpClient.GetNotesList(progenyId);
                
                notes.AddRange(progenyNotes);
            }

            parameters.TotalPages = (int)double.Ceiling((double)notes.Count / parameters.ItemsPerPage);
            parameters.TotalItems = notes.Count;

            notes = [.. notes.OrderBy(n => n.CreatedDate)];

            if (parameters.Sort == 1)
            {
                notes.Reverse();
            }

            notes = [.. notes.Skip(parameters.ItemsPerPage * (parameters.CurrentPageNumber - 1)).Take(parameters.ItemsPerPage)];
            List<int> notesList = [.. notes.Select(n => n.NoteId)];
            return Json(new NotesPageResponse()
            {
                PageNumber = parameters.CurrentPageNumber,
                TotalPages = parameters.TotalPages,
                TotalItems = parameters.TotalItems,
                NotesList = notesList
            });
        }

        /// <summary>
        /// Gets a partial view with a Note element, for notes lists to fetch HTML for each note.
        /// </summary>
        /// <param name="parameters">NoteItemParameters object with the Note details.</param>
        /// <returns>PartialView with NoteItemResponse.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> NoteElement([FromBody] NoteItemParameters parameters)
        {
            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }
            
            NoteItemResponse noteItemResponse = new()
            {
                LanguageId = parameters.LanguageId
            };

            if (parameters.NoteId == 0)
            {
                noteItemResponse.Note = new Note { NoteId = 0};
            }
            else
            {
                noteItemResponse.Note = await notesHttpClient.GetNote(parameters.NoteId);
                if (noteItemResponse.Note == null || noteItemResponse.Note.NoteId == 0)
                {
                    return PartialView("_NotFoundPartial");
                }
                noteItemResponse.Note.Progeny = await progenyHttpClient.GetProgeny(noteItemResponse.Note.ProgenyId);
                noteItemResponse.NoteId = noteItemResponse.Note.NoteId; 
                UserInfo noteUserInfo = await userInfosHttpClient.GetSimpleUserInfoByUserId(noteItemResponse.Note.Owner);
                noteItemResponse.Note.Owner = noteUserInfo.FullName();
            }
            

            return PartialView("_NoteItemPartial", noteItemResponse);

        }

        /// <summary>
        /// Page for adding a new Note.
        /// </summary>
        /// <returns>View with NoteViewModel.</returns>
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            NoteViewModel model = new(baseModel);
            
            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();

            model.NoteItem.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.PathName = model.CurrentUser.UserId;

            return PartialView("_AddNotePartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for adding a new Note.
        /// </summary>
        /// <param name="model">NoteViewModel with the properties for the Note to add.</param>
        /// <returns>Redirects to Notes/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(NoteViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            bool canUserAdd = false;
            if (model.NoteItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.NoteItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }

            if (!canUserAdd)
            {
                // Todo: Show that no entities are available to add note for.
                return RedirectToAction("Index");
            }

            Note noteItem = model.CreateNote();
                
            model.NoteItem = await notesHttpClient.AddNote(noteItem);
            model.NoteItem.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(model.NoteItem.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.NoteItem.Progeny = await progenyHttpClient.GetProgeny(model.NoteItem.ProgenyId);

            return PartialView("_NoteAddedPartial", model);
        }

        /// <summary>
        /// Edit Note page.
        /// </summary>
        /// <param name="itemId">The NoteId of the Note to edit.</param>
        /// <returns>View with NoteViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            Note note = await notesHttpClient.GetNote(itemId);
            if (note == null || note.NoteId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (note.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            NoteViewModel model = new(baseModel);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(note.ProgenyId);
            model.SetProgenyList();

            model.SetPropertiesFromNote(note);
            model.PathName = model.CurrentUser.UserId;
            
            return PartialView("_EditNotePartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for updating an edited Note.
        /// </summary>
        /// <param name="model">NoteViewModel with the updated Note properties.</param>
        /// <returns>Note updated page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(NoteViewModel model)
        {
            Note existingNote = await notesHttpClient.GetNote(model.NoteItem.NoteId);
            if (existingNote == null || existingNote.NoteId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (existingNote.ItemPerMission.PermissionLevel < PermissionLevel.Edit)
            {
                return Unauthorized();
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            
            Note editedNote = model.CreateNote();

            model.NoteItem = await notesHttpClient.UpdateNote(editedNote);
            model.NoteItem.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(model.NoteItem.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.NoteItem.Progeny = await progenyHttpClient.GetProgeny(model.NoteItem.ProgenyId);

            return PartialView("_NoteUpdatedPartial", model);
        }

        /// <summary>
        /// Page to delete a Note.
        /// </summary>
        /// <param name="itemId">The NoteId of the Note to delete.</param>
        /// <returns>View with NoteViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            Note note = await notesHttpClient.GetNote(itemId);
            if (note == null || note.NoteId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (note.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId, 0, false);
            NoteViewModel model = new(baseModel);

            model.NoteItem = note; 
            
            return View(model);
        }

        /// <summary>
        /// HttpPost endpoint for deleting a Note.
        /// </summary>
        /// <param name="model">NoteViewModel with properties of the Note to delete.</param>
        /// <returns>Redirects to Notes/Index page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(NoteViewModel model)
        {
            Note note = await notesHttpClient.GetNote(model.NoteItem.NoteId);
            if (note == null || note.NoteId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (note.ItemPerMission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            
            _ = await notesHttpClient.DeleteNote(note.NoteId);

            return RedirectToAction("Index", "Notes");
        }

        /// <summary>
        /// Copy Note page.
        /// </summary>
        /// <param name="itemId">The NoteId of the Note to copy.</param>
        /// <returns>View with NoteViewModel.</returns>
        [HttpGet]
        public async Task<IActionResult> CopyNote(int itemId)
        {
            Note note = await notesHttpClient.GetNote(itemId);
            if (note == null || note.NoteId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId, 0, false);
            NoteViewModel model = new(baseModel);
            model.SetPropertiesFromNote(note);

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList();
            model.SetProgenyList();
            
            model.PathName = model.CurrentUser.UserId;

            return PartialView("_CopyNotePartial", model);
        }

        /// <summary>
        /// HttpPost endpoint for updating an edited Note.
        /// </summary>
        /// <param name="model">NoteViewModel with the updated Note properties.</param>
        /// <returns>Note copied partial view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopyNote(NoteViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId, 0, false);
            model.SetBaseProperties(baseModel);
            
            bool canUserAdd = false;
            if (model.NoteItem.ProgenyId > 0)
            {
                List<Progeny> progenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.Add);
                if (progenies.Exists(p => p.Id == model.NoteItem.ProgenyId))
                {
                    canUserAdd = true;
                }
            }
            
            if (!canUserAdd)
            {
                // Todo: Show that no family or family members are available to add note for.
                return PartialView("_AccessDeniedPartial");
            }
            Note editedNote = model.CreateNote();

            model.NoteItem = await notesHttpClient.AddNote(editedNote);
            model.NoteItem.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(model.NoteItem.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.NoteItem.Progeny = await progenyHttpClient.GetProgeny(model.NoteItem.ProgenyId);

            return PartialView("_NoteCopiedPartial", model);
        }

        /// <summary>
        /// Get method for fetching an image for a Note from the Azure Blob Storage.
        /// This provides a static URL for embedded images.
        /// </summary>
        /// <param name="imageId">The Id of the image.</param>
        /// <param name="noteId">The NoteId of the Note the image is attached to.</param>
        /// <returns>FileContentResult with the image data.</returns>
        [AllowAnonymous]

        public async Task<FileContentResult> Image([FromQuery] int noteId, [FromQuery] string imageId)
        {
            Note note = await notesHttpClient.GetNote(noteId);
            if (note == null || note.NoteId ==0 || note.ItemPerMission.PermissionLevel < PermissionLevel.View)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("868b62e2-6978-41a1-97dc-1cc1116f65a6.jpg");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/jpeg");
            }

            MemoryStream fileContent = await imageStore.GetStream(imageId, BlobContainers.Notes);
            byte[] fileContentBytes = fileContent.ToArray();

            string contentType = FileContentTypeHelpers.GetContentTypeString(imageId);

            return new FileContentResult(fileContentBytes, contentType);
        }
    }
}