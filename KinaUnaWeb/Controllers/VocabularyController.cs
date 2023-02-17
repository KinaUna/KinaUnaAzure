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
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace KinaUnaWeb.Controllers
{
    public class VocabularyController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IWordsHttpClient _wordsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;
        public VocabularyController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IWordsHttpClient wordsHttpClient,
            IUserAccessHttpClient userAccessHttpClient, IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _wordsHttpClient = wordsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
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

        [HttpGet]
        public async Task<IActionResult> AddVocabulary()
        {
            VocabularyItemViewModel model = new VocabularyItemViewModel();

            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {

                accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
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

            model.Progeny = accessList[0];

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
        public async Task<IActionResult> AddVocabulary(VocabularyItemViewModel model)
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


            VocabularyItem vocabItem = new VocabularyItem();
            vocabItem.Word = model.Word;
            vocabItem.ProgenyId = model.ProgenyId;
            vocabItem.DateAdded = DateTime.UtcNow;
            if (model.Date == null)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            vocabItem.Date = model.Date;
            vocabItem.Description = model.Description;
            vocabItem.Language = model.Language;
            vocabItem.SoundsLike = model.SoundsLike;
            vocabItem.AccessLevel = model.AccessLevel;
            vocabItem.Author = model.CurrentUser.UserId;

            await _wordsHttpClient.AddWord(vocabItem);

            
            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= vocabItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string vocabTimeString;
                        DateTime startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabItem.Date.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy");

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = model.CurrentUser.FullName();
                        notification.Message = "Word: " + vocabItem.Word + "\r\nLanguage: " + vocabItem.Language + vocabTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new word was added for " + progeny.NickName;
                        notification.Link = "/Vocabulary?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavocabulary" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> EditVocabulary(int itemId)
        {

            VocabularyItemViewModel model = new VocabularyItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            VocabularyItem vocab = await _wordsHttpClient.GetWord(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vocab.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.ProgenyId = vocab.ProgenyId;
            model.AccessLevel = vocab.AccessLevel;
            model.Author = vocab.Author;
            model.Date = vocab.Date;
            if (model.Date == null)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            model.DateAdded = vocab.DateAdded;
            model.Description = vocab.Description;
            model.Language = vocab.Language;
            model.SoundsLike = vocab.SoundsLike;
            model.Word = vocab.Word;
            model.WordId = vocab.WordId;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

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
        public async Task<IActionResult> EditVocabulary(VocabularyItemViewModel model)
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
                VocabularyItem editedWord = new VocabularyItem();
                editedWord.Author = model.Author;
                editedWord.ProgenyId = model.ProgenyId;
                editedWord.AccessLevel = model.AccessLevel;
                editedWord.Date = model.Date;
                if (editedWord.Date == null)
                {
                    editedWord.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedWord.DateAdded = model.DateAdded;
                editedWord.Description = model.Description;
                editedWord.Language = model.Language;
                editedWord.SoundsLike = model.SoundsLike;
                editedWord.Word = model.Word;
                editedWord.WordId = model.WordId;

                await _wordsHttpClient.UpdateWord(editedWord);

            }
            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVocabulary(int itemId)
        {
            VocabularyItemViewModel model = new VocabularyItemViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.VocabularyItem = await _wordsHttpClient.GetWord(itemId);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.VocabularyItem.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVocabulary(VocabularyItemViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            VocabularyItem vocab = await _wordsHttpClient.GetWord(model.VocabularyItem.WordId);
            Progeny prog = await _progenyHttpClient.GetProgeny(vocab.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _wordsHttpClient.DeleteWord(vocab.WordId);
            return RedirectToAction("Index", "Vocabulary");
        }
    }
}