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

namespace KinaUnaWeb.Controllers
{
    public class NotesController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly INotesHttpClient _notesHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public NotesController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, INotesHttpClient notesHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _notesHttpClient = notesHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
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
    }
}