using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class SkillsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public SkillsController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context; // Todo: replace _context with httpClient
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
            List<SkillViewModel> model = new List<SkillViewModel>();
            
            List<Skill> skillsList = _context.SkillsDb.Where(w => w.ProgenyId == _progId).ToList();
            skillsList = skillsList.OrderBy(s => s.SkillFirstObservation).ToList();
            if (skillsList.Count != 0)
            {
                foreach (Skill skill in skillsList)
                {
                    SkillViewModel skillViewModel = new SkillViewModel();
                    skillViewModel.ProgenyId = skill.ProgenyId;
                    skillViewModel.AccessLevel = skill.AccessLevel;
                    skillViewModel.Description = skill.Description;
                    skillViewModel.Category = skill.Category;
                    skillViewModel.Name = skill.Name;
                    skillViewModel.SkillFirstObservation = skill.SkillFirstObservation;
                    skillViewModel.SkillId = skill.SkillId;
                    skillViewModel.IsAdmin = _userIsProgenyAdmin;
                    if (skillViewModel.AccessLevel >= userAccessLevel)
                    {
                        model.Add(skillViewModel);
                    }

                }
            }
            else
            {
                SkillViewModel skillViewModel = new SkillViewModel();
                skillViewModel.ProgenyId = _progId;
                skillViewModel.AccessLevel = (int)AccessLevel.Public;
                skillViewModel.Description = "The skills list is empty.";
                skillViewModel.Category = "";
                skillViewModel.Name = "No items";
                skillViewModel.SkillFirstObservation = DateTime.UtcNow;

                skillViewModel.IsAdmin = _userIsProgenyAdmin;

                model.Add(skillViewModel);
            }

            model[0].Progeny = progeny;
            return View(model);

        }
    }
}