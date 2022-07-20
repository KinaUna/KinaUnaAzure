using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class VocabularyController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IWordsHttpClient _wordsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public VocabularyController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IWordsHttpClient wordsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _wordsHttpClient = wordsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            VocabularyListViewModel model = new VocabularyListViewModel();
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

            model.VocabularyList = new List<VocabularyItemViewModel>();
            List<VocabularyItem> wordList = await _wordsHttpClient.GetWordsList(childId, userAccessLevel);
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
                        vocabularyItemViewModel.IsAdmin = model.IsAdmin;
                        vocabularyItemViewModel.WordId = vocabularyItem.WordId;
                        model.VocabularyList.Add(vocabularyItemViewModel);
                    }
                    
                }
            }
            
            model.Progeny = progeny;

            List<WordDateCount> dateTimesList = new List<WordDateCount>();
            int wordCount = 0;
            foreach (VocabularyItemViewModel vocabularyItemViewModel in model.VocabularyList)
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