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
    public class VocabularyController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public VocabularyController(IProgenyHttpClient progenyHttpClient)
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

            List<VocabularyItemViewModel> model = new List<VocabularyItemViewModel>();
            List<VocabularyItem> wordList = await _progenyHttpClient.GetWordsList(_progId, userAccessLevel); // _context.VocabularyDb.Where(w => w.ProgenyId == _progId).ToList();
            wordList = wordList.OrderBy(w => w.Date).ToList();
            if (wordList.Count != 0)
            {
                foreach (VocabularyItem vocabularyItem in wordList)
                {
                    if (vocabularyItem.AccessLevel >= userAccessLevel)
                    {
                        VocabularyItemViewModel vocabularyItemViewModel = new VocabularyItemViewModel();
                        vocabularyItemViewModel.ProgenyId = vocabularyItem.ProgenyId;
                        vocabularyItemViewModel.Date = vocabularyItem.Date;
                        vocabularyItemViewModel.DateAdded = vocabularyItem.DateAdded;
                        vocabularyItemViewModel.Description = vocabularyItem.Description;
                        vocabularyItemViewModel.Language = vocabularyItem.Language;
                        vocabularyItemViewModel.SoundsLike = vocabularyItem.SoundsLike;
                        vocabularyItemViewModel.Word = vocabularyItem.Word;
                        vocabularyItemViewModel.IsAdmin = _userIsProgenyAdmin;
                        vocabularyItemViewModel.WordId = vocabularyItem.WordId;
                        model.Add(vocabularyItemViewModel);
                    }
                    
                }
            }
            else
            {
                VocabularyItemViewModel vocabularyItemViewModel = new VocabularyItemViewModel();
                vocabularyItemViewModel.ProgenyId = _progId;
                vocabularyItemViewModel.Date = DateTime.UtcNow;
                vocabularyItemViewModel.DateAdded = DateTime.UtcNow;
                vocabularyItemViewModel.Description = "The vocabulary list is empty.";
                vocabularyItemViewModel.Language = "English";
                vocabularyItemViewModel.SoundsLike = "";
                vocabularyItemViewModel.Word = "No words found.";
                vocabularyItemViewModel.IsAdmin = _userIsProgenyAdmin;
                model.Add(vocabularyItemViewModel);
            }

            model[0].Progeny = progeny;

            List<WordDateCount> dateTimesList = new List<WordDateCount>();
            int wordCount = 0;
            foreach (VocabularyItemViewModel vocabularyItemViewModel in model)
            {
                wordCount++;
                if (vocabularyItemViewModel.Date != null)
                {
                    if (dateTimesList.SingleOrDefault(d => d.WordDate.Date == vocabularyItemViewModel.Date.Value.Date) == null)
                    {
                        WordDateCount newDate = new WordDateCount();
                        newDate.WordDate = vocabularyItemViewModel.Date.Value.Date;
                        newDate.WordCount = wordCount;
                        dateTimesList.Add(newDate);
                    }
                    else
                    {
                        WordDateCount wrdDateCount = dateTimesList.SingleOrDefault(d => d.WordDate.Date == vocabularyItemViewModel.Date.Value.Date);
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