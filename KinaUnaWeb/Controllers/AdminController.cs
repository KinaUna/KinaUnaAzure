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
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    // Todo: Split this controller into multiple controllers.
    /// <summary>
    /// Pages and actions for KinaUna Admins.
    /// </summary>
    /// <param name="hubContext"></param>
    /// <param name="pushMessageSender"></param>
    /// <param name="authHttpClient"></param>
    /// <param name="userInfosHttpClient"></param>
    /// <param name="languagesHttpClient"></param>
    /// <param name="translationsHttpClient"></param>
    /// <param name="pageTextsHttpClient"></param>
    /// <param name="imageStore"></param>
    /// <param name="webNotificationsService"></param>
    public class AdminController(
        IHubContext<WebNotificationHub> hubContext,
        IPushMessageSender pushMessageSender,
        IAuthHttpClient authHttpClient,
        IUserInfosHttpClient userInfosHttpClient,
        ILanguagesHttpClient languagesHttpClient,
        ITranslationsHttpClient translationsHttpClient,
        IPageTextsHttpClient pageTextsHttpClient,
        ImageStore imageStore,
        IWebNotificationsService webNotificationsService)
        : Controller
    {
        /// <summary>
        /// Index page for the AdminController. Only available to KinaUna Admins.
        /// The starting point for managing the site.
        /// </summary>
        /// <returns>View.</returns>
        public async Task<IActionResult> Index()
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            List<UserInfo> deletedUserInfosList = await userInfosHttpClient.GetDeletedUserInfos();
            if (deletedUserInfosList.Count == 0) return View();

            foreach (UserInfo deletedUserInfo in deletedUserInfosList)
            {
                if (!deletedUserInfo.Deleted || deletedUserInfo.DeletedTime >= DateTime.UtcNow - TimeSpan.FromDays(30)) continue;

                UserInfo authResponseUserInfo = await authHttpClient.CheckDeleteUser(deletedUserInfo);
                if (authResponseUserInfo != null && authResponseUserInfo.UserId == deletedUserInfo.UserId && deletedUserInfo.Deleted && deletedUserInfo.DeletedTime < DateTime.UtcNow - TimeSpan.FromDays(30))
                {
                    await userInfosHttpClient.RemoveUserInfoForGood(deletedUserInfo);
                }
            }

            await userInfosHttpClient.GetAllUserInfos();
            return View();
        }

        /// <summary>
        /// Page for managing KinaUnaLanguages. Only available to KinaUna Admins.
        /// </summary>
        /// <returns>View.</returns>
        [Authorize]
        public async Task<IActionResult> ManageLanguages()
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            List<KinaUnaLanguage> model = await languagesHttpClient.GetAllLanguages();
            
            return View(model);
        }

        /// <summary>
        /// Page for adding a new KinaUnaLanguage. Only available to KinaUna Admins.
        /// </summary>
        /// <returns>View with KinaUnaLanguage model.</returns>
        [Authorize]
        public async Task<ActionResult> AddLanguage()
        {

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            KinaUnaLanguage model = new();
            return View(model);
        }

        /// <summary>
        /// HttpPost action for adding a new KinaUnaLanguage. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="model">KinaUnaLanguage object to add.</param>
        /// <returns>Redirects to ManageLanguages page.</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddLanguage(KinaUnaLanguage model)
        {

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            
            KinaUnaLanguage newLanguage = await languagesHttpClient.AddLanguage(model);
            _ = await languagesHttpClient.GetLanguage(newLanguage.Id, true);
            _ = await languagesHttpClient.GetAllLanguages(true);
            
            return RedirectToAction("ManageLanguages", "Admin");
        }

        /// <summary>
        /// Edit page for a KinaUnaLanguage. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage to edit.</param>
        /// <returns>View with KinaUnaLanguage model.</returns>
        [Authorize]
        public async Task<ActionResult> EditLanguage(int languageId)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            KinaUnaLanguage model = await languagesHttpClient.GetLanguage(languageId);
            return View(model);
        }

        /// <summary>
        /// HttpPost action for editing a KinaUnaLanguage. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="model">KinaUnaLanguage object with the updated properties.</param>
        /// <returns>Redirects to ManageLanguages page.</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditLanguage(KinaUnaLanguage model)
        {

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            _ = await languagesHttpClient.UpdateLanguage(model);
            _ = await languagesHttpClient.GetLanguage(model.Id, true);
            _ = await languagesHttpClient.GetAllLanguages(true);

            return RedirectToAction("ManageLanguages", "Admin");
        }

        /// <summary>
        /// Delete page for a KinaUnaLanguage. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="languageId">The Id of the KinaUnaLanguage to delete.</param>
        /// <returns>View with KinaUnaLanguage model.</returns>
        [Authorize]
        public async Task<ActionResult> DeleteLanguage(int languageId)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            KinaUnaLanguage model = await languagesHttpClient.GetLanguage(languageId);

            return View(model);
        }

        /// <summary>
        /// HttpPost action for deleting a KinaUnaLanguage. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="model">KinaUnaLanguage object to delete.</param>
        /// <returns>Redirects to ManageLanguages.</returns>
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteLanguage(KinaUnaLanguage model)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }
            
            _ = await languagesHttpClient.DeleteLanguage(model); 
            _ = await languagesHttpClient.GetLanguage(model.Id, true);
            _ = await languagesHttpClient.GetAllLanguages(true);

            return RedirectToAction("ManageLanguages", "Admin");
        }

        /// <summary>
        /// Page for managing TextTranslations. Only available to KinaUna Admins.
        /// </summary>
        /// <returns>View with ManageTranslationsViewModel.</returns>
        [Authorize]
        public async Task<IActionResult> ManageTranslations()
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            ManageTranslationsViewModel model = new()
            {
                Translations = await translationsHttpClient.GetAllTranslations(),
                PagesList = [],
                WordsList = []
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

            model.LanguagesList = await languagesHttpClient.GetAllLanguages();
            return View(model);
        }

        /// <summary>
        /// HttpPost API endpoint for loading translations for a specific page. Only available to KinaUna Admins.
        /// Gets all languages and all translations for a specific page defined in the TextTranslationPageListModel.
        /// </summary>
        /// <param name="model">TextTranslationPageListModel object.</param>
        /// <returns>Json of TextTranslationPageListModel.</returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> LoadPageTranslations([FromBody] TextTranslationPageListModel model)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            model.Translations = await translationsHttpClient.GetAllTranslations();
            model.Translations = model.Translations.Where(t => t.Page.Trim().Equals(model.Page.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToList();
            model.Translations = [.. model.Translations.OrderBy(t => t.Word).ThenBy(t => t.LanguageId)];

            model.LanguagesList = await languagesHttpClient.GetAllLanguages();

            return Json(model);
        }

        /// <summary>
        /// HttpPost API endpoint for updating a TextTranslation. Only available to KinaUna Admins.
        /// Also updates the cache.
        /// </summary>
        /// <param name="translation">The TextTranslation with the updated properties.</param>
        /// <returns>Json of the updated TextTranslation object.</returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePageTranslation([FromBody] TextTranslation translation)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation existingTextTranslation = await translationsHttpClient.GetTranslationById(translation.Id);
            existingTextTranslation.Translation = translation.Translation;

            TextTranslation updatedTextTranslation = await translationsHttpClient.UpdateTranslation(existingTextTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await translationsHttpClient.GetTranslationById(updatedTextTranslation.Id, true);
                _ = await translationsHttpClient.GetTranslation(updatedTextTranslation.Word, updatedTextTranslation.Page, lang.Id, true);
                _ = await translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return Json(updatedTextTranslation);
        }

        /// <summary>
        /// HttpPost API endpoint for deleting a TextTranslation. Only available to KinaUna Admins.
        /// Also deletes the translations in all other languages for the same word and page.
        /// Also updates the cache.
        /// </summary>
        /// <param name="translation">The TextTranslation object to delete.</param>
        /// <returns>Json with the deleted TextTranslation.</returns>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeletePageTranslation([FromBody] TextTranslation translation)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            TextTranslation existingTextTranslation = await translationsHttpClient.GetTranslationById(translation.Id);
            existingTextTranslation.Translation = translation.Translation;

            _ = await translationsHttpClient.DeleteTranslation(existingTextTranslation);

            // Update caches
            List<KinaUnaLanguage> languages = await languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await translationsHttpClient.GetAllTranslations(lang.Id, true);
            }

            return Json(existingTextTranslation);
        }

        /// <summary>
        /// PartialView for editing KinaUnaTexts. Only available to KinaUna Admins.
        /// For use in modals/popups.
        /// </summary>
        /// <param name="id">The Id of the KinaUnaText</param>
        /// <param name="returnUrl">The url to return to when editing is done. Empty string for reloading the updated text in the editor when done.</param>
        /// <returns>PartialView with KinaUnaText model.</returns>
        [Authorize]
        public async Task<IActionResult> EditText(int id, string returnUrl = "")
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            KinaUnaText model = await pageTextsHttpClient.GetPageTextById(id);
            model.ReturnUrl = returnUrl;

            return PartialView("_EditTextPartial", model);

        }

        /// <summary>
        /// HttpPost API endpoint for updating a KinaUnaText. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="model">KinaUnaText object with the updated properties.</param>
        /// <returns>PartialView with the updated KinaUnaText model, or if a return url is specified redirects to that url.</returns>
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditText([FromForm] KinaUnaText model)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            KinaUnaText updateText = await pageTextsHttpClient.GetPageTextById(model.Id);
            updateText.Text = model.Text;
            KinaUnaText updatedText = await pageTextsHttpClient.UpdatePageText(updateText);

            // Update caches
            await pageTextsHttpClient.GetPageTextById(updatedText.Id, true);
            List<KinaUnaLanguage> languages = await languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await pageTextsHttpClient.GetPageTextByTitle(updatedText.Title, updatedText.Page, lang.Id, true);
                await pageTextsHttpClient.GetAllKinaUnaTexts(lang.Id, true);
            }

            if (string.IsNullOrEmpty(model.ReturnUrl))
            {
                return PartialView("_EditTextPartial", updateText);
            }
                
            return Redirect(model.ReturnUrl);

        }

        /// <summary>
        /// HttpGet API endpoint for loading a KinaUnaText for translation. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="textId">The TextId of the KinaUnaText.</param>
        /// <param name="languageId">The language the text should be translated into.</param>
        /// <returns>Json of the KinaUnaText object.</returns>
        [Authorize]
        [Produces("application/json")]
        public async Task<IActionResult> EditTextTranslation(int textId, int languageId)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            List<KinaUnaText> allTexts = await pageTextsHttpClient.GetAllKinaUnaTexts(languageId, true);
            KinaUnaText textToEdit = allTexts.SingleOrDefault(t => t.TextId == textId && t.LanguageId == languageId);
            
            return Json(textToEdit);

        }

        /// <summary>
        /// HttpPost API endpoint for updating a KinaUnaText translation. Only available to KinaUna Admins.
        /// Also updates the cache.
        /// </summary>
        /// <param name="model">The KinaUnaText to update with the updated text.</param>
        /// <returns>Json of the updated KinaUnaText object.</returns>
        [Authorize]
        [ValidateAntiForgeryToken]
        [Produces("application/json")]
        [HttpPost]
        public async Task<IActionResult> EditTextTranslation([FromForm] KinaUnaText model)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            KinaUnaText updateText = await pageTextsHttpClient.GetPageTextById(model.Id);
            updateText.Text = model.Text;
            KinaUnaText updatedText = await pageTextsHttpClient.UpdatePageText(updateText);
                
            // Update caches
            await pageTextsHttpClient.GetPageTextById(updatedText.Id, true);
            List<KinaUnaLanguage> languages = await languagesHttpClient.GetAllLanguages();
            foreach (KinaUnaLanguage lang in languages)
            {
                _ = await pageTextsHttpClient.GetPageTextByTitle(updatedText.Title, updatedText.Page, lang.Id, true);
                await pageTextsHttpClient.GetAllKinaUnaTexts(lang.Id, true);
            }

            return Json(updatedText);

        }

        /// <summary>
        /// HttpPost API endpoint for uploading a files embedded in RTF text (Syncfusion text editor). Only available to KinaUna Admins.
        /// </summary>
        /// <param name="UploadFiles">List of IFormFile objects with the file data.</param>
        /// <returns>Empty string. The file names are returned via the response headers.</returns>
        [HttpPost]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ASP0019:Suggest using IHeaderDictionary.Append or the indexer", Justification = "From Syncfusion Sample")]
        // ReSharper disable once InconsistentNaming
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
                            string fileFormat = Path.GetExtension(file.FileName);
                            filename = await imageStore.SaveImage(stream, BlobContainers.KinaUnaTexts, fileFormat);
                        }

                        string resultName = imageStore.UriFor(filename, BlobContainers.KinaUnaTexts);
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

        /// <summary>
        /// Page for managing KinaUnaTexts. Only available to KinaUna Admins.
        /// </summary>
        /// <returns>View with a ManageKinaTextsViewModel.</returns>
        [Authorize]
        public async Task<IActionResult> ManageTexts()
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            ManageKinaUnaTextsViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie(),
                Texts = await pageTextsHttpClient.GetAllKinaUnaTexts(1),
                PagesList = [],
                TitlesList = []
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

            model.LanguagesList = await languagesHttpClient.GetAllLanguages();
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

        /// <summary>
        /// Page for sending push messages. Only available to KinaUna Admins.
        /// </summary>
        /// <returns>View with WebNotification model.</returns>
        public async Task<IActionResult> SendAdminMessage()
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            WebNotification model = new();
            return View(model);
        }

        /// <summary>
        /// HttpPost API endpoint for sending a web push message. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="notification">The WebNotification object with properties for sending push messages.</param>
        /// <returns>View for the SendAdminMessage page with WebNotification model.</returns>
        [HttpPost]
        public async Task<IActionResult> SendAdminMessage(WebNotification notification)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            if (notification.To == "OnlineUsers")
            {
                notification.DateTime = DateTime.UtcNow;
                notification.DateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById(userInfo.Timezone));
                notification.DateTimeString = notification.DateTime.ToString("dd-MMM-yyyy HH:mm");
                await hubContext.Clients.All.SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));
            }
            else
            {
                UserInfo userinfo;
                if (notification.To.Contains('@'))
                {
                    userinfo = await userInfosHttpClient.GetUserInfo(notification.To);
                    notification.To = userinfo.UserId;
                }
                else
                {
                    userinfo = await userInfosHttpClient.GetUserInfoByUserId(notification.To);
                }

                notification.DateTime = DateTime.UtcNow;

                notification = await webNotificationsService.SaveNotification(notification);

                await hubContext.Clients.User(userinfo.UserId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(notification));

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
                await hubContext.Clients.User(userInfo.UserId).SendAsync("ReceiveMessage", JsonConvert.SerializeObject(webNotification));
            }

            notification.Title = "Notification Added";

            return View(notification);
        }

        /// <summary>
        /// Page for sending mobile push messages. Only available to KinaUna Admins.
        /// </summary>
        /// <returns>View with PushNotification model.</returns>
        public async Task<IActionResult> SendPush()
        {

            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            PushNotification notification = new()
            {
                UserId = userInfo.UserId
            };

            return View(notification);
        }

        /// <summary>
        /// HttpPost API endpoint for sending a mobile push message. Only available to KinaUna Admins.
        /// </summary>
        /// <param name="notification">PushNotification object with properties for sending the push message.</param>
        /// <returns>SendPush View with PushNotification model.</returns>
        [HttpPost]
        public async Task<IActionResult> SendPush(PushNotification notification)
        {
            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin) return RedirectToAction("Index", "Home");

            if (notification.UserId.Contains('@'))
            {
                UserInfo userinfo = await userInfosHttpClient.GetUserInfo(notification.UserId);
                notification.UserId = userinfo.UserId;
            }

            await pushMessageSender.SendMessage(notification.UserId, notification.Title, notification.Message,
                notification.Link, "kinaunapush");
            notification.Title = "Message Sent";

            return View(notification);
        }
        
    }
}
