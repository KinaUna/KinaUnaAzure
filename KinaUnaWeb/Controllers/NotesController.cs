using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class NotesController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly INotesHttpClient _notesHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly ImageStore _imageStore;
        private readonly IWebNotificationsService _webNotificationsService;

        public NotesController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, INotesHttpClient notesHttpClient, IUserAccessHttpClient userAccessHttpClient,
            IPushMessageSender pushMessageSender, ImageStore imageStore, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _notesHttpClient = notesHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _imageStore = imageStore;
            _webNotificationsService = webNotificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            NotesListViewModel model = new NotesListViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            
            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            List<Note> notes = await _notesHttpClient.GetNotesList(childId, userAccessLevel);
            if (notes.Count != 0)
            {
                foreach (Note note in notes)
                {
                    NoteViewModel notesViewModel = new NoteViewModel();
                    notesViewModel.ProgenyId = note.ProgenyId;
                    notesViewModel.AccessLevel = note.AccessLevel;
                    notesViewModel.Category = note.Category;
                    notesViewModel.Content = note.Content;
                    notesViewModel.NoteId = note.NoteId;
                    notesViewModel.Title = note.Title;
                    notesViewModel.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                    notesViewModel.IsAdmin = model.IsAdmin;
                    UserInfo nUser = await _userInfosHttpClient.GetUserInfoByUserId(note.Owner);
                    notesViewModel.Owner = nUser.FirstName + " " + nUser.MiddleName + " " + nUser.LastName;
                    if (notesViewModel.AccessLevel >= userAccessLevel)
                    {
                        model.NotesList.Add(notesViewModel);
                    }

                }
                model.NotesList = model.NotesList.OrderBy(n => n.CreatedDate).ToList();
                model.NotesList.Reverse();
            }
            else
            {
                NoteViewModel noteViewModel = new NoteViewModel();
                noteViewModel.ProgenyId = childId;
                noteViewModel.Title = "No notes found.";
                noteViewModel.Content = "The notes list is empty.";
                noteViewModel.IsAdmin = model.IsAdmin;
                model.NotesList.Add(noteViewModel);
            }

            model.Progeny = progeny;

            return View(model);

        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            NoteViewModel model = new NoteViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            model.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.PathName = model.CurrentUser.UserId;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(NoteViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            List<Progeny> progAdminList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
            if (!progAdminList.Any())
            {
                // Todo: Show that no children are available to add note for.
                return RedirectToAction("Index");
            }

            Note noteItem = new Note();
            noteItem.Title = model.Title;
            noteItem.ProgenyId = model.ProgenyId;
            noteItem.CreatedDate = TimeZoneInfo.ConvertTimeToUtc(model.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            noteItem.Content = model.Content;
            noteItem.Category = model.Category;
            noteItem.AccessLevel = model.AccessLevel;
            noteItem.Owner = model.CurrentUser.UserId;

            await _notesHttpClient.AddNote(noteItem);

            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= noteItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = model.CurrentUser.FullName();
                        notification.Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new note was added for " + progeny.NickName;
                        notification.Link = "/Notes?childId=" + model.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + progeny.Id);
                    }
                }
            }

            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            NoteViewModel model = new NoteViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Note note = await _notesHttpClient.GetNote(itemId);

            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.NoteId = note.NoteId;
            model.ProgenyId = note.ProgenyId;
            model.AccessLevel = note.AccessLevel;
            model.Category = note.Category;
            model.Title = note.Title;
            model.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            model.Content = _imageStore.UpdateBlobLinks(note.Content);
            model.Owner = note.Owner;
            if (model.Owner.Contains("@"))
            {
                model.Owner = model.CurrentUser.UserId;
            }
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            model.PathName = model.CurrentUser.UserId;

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(NoteViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                Note editedNote = new Note();
                editedNote.NoteId = model.NoteId;
                editedNote.ProgenyId = model.ProgenyId;
                editedNote.AccessLevel = model.AccessLevel;
                editedNote.Category = model.Category;
                editedNote.Title = model.Title;
                editedNote.CreatedDate = TimeZoneInfo.ConvertTimeToUtc(model.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                editedNote.Content = model.Content;
                editedNote.Owner = model.Owner;

                await _notesHttpClient.UpdateNote(editedNote);
            }
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            NoteViewModel model = new NoteViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Note = await _notesHttpClient.GetNote(itemId);
            model.Note.Content = _imageStore.UpdateBlobLinks(model.Note.Content);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Note.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(NoteViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Note note = await _notesHttpClient.GetNote(model.Note.NoteId);
            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _notesHttpClient.DeleteNote(note.NoteId);
            return RedirectToAction("Index", "Notes");
        }
    }
}