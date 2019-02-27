using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class NotesController : Controller
    {
        private WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = 2;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public NotesController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context;
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
                _progId = 2;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

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
                userAccessLevel = 0;
            }

            List<NoteViewModel> model = new List<NoteViewModel>();
            
            List<Note> nList = _context.NotesDb.Where(n => n.ProgenyId == _progId).ToList();
            if (nList.Count != 0)
            {
                foreach (Note n in nList)
                {
                    NoteViewModel fIvm = new NoteViewModel();
                    fIvm.ProgenyId = n.ProgenyId;
                    fIvm.AccessLevel = n.AccessLevel;
                    fIvm.Category = n.Category;
                    fIvm.Content = n.Content;
                    fIvm.NoteId = n.NoteId;
                    fIvm.Title = n.Title;
                    fIvm.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(n.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    fIvm.IsAdmin = _userIsProgenyAdmin;
                    UserInfo nUser = await _progenyHttpClient.GetUserInfoByUserId(n.Owner);
                    fIvm.Owner = nUser.FirstName + " " + nUser.MiddleName + " " + nUser.LastName;
                    if (fIvm.AccessLevel >= userAccessLevel)
                    {
                        model.Add(fIvm);
                    }

                }
                model = model.OrderBy(n => n.CreatedDate).ToList();
                model.Reverse();
            }
            else
            {
                NoteViewModel n = new NoteViewModel();
                n.ProgenyId = _progId;
                n.Title = "No notes found.";
                n.Content = "The notes list is empty.";
                n.IsAdmin = _userIsProgenyAdmin;
                model.Add(n);
            }

            model[0].Progeny = progeny;
            return View(model);

        }
    }
}