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
    public class SkillsController : Controller
    {
        private WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = 2;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public SkillsController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
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
            List<SkillViewModel> model = new List<SkillViewModel>();
            
            List<Skill> skillsList = _context.SkillsDb.Where(w => w.ProgenyId == _progId).ToList();
            skillsList = skillsList.OrderBy(s => s.SkillFirstObservation).ToList();
            if (skillsList.Count != 0)
            {
                foreach (Skill s in skillsList)
                {
                    SkillViewModel sIvm = new SkillViewModel();
                    sIvm.ProgenyId = s.ProgenyId;
                    sIvm.AccessLevel = s.AccessLevel;
                    sIvm.Description = s.Description;
                    sIvm.Category = s.Category;
                    sIvm.Name = s.Name;
                    sIvm.SkillFirstObservation = s.SkillFirstObservation;
                    sIvm.SkillId = s.SkillId;
                    sIvm.IsAdmin = _userIsProgenyAdmin;
                    if (sIvm.AccessLevel >= userAccessLevel)
                    {
                        model.Add(sIvm);
                    }

                }
            }
            else
            {
                SkillViewModel s = new SkillViewModel();
                s.ProgenyId = _progId;
                s.AccessLevel = 5;
                s.Description = "The skills list is empty.";
                s.Category = "";
                s.Name = "No items";
                s.SkillFirstObservation = DateTime.UtcNow;

                s.IsAdmin = _userIsProgenyAdmin;

                model.Add(s);
            }

            model[0].Progeny = progeny;
            return View(model);

        }
    }
}