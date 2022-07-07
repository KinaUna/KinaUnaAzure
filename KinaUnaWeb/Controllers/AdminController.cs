using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KinaUnaWeb.Controllers
{
    public class AdminController: Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly WebDbContext _context;
        private readonly IHubContext<WebNotificationHub> _hubContext;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly string _adminEmail = Constants.AdminEmail;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly ILanguagesHttpClient _languagesHttpClient;
        private readonly ITranslationsHttpClient _translationsHttpClient;
        public AdminController(WebDbContext context, IBackgroundTaskQueue queue, IHubContext<WebNotificationHub> hubContext, IPushMessageSender pushMessageSender, IAuthHttpClient authHttpClient,
            IUserInfosHttpClient userInfosHttpClient, ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient)
        {
            _context = context;
            Queue = queue;
            _hubContext = hubContext;
            _pushMessageSender = pushMessageSender;
            _authHttpClient = authHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _languagesHttpClient = languagesHttpClient;
            _translationsHttpClient = translationsHttpClient;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private IBackgroundTaskQueue Queue { get; }

        public async Task<IActionResult> Index()
        {
            // Todo: Implement Admin as role instead
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? Constants.DefaultUserEmail;
            
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            List<UserInfo> deletedUserInfosList = await _userInfosHttpClient.GetDeletedUserInfos();
            if (deletedUserInfosList.Any())
            {
                foreach (UserInfo deletedUserInfo in deletedUserInfosList)
                {
                    if (deletedUserInfo.Deleted && deletedUserInfo.DeletedTime < DateTime.UtcNow - TimeSpan.FromDays(30))
                    {
                        UserInfo authResponseUserInfo = await _authHttpClient.CheckDeleteUser(deletedUserInfo);
                        if (authResponseUserInfo != null && authResponseUserInfo.UserId == deletedUserInfo.UserId && deletedUserInfo.Deleted && deletedUserInfo.DeletedTime < DateTime.UtcNow - TimeSpan.FromDays(30))
                        {
                            await _userInfosHttpClient.RemoveUserInfoForGood(deletedUserInfo);
                        }
                    }
                }
            }

            return View();
        }

        [Authorize]
        public async Task<IActionResult> ManageLanguages()
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            List<KinaUnaLanguage> model = await _languagesHttpClient.GetAllLanguages();
            
            return View(model);
        }

        [Authorize]
        public async Task<ActionResult> AddLanguage()
        {

            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            KinaUnaLanguage model = new KinaUnaLanguage();
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddLanguage(KinaUnaLanguage model)
        {

            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            
            KinaUnaLanguage newLanguage = await _languagesHttpClient.AddLanguage(model);
            _ = await _languagesHttpClient.GetLanguage(newLanguage.Id, true);
            _ = await _languagesHttpClient.GetAllLanguages(true);
            
            return RedirectToAction("ManageLanguages", "Admin");
        }

        [Authorize]
        public async Task<ActionResult> EditLanguage(int languageId)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            KinaUnaLanguage model = await _languagesHttpClient.GetLanguage(languageId);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditLanguage(KinaUnaLanguage model)
        {

            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            _ = await _languagesHttpClient.UpdateLanguage(model);
            _ = await _languagesHttpClient.GetLanguage(model.Id, true);
            _ = await _languagesHttpClient.GetAllLanguages(true);
            return RedirectToAction("ManageLanguages", "Admin");
        }

        [Authorize]
        public async Task<ActionResult> DeleteLanguage(int languageId)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            KinaUnaLanguage model = await _languagesHttpClient.GetLanguage(languageId);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteLanguage(KinaUnaLanguage model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            
            _ = await _languagesHttpClient.DeleteLanguage(model); 
            _ = await _languagesHttpClient.GetLanguage(model.Id, true);
            _ = await _languagesHttpClient.GetAllLanguages(true);
            return RedirectToAction("ManageLanguages", "Admin");
        }

        [Authorize]
        public async Task<IActionResult> ManageTranslations()
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            ManageTranslationsViewModel model = new ManageTranslationsViewModel();

            model.Translations = await _translationsHttpClient.GetAllTranslations();
            model.PagesList = new List<string>();
            model.WordsList = new List<string>();
            foreach (TextTranslation translationItem in model.Translations)
            {
                if (!model.PagesList.Contains(translationItem.Page))
                {
                    model.PagesList.Add(translationItem.Page);
                }

                if (!model.WordsList.Contains(translationItem.Word))
                {
                    model.WordsList.Add(translationItem.Word);
                }
            }

            model.LanguagesList = await _languagesHttpClient.GetAllLanguages();
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> GetPageTranslations(string pageName)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            ManageTranslationsViewModel model = new ManageTranslationsViewModel();

            model.Translations = await _translationsHttpClient.GetAllTranslations();
            model.PagesList = new List<string>();
            model.WordsList = new List<string>();
            foreach (TextTranslation translationItem in model.Translations)
            {
                if (translationItem.Page.Trim().ToUpper() == pageName.Trim().ToUpper())
                {
                    if (!model.PagesList.Contains(translationItem.Page))
                    {
                        model.PagesList.Add(translationItem.Page);
                    }

                    if (!model.WordsList.Contains(translationItem.Word))
                    {
                        model.WordsList.Add(translationItem.Word);
                    }
                }
            }

            model.LanguagesList = await _languagesHttpClient.GetAllLanguages();
            return PartialView("TranslationPagePartial", model);
        }

        [Authorize]
        public async Task<IActionResult> EditTranslation(int translationId)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation model = await _translationsHttpClient.GetTranslationById(translationId);

            return PartialView("EditTranslationPartial", model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditTranslation([FromBody] TextTranslation model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation updateTranslation = await _translationsHttpClient.GetTranslationById(model.Id);
            updateTranslation.Translation = model.Translation;
            TextTranslation updatedTranslation = await _translationsHttpClient.UpdateTranslation(updateTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await _translationsHttpClient.GetTranslationById(updatedTranslation.Id, true);
                _ = await _translationsHttpClient.GetTranslation(updatedTranslation.Word, updateTranslation.Page, lang.Id, true);
                _ = await _translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return PartialView("EditTranslationPartial", updateTranslation);
        }

        [Authorize]
        public async Task<IActionResult> DeleteTranslationItem(int translationId)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            List<TextTranslation> translationsList = await _translationsHttpClient.GetAllTranslations();

            TextTranslation model = translationsList.SingleOrDefault(t => t.Id == translationId);

            return PartialView("DeleteTranslationItemPartial", model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteTranslationItem([FromBody] TextTranslation model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation updateTranslation = await _translationsHttpClient.GetTranslationById(model.Id);
            updateTranslation.Translation = model.Translation;
            _ = await _translationsHttpClient.DeleteSingleItemTranslation(updateTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await _translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return PartialView("DeleteTranslationItemPartial", updateTranslation);
        }

        [Authorize]
        public async Task<IActionResult> DeleteTranslation(string word, string page)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            List<TextTranslation> translationsList = await _translationsHttpClient.GetAllTranslations();

            TextTranslation model = translationsList.SingleOrDefault(t => t.Word == word && t.Page == page && t.LanguageId == 1);

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTranslation(TextTranslation model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation deleteTranslation = await _translationsHttpClient.GetTranslationById(model.Id);
            await _translationsHttpClient.DeleteTranslation(deleteTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await _translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return RedirectToAction("ManageTranslations", "Admin");
        }

        public IActionResult SendAdminMessage()
        {
            // Todo: Implement Admin as role instead
            WebNotification model = new WebNotification();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendAdminMessage(WebNotification notification)
        {
            // Todo: Implement Admin as role instead
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userTimeZone = User.FindFirst("timezone")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            if (userEmail.ToUpper() == _adminEmail.ToUpper())
            {
                if (notification.To == "OnlineUsers")
                {
                    notification.DateTime = DateTime.UtcNow;
                    notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userTimeZone));
                    notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await _hubContext.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));
                }
                else
                {
                    UserInfo userinfo;
                    if (notification.To.Contains('@'))
                    {
                        userinfo = await _userInfosHttpClient.GetUserInfo(notification.To);
                        notification.To = userinfo.UserId;
                    }
                    else
                    {
                        userinfo = await _userInfosHttpClient.GetUserInfoByUserId(notification.To);
                    }

                    notification.DateTime = DateTime.UtcNow;
                    await _context.WebNotificationsDb.AddAsync(notification);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.User(userinfo.UserId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));

                    WebNotification webNotification = new WebNotification();
                    webNotification.Title = "Notification Sent" ;
                    webNotification.Message = "To: " + notification.To + "<br/>From: " + notification.From + "<br/><br/>Message: <br/>" + notification.Message;
                    webNotification.From = Constants.AppName + " Notification System";
                    webNotification.Type = "Notification";
                    webNotification.DateTime = DateTime.UtcNow;
                    webNotification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    webNotification.DateTimeString = webNotification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                    await _hubContext.Clients.User(userId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
                }
            }

            notification.Title = "Notification Added";
            return View(notification);
        }

        public IActionResult SendPush()
        {
            // Todo: Implement Admin as role instead
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            string userId = User.FindFirst("sub")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            PushNotification notification = new PushNotification();
            notification.UserId = userId;
            return View(notification);
        }

        [HttpPost]
        public async Task<IActionResult> SendPush(PushNotification notification)
        {
            string userEmail = User.FindFirst("email")?.Value ?? "NoUser";
            if (userEmail.ToUpper() != _adminEmail.ToUpper())
            {
                return RedirectToAction("Index", "Home");
            }

            if (notification.UserId.Contains('@'))
            {
                UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(notification.UserId);
                notification.UserId = userinfo.UserId;
            }

            await _pushMessageSender.SendMessage(notification.UserId, notification.Title, notification.Message,
                notification.Link, "kinaunapush");
            notification.Title = "Message Sent";
            return View(notification);
        }
        
    }
}
