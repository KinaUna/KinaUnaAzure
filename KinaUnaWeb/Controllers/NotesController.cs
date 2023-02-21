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

namespace KinaUnaWeb.Controllers
{
    public class NotesController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly INotesHttpClient _notesHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IViewModelSetupService _viewModelSetupService;
        private readonly INotificationsService _notificationsService;

        public NotesController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, INotesHttpClient notesHttpClient, ImageStore imageStore,
            IViewModelSetupService viewModelSetupService, INotificationsService notificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _notesHttpClient = notesHttpClient;
            _imageStore = imageStore;
            _viewModelSetupService = viewModelSetupService;
            _notificationsService = notificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            NotesListViewModel model = new NotesListViewModel(baseModel);
            
            List<Note> notes = await _notesHttpClient.GetNotesList(model.CurrentProgenyId, model.CurrentAccessLevel);
            if (notes.Count != 0)
            {
                foreach (Note note in notes)
                {
                    NoteViewModel notesViewModel = new NoteViewModel();
                    notesViewModel.SetBaseProperties(baseModel);
                    notesViewModel.SetPropertiesFromNote(note);
                    notesViewModel.IsCurrentUserProgenyAdmin = model.IsCurrentUserProgenyAdmin;
                    UserInfo noteUserInfo = await _userInfosHttpClient.GetUserInfoByUserId(note.Owner);
                    notesViewModel.NoteItem.Owner = noteUserInfo.FullName();
                    model.NotesList.Add(notesViewModel);

                }
                model.NotesList = model.NotesList.OrderBy(n => n.NoteItem.CreatedDate).ToList();
                model.NotesList.Reverse();
            }
            else
            {
                NoteViewModel noteViewModel = new NoteViewModel();
                noteViewModel.NoteItem.ProgenyId = model.CurrentProgenyId;
                noteViewModel.NoteItem.Title = "No notes found.";
                noteViewModel.NoteItem.Content = "The notes list is empty.";
                noteViewModel.IsCurrentUserProgenyAdmin = model.IsCurrentUserProgenyAdmin;
                model.NotesList.Add(noteViewModel);
            }
            
            return View(model);

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            NoteViewModel model = new NoteViewModel(baseModel);
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

            noteItem = await _notesHttpClient.AddNote(noteItem);

            await _notificationsService.SendNoteNotification(noteItem, model.CurrentUser);
            
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            Note note = await _notesHttpClient.GetNote(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            NoteViewModel model = new NoteViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromNote(note);
            model.NoteItem.Content = _imageStore.UpdateBlobLinks(model.NoteItem.Content);

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

            if (ModelState.IsValid)
            {
                Note editedNote = model.CreateNote();
                
                await _notesHttpClient.UpdateNote(editedNote);
            }
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            Note note = await _notesHttpClient.GetNote(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), note.ProgenyId);
            NoteViewModel model = new NoteViewModel(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.NoteItem = note; 
            model.NoteItem.Content = _imageStore.UpdateBlobLinks(model.NoteItem.Content);
            
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

            await _notesHttpClient.DeleteNote(note.NoteId);
            return RedirectToAction("Index", "Notes");
        }
    }
}