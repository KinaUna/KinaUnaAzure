using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.TypeScriptModels.Notes;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class NotesController(
        IProgenyHttpClient progenyHttpClient,
        INotesHttpClient notesHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        IViewModelSetupService viewModelSetupService)
        : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int page = 0, int sort = 1, int itemsPerPage = 10)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            NotesListViewModel model = new(baseModel)
            {
                NotesPageParameters =
                {
                    CurrentPageNumber = page,
                    Sort = sort,
                    ItemsPerPage = itemsPerPage
                }
            };

            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewNote(int noteId, bool partialView = false)
        {
            Note note = await notesHttpClient.GetNote(noteId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            NoteViewModel model = new(baseModel);

            model.NoteItem = note;
            model.NoteItem.Progeny = model.CurrentProgeny;
            model.NoteItem.Progeny.PictureLink = model.NoteItem.Progeny.GetProfilePictureUrl();
            UserInfo noteUserInfo = await userInfosHttpClient.GetUserInfoByUserId(model.NoteItem.Owner);
            model.NoteItem.Owner = noteUserInfo.FullName();
            if (partialView)
            {
                return PartialView("_NoteDetailsPartial", model);
            }

            return View(model);
        }

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

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), parameters.ProgenyId);
            List<Note> notes = await notesHttpClient.GetNotesList(baseModel.CurrentProgenyId, baseModel.CurrentAccessLevel);
            
            parameters.TotalPages = (int)double.Ceiling((double)notes.Count / parameters.ItemsPerPage);
            parameters.TotalItems = notes.Count;

            notes = [.. notes.OrderBy(n => n.CreatedDate)];

            if (parameters.Sort == 1)
            {
                notes.Reverse();
            }

            notes = notes.Skip(parameters.ItemsPerPage * (parameters.CurrentPageNumber -1)).Take(parameters.ItemsPerPage).ToList();
            List<int> notesList = notes.Select(n => n.NoteId).ToList();
            return Json(new NotesPageResponse()
            {
                PageNumber = parameters.CurrentPageNumber,
                TotalPages = parameters.TotalPages,
                TotalItems = parameters.TotalItems,
                NotesList = notesList
            });
        }

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
                noteItemResponse.NoteId = noteItemResponse.Note.NoteId;

                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), noteItemResponse.Note.ProgenyId);
                noteItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
                UserInfo noteUserInfo = await userInfosHttpClient.GetUserInfoByUserId(noteItemResponse.Note.Owner);
                noteItemResponse.Note.Owner = noteUserInfo.FullName();
            }
            

            return PartialView("_NoteItemPartial", noteItemResponse);

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            NoteViewModel model = new(baseModel);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
                model.SetProgenyList();
            }

            model.NoteItem.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.PathName = model.CurrentUser.UserId;

            model.SetAccessLevelList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(NoteViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            List<Progeny> progAdminList = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (progAdminList.Count == 0)
            {
                // Todo: Show that no children are available to add note for.
                return RedirectToAction("Index");
            }

            Note noteItem = model.CreateNote();

            _ = await notesHttpClient.AddNote(noteItem);
            
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            Note note = await notesHttpClient.GetNote(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            NoteViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromNote(note);
            
            model.SetAccessLevelList();

            model.PathName = model.CurrentUser.UserId;
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(NoteViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            Note editedNote = model.CreateNote();

            _ = await notesHttpClient.UpdateNote(editedNote);
            
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            Note note = await notesHttpClient.GetNote(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            NoteViewModel model = new(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.NoteItem = note; 
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(NoteViewModel model)
        {
            Note note = await notesHttpClient.GetNote(model.NoteItem.NoteId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await notesHttpClient.DeleteNote(note.NoteId);
            return RedirectToAction("Index", "Notes");
        }
    }
}