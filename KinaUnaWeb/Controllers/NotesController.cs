using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class NotesController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public NotesController(IProgenyHttpClient progenyHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            List<NoteViewModel> model = new List<NoteViewModel>();
            
            // Todo: Replace _context with _progenyClient.GetNotes()
            List<Note> notes = await _progenyHttpClient.GetNotesList(_progId, userAccessLevel); // _context.NotesDb.Where(n => n.ProgenyId == _progId).ToList();
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
                    notesViewModel.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    notesViewModel.IsAdmin = _userIsProgenyAdmin;
                    UserInfo nUser = await _progenyHttpClient.GetUserInfoByUserId(note.Owner);
                    notesViewModel.Owner = nUser.FirstName + " " + nUser.MiddleName + " " + nUser.LastName;
                    if (notesViewModel.AccessLevel >= userAccessLevel)
                    {
                        model.Add(notesViewModel);
                    }

                }
                model = model.OrderBy(n => n.CreatedDate).ToList();
                model.Reverse();
            }
            else
            {
                NoteViewModel noteViewModel = new NoteViewModel();
                noteViewModel.ProgenyId = _progId;
                noteViewModel.Title = "No notes found.";
                noteViewModel.Content = "The notes list is empty.";
                noteViewModel.IsAdmin = _userIsProgenyAdmin;
                model.Add(noteViewModel);
            }

            model[0].Progeny = progeny;
            return View(model);

        }
    }
}