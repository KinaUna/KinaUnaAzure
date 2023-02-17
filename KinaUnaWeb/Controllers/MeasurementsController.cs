using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KinaUnaWeb.Controllers
{
    public class MeasurementsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IMeasurementsHttpClient _measurementsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;
        public MeasurementsController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IMeasurementsHttpClient measurementsHttpClient, IUserAccessHttpClient userAccessHttpClient,
            IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService )
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _measurementsHttpClient = measurementsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            MeasurementViewModel model = new MeasurementViewModel();
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
            
            
            // ToDo: Implement _progenyClient.GetMeasurements()
            List<Measurement> mList = await _measurementsHttpClient.GetMeasurementsList(childId, userAccessLevel);
            List<Measurement> measurementsList = new List<Measurement>();
            foreach (Measurement m in mList)
            {
                if (m.AccessLevel >= userAccessLevel)
                {
                    measurementsList.Add(m);
                }
            }
            measurementsList = measurementsList.OrderBy(m => m.Date).ToList();
            if (measurementsList.Count != 0)
            {
                model.MeasurementsList = measurementsList;

            }
            else
            {
                Measurement m = new Measurement();
                m.ProgenyId = childId;
                m.Date = DateTime.UtcNow;
                m.CreatedDate = DateTime.UtcNow;
                model.MeasurementsList = new List<Measurement>();
                model.MeasurementsList.Add(m);
            }
            model.Progeny = progeny;
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            if (User.Identity != null && User.Identity.IsAuthenticated && userEmail != null && model.CurrentUser.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
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
        public async Task<IActionResult> AddMeasurement(MeasurementViewModel model)
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
            Measurement measurementItem = new Measurement();
            measurementItem.ProgenyId = model.ProgenyId;
            measurementItem.CreatedDate = DateTime.UtcNow;
            measurementItem.Date = model.Date;
            measurementItem.Height = model.Height;
            measurementItem.Weight = model.Weight;
            measurementItem.Circumference = model.Circumference;
            measurementItem.HairColor = model.HairColor;
            measurementItem.EyeColor = model.EyeColor;
            measurementItem.AccessLevel = model.AccessLevel;
            measurementItem.Author = model.CurrentUser.UserId;

            await _measurementsHttpClient.AddMeasurement(measurementItem);

            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= measurementItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = model.CurrentUser.UserId;
                        notification.Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new measurement was added for " + progeny.NickName;
                        notification.Link = "/Measurements?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunameasurement" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> EditMeasurement(int itemId)
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Measurement measurement = await _measurementsHttpClient.GetMeasurement(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.ProgenyId = measurement.ProgenyId;
            model.MeasurementId = measurement.MeasurementId;
            model.AccessLevel = measurement.AccessLevel;
            model.Author = measurement.Author;
            model.CreatedDate = measurement.CreatedDate;
            model.Date = measurement.Date;
            model.Height = measurement.Height;
            model.Weight = measurement.Weight;
            model.Circumference = measurement.Circumference;
            model.HairColor = measurement.HairColor;
            model.EyeColor = measurement.EyeColor;
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
        public async Task<IActionResult> EditMeasurement(MeasurementViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? Constants.DefaultUserEmail;
            UserInfo userinfo = await _userInfosHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.IsInAdminList(userinfo.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Measurement editedMeasurement = new Measurement();
            editedMeasurement.ProgenyId = model.ProgenyId;
            editedMeasurement.MeasurementId = model.MeasurementId;
            editedMeasurement.AccessLevel = model.AccessLevel;
            editedMeasurement.Author = model.Author;
            editedMeasurement.CreatedDate = model.CreatedDate;
            editedMeasurement.Date = model.Date;
            editedMeasurement.Height = model.Height;
            editedMeasurement.Weight = model.Weight;
            editedMeasurement.Circumference = model.Circumference;
            editedMeasurement.HairColor = model.HairColor;
            editedMeasurement.EyeColor = model.EyeColor;

            await _measurementsHttpClient.UpdateMeasurement(editedMeasurement);

            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMeasurement(int itemId)
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Measurement = await _measurementsHttpClient.GetMeasurement(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Measurement.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(MeasurementViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Measurement measurement = await _measurementsHttpClient.GetMeasurement(model.Measurement.MeasurementId);
            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _measurementsHttpClient.DeleteMeasurement(measurement.MeasurementId);

            return RedirectToAction("Index", "Measurements");
        }
    }
}