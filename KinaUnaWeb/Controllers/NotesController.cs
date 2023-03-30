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

namespace KinaUnaWeb.Controllers
{
    public class NotesController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly INotesHttpClient _notesHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IViewModelSetupService _viewModelSetupService;
        
        public NotesController(IProgenyHttpClient progenyHttpClient, INotesHttpClient notesHttpClient, IUserInfosHttpClient userInfosHttpClient,
            IViewModelSetupService viewModelSetupService)
        {
            _progenyHttpClient = progenyHttpClient;
            _notesHttpClient = notesHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _viewModelSetupService = viewModelSetupService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, int page = 0, int sort = 1, int itemsPerPage = 10)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            NotesListViewModel model = new(baseModel);
            model.NotesPageParameters.CurrentPageNumber = page;
            model.NotesPageParameters.Sort = sort;
            model.NotesPageParameters.ItemsPerPage = itemsPerPage;

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

            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), parameters.ProgenyId);
            List<Note> notes = await _notesHttpClient.GetNotesList(baseModel.CurrentProgenyId, baseModel.CurrentAccessLevel);
            
            parameters.TotalPages = (int)double.Ceiling((double)notes.Count / parameters.ItemsPerPage);
            parameters.TotalItems = notes.Count;

            notes = notes.OrderBy(n => n.CreatedDate).ToList();

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
                noteItemResponse.Note = new() { NoteId = 0};
            }
            else
            {
                noteItemResponse.Note = await _notesHttpClient.GetNote(parameters.NoteId);
                noteItemResponse.NoteId = noteItemResponse.Note.NoteId;

                BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), noteItemResponse.Note.ProgenyId);
                noteItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
                UserInfo noteUserInfo = await _userInfosHttpClient.GetUserInfoByUserId(noteItemResponse.Note.Owner);
                noteItemResponse.Note.Owner = noteUserInfo.FullName();
            }
            

            return PartialView("_NoteItemPartial", noteItemResponse);

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            NoteViewModel model = new(baseModel);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && model.CurrentUser.UserId != null)
            {
                model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            List<Progeny> progAdminList = await _progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);
            if (!progAdminList.Any())
            {
                // Todo: Show that no children are available to add note for.
                return RedirectToAction("Index");
            }

            Note noteItem = model.CreateNote();

            _ = await _notesHttpClient.AddNote(noteItem);
            
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            Note note = await _notesHttpClient.GetNote(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.NoteItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            Note editedNote = model.CreateNote();

            _ = await _notesHttpClient.UpdateNote(editedNote);
            
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            Note note = await _notesHttpClient.GetNote(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
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
            Note note = await _notesHttpClient.GetNote(model.NoteItem.NoteId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await _notesHttpClient.DeleteNote(note.NoteId);
            return RedirectToAction("Index", "Notes");
        }
    }
}