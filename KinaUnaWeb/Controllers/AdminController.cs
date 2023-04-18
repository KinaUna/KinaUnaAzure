using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaWeb.Hubs;
using KinaUnaWeb.Models.AdminViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class AdminController: Controller
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IHubContext<WebNotificationHub> _hubContext;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly string _adminEmail;
        private readonly IAuthHttpClient _authHttpClient;
        private readonly ILanguagesHttpClient _languagesHttpClient;
        private readonly ITranslationsHttpClient _translationsHttpClient;
        private readonly IPageTextsHttpClient _pageTextsHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IWebNotificationsService _webNotificationsService;

        public AdminController(IBackgroundTaskQueue queue, IHubContext<WebNotificationHub> hubContext, IPushMessageSender pushMessageSender, IAuthHttpClient authHttpClient,
            IUserInfosHttpClient userInfosHttpClient, ILanguagesHttpClient languagesHttpClient, ITranslationsHttpClient translationsHttpClient, IPageTextsHttpClient pageTextsHttpClient,
            ImageStore imageStore, IWebNotificationsService webNotificationsService, IConfiguration configuration)
        {
            Queue = queue;
            _hubContext = hubContext;
            _pushMessageSender = pushMessageSender;
            _authHttpClient = authHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _languagesHttpClient = languagesHttpClient;
            _translationsHttpClient = translationsHttpClient;
            _pageTextsHttpClient = pageTextsHttpClient;
            _imageStore = imageStore;
            _webNotificationsService = webNotificationsService;
            _adminEmail = configuration.GetValue<string>("AdminEmail");
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private IBackgroundTaskQueue Queue { get; }

        public async Task<IActionResult> Index()
        {
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

            KinaUnaLanguage model = new();
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

            ManageTranslationsViewModel model = new()
            {
                Translations = await _translationsHttpClient.GetAllTranslations(),
                PagesList = new List<string>(),
                WordsList = new List<string>()
            };

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
        [HttpPost]
        public async Task<IActionResult> LoadPageTranslations([FromBody] TextTranslationPageListModel model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            model.Translations = await _translationsHttpClient.GetAllTranslations();
            model.Translations = model.Translations.Where(t => t.Page.Trim().ToUpper() == model.Page.Trim().ToUpper()).ToList();
            model.Translations = model.Translations.OrderBy(t => t.Word).ThenBy(t => t.LanguageId).ToList();

            model.LanguagesList = await _languagesHttpClient.GetAllLanguages();

            return Json(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePageTranslation([FromBody] TextTranslation translation)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation existingTextTranslation = await _translationsHttpClient.GetTranslationById(translation.Id);
            existingTextTranslation.Translation = translation.Translation;

            TextTranslation updatedTextTranslation = await _translationsHttpClient.UpdateTranslation(existingTextTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await _translationsHttpClient.GetTranslationById(updatedTextTranslation.Id, true);
                _ = await _translationsHttpClient.GetTranslation(updatedTextTranslation.Word, updatedTextTranslation.Page, lang.Id, true);
                _ = await _translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return Json(updatedTextTranslation);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeletePageTranslation([FromBody] TextTranslation translation)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation existingTextTranslation = await _translationsHttpClient.GetTranslationById(translation.Id);
            existingTextTranslation.Translation = translation.Translation;

            _ = await _translationsHttpClient.DeleteTranslation(existingTextTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await _translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return Json(existingTextTranslation);
        }
        
        [Authorize]
        public async Task<IActionResult> EditText(int id, string returnUrl = "")
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo != null && userInfo.IsKinaUnaAdmin)
            {
                KinaUnaText model = await _pageTextsHttpClient.GetPageTextById(id);
                model.ReturnUrl = returnUrl;

                return PartialView("_EditTextPartial", model);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditText([FromForm] KinaUnaText model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo != null && userInfo.IsKinaUnaAdmin)
            {
                KinaUnaText updateText = await _pageTextsHttpClient.GetPageTextById(model.Id);
                updateText.Text = model.Text;
                KinaUnaText updatedText = await _pageTextsHttpClient.UpdatePageText(updateText);

                // Update caches
                await _pageTextsHttpClient.GetPageTextById(updatedText.Id, true);
                List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
                foreach (KinaUnaLanguage lang in languages)
                {
                    _ = await _pageTextsHttpClient.GetPageTextByTitle(updatedText.Title, updatedText.Page, lang.Id, true);
                    await _pageTextsHttpClient.GetAllKinaUnaTexts(lang.Id, true);
                }

                if (string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return PartialView("_EditTextPartial", updateText);
                }
                
                return Redirect(model.ReturnUrl);
                
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> EditTextTranslation(int textId, int languageId)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo != null && userInfo.IsKinaUnaAdmin)
            {
                List<KinaUnaText> allTexts = await _pageTextsHttpClient.GetAllKinaUnaTexts(languageId, true);
                KinaUnaText textToEdit = allTexts.SingleOrDefault(t => t.TextId == textId && t.LanguageId == languageId);
                //if (textToEdit != null)
                //{
                //    textToEdit.Text = "";
                //}

                return Json(textToEdit);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [Produces("application/json")]
        [HttpPost]
        public async Task<IActionResult> EditTextTranslation([FromForm] KinaUnaText model)
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo != null && userInfo.IsKinaUnaAdmin)
            {
                KinaUnaText updateText = await _pageTextsHttpClient.GetPageTextById(model.Id);
                updateText.Text = model.Text;
                KinaUnaText updatedText = await _pageTextsHttpClient.UpdatePageText(updateText);
                
                // Update caches
                await _pageTextsHttpClient.GetPageTextById(updatedText.Id, true);
                List<KinaUnaLanguage> languages = await _languagesHttpClient.GetAllLanguages();
                foreach (KinaUnaLanguage lang in languages)
                {
                    _ = await _pageTextsHttpClient.GetPageTextByTitle(updatedText.Title, updatedText.Page, lang.Id, true);
                    await _pageTextsHttpClient.GetAllKinaUnaTexts(lang.Id, true);
                }

                return Json(updatedText);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<ActionResult> SaveRtfFile(IList<IFormFile> UploadFiles)
        {
            try
            {
                if (UploadFiles.Any())
                {
                    foreach (IFormFile file in UploadFiles)
                    {
                        string filename;
                        await using (Stream stream = file.OpenReadStream())
                        {
                            filename = await _imageStore.SaveImage(stream, BlobContainers.KinaUnaTexts);
                        }

                        string resultName = _imageStore.UriFor(filename, BlobContainers.KinaUnaTexts);
                        Response.Clear();
                        Response.ContentType = "application/json; charset=utf-8";
                        Response.Headers.Add("name", resultName);
                        Response.StatusCode = 204;

                    }
                }
            }
            catch (Exception)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = 204;
            }

            return Content("");
        }

        [Authorize]
        public async Task<IActionResult> ManageTexts()
        {
            UserInfo userInfo = await _userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo != null && userInfo.IsKinaUnaAdmin)
            {
                ManageKinaUnaTextsViewModel model = new()
                {
                    LanguageId = Request.GetLanguageIdFromCookie(),
                    Texts = await _pageTextsHttpClient.GetAllKinaUnaTexts(1),
                    PagesList = new List<string>(),
                    TitlesList = new List<string>()
                };
                foreach (KinaUnaText textItem in model.Texts)
                {
                    if (!model.PagesList.Contains(textItem.Page))
                    {
                        model.PagesList.Add(textItem.Page);
                    }

                    if (!model.TitlesList.Contains(textItem.Title))
                    {
                        model.TitlesList.Add(textItem.Title);
                    }
                }

                model.LanguagesList = await _languagesHttpClient.GetAllLanguages();
                KinaUnaLanguage selectedLanguage = model.LanguagesList.SingleOrDefault(l => l.Id == model.LanguageId);
                if (selectedLanguage != null)
                {
                    model.Language = model.LanguagesList.IndexOf(selectedLanguage);
                }
                else
                {
                    model.Language = 0;
                }
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }
        public IActionResult SendAdminMessage()
        {
            // Todo: Implement Admin as role instead
            WebNotification model = new();
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

                    notification = await _webNotificationsService.SaveNotification(notification);
                    
                    await _hubContext.Clients.User(userinfo.UserId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));

                    WebNotification webNotification = new()
                    {
                        Title = "Notification Sent",
                        Message = "To: " + notification.To + "<br/>From: " + notification.From + "<br/><br/>Message: <br/>" + notification.Message,
                        From = Constants.AppName + " Notification System",
                        Type = "Notification",
                        DateTime = DateTime.UtcNow
                    };
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

            PushNotification notification = new()
            {
                UserId = userId
            };

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
