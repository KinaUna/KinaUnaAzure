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
    public class VocabularyController : Controller
    {
        private WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = 2;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public VocabularyController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
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

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
            }

            List<VocabularyItemViewModel> model = new List<VocabularyItemViewModel>();
            List<VocabularyItem> wordList = _context.VocabularyDb.Where(w => w.ProgenyId == _progId).ToList();
            wordList = wordList.OrderBy(w => w.Date).ToList();
            if (wordList.Count != 0)
            {
                foreach (VocabularyItem w in wordList)
                {
                    VocabularyItemViewModel vIvm = new VocabularyItemViewModel();
                    vIvm.ProgenyId = w.ProgenyId;
                    vIvm.Date = w.Date;
                    vIvm.DateAdded = w.DateAdded;
                    vIvm.Description = w.Description;
                    vIvm.Language = w.Language;
                    vIvm.SoundsLike = w.SoundsLike;
                    vIvm.Word = w.Word;
                    vIvm.IsAdmin = _userIsProgenyAdmin;
                    vIvm.WordId = w.WordId;
                    model.Add(vIvm);
                }
            }
            else
            {
                VocabularyItemViewModel m = new VocabularyItemViewModel();
                m.ProgenyId = _progId;
                m.Date = DateTime.UtcNow;
                m.DateAdded = DateTime.UtcNow;
                m.Description = "The vocabulary list is empty.";
                m.Language = "English";
                m.SoundsLike = "";
                m.Word = "No words found.";
                m.IsAdmin = _userIsProgenyAdmin;
                model.Add(m);
            }

            model[0].Progeny = progeny;

            List<WordDateCount> dateTimesList = new List<WordDateCount>();
            int wordCount = 0;
            foreach (VocabularyItemViewModel vIvm in model)
            {
                wordCount++;
                if (vIvm.Date != null)
                {

                    if (dateTimesList.SingleOrDefault(d => d.WordDate.Date == vIvm.Date.Value.Date) == null)
                    {
                        WordDateCount newDate = new WordDateCount();
                        newDate.WordDate = vIvm.Date.Value.Date;
                        newDate.WordCount = wordCount;
                        dateTimesList.Add(newDate);
                    }
                    else
                    {
                        WordDateCount wrdDateCount = dateTimesList.SingleOrDefault(d => d.WordDate.Date == vIvm.Date.Value.Date);
                        if (wrdDateCount != null)
                        {
                            wrdDateCount.WordCount = wordCount;
                        }
                    }

                }

            }

            ViewBag.ChartData = dateTimesList;
            return View(model);
        }
    }
}