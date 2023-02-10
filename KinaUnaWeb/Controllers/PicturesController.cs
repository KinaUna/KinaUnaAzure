using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace KinaUnaWeb.Controllers
{
    public class PicturesController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly ITimelineHttpClient _timelineHttpClient;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IEmailSender _emailSender;
        private readonly IWebNotificationsService _webNotificationsService;

        public PicturesController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient,
            ILocationsHttpClient locationsHttpClient, ITimelineHttpClient timelineHttpClient, IPushMessageSender pushMessageSender, IEmailSender emailSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _timelineHttpClient = timelineHttpClient;
            _pushMessageSender = pushMessageSender;
            _emailSender = emailSender;
            _webNotificationsService = webNotificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 8, int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            PicturePageViewModel model = new PicturePageViewModel();
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

            bool isAdmin = false;
            if (progeny.IsInAdminList(userEmail))
            {
                isAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }


            model = await _mediaHttpClient.GetPicturePage(pageSize, id, progeny.Id, userAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.LanguageId = Request.GetLanguageIdFromCookie();
            model.IsAdmin = isAdmin;
            model.Progeny = progeny;
            model.SortBy = sortBy;
            model.PageSize = pageSize;
            foreach (Picture pic in model.PicturesList)
            {
                if (!pic.PictureLink600.StartsWith("https://"))
                {
                    pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Picture(int id, int childId = 0, string tagFilter = "", int sortBy = 1)
        {
            PictureViewModel model = new PictureViewModel();
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
            
            PictureViewModel picture = await _mediaHttpClient.GetPictureViewModel(id, userAccessLevel, sortBy, model.CurrentUser.Timezone);
            if (!picture.PictureLink.StartsWith("https://"))
            {
                picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
            }
            
            model.PictureId = picture.PictureId;
            model.PictureTime = picture.PictureTime;
            model.ProgenyId = picture.ProgenyId;
            model.Progeny = progeny;
            model.Owners = picture.Owners;
            model.PictureLink = picture.PictureLink;
            model.AccessLevel = picture.AccessLevel;
            model.Author = picture.Author;
            model.AccessLevelListEn[picture.AccessLevel].Selected = true;
            model.AccessLevelListDa[picture.AccessLevel].Selected = true;
            model.AccessLevelListDe[picture.AccessLevel].Selected = true;
            model.CommentThreadNumber = picture.CommentThreadNumber;
            model.Tags = picture.Tags;
            model.TagsList = picture.TagsList;
            model.Location = picture.Location;
            model.Latitude = picture.Latitude;
            model.Longtitude = picture.Longtitude;
            model.Altitude = picture.Altitude;
            model.PictureNumber = picture.PictureNumber;
            model.PictureCount = picture.PictureCount;
            model.PrevPicture = picture.PrevPicture;
            model.NextPicture = picture.NextPicture;
            model.CommentsList = picture.CommentsList ?? new List<Comment>();
            model.CommentsCount = picture.CommentsList?.Count ?? 0;
            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            model.UserId = model.CurrentUser.UserId;
            if (model.PictureTime != null && progeny.BirthDay.HasValue)
            {
                PictureTime picTime = new PictureTime(progeny.BirthDay.Value,
                    TimeZoneInfo.ConvertTimeToUtc(model.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone)),
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                model.PicTimeValid = true;
                model.PicTime = model.PictureTime.Value.ToString("dd MMMM yyyy HH:mm"); // Todo: Replace format string with global constant or user defined value
                model.PicYears = picTime.CalcYears();
                model.PicMonths = picTime.CalcMonths();
                model.PicWeeks = picTime.CalcWeeks();
                model.PicDays = picTime.CalcDays();
                model.PicHours = picTime.CalcHours();
                model.PicMinutes = picTime.CalcMinutes();
            }
            else
            {
                model.PicTimeValid = false;
                model.PicTime = "";
            }

            if (model.CommentsCount > 0)
            {
                foreach(Comment comment in model.CommentsList)
                {
                    UserInfo cmntAuthor = await _userInfosHttpClient.GetUserInfoByUserId(comment.Author);
                    string authorImg = cmntAuthor?.ProfilePicture ?? "";
                    string authorName = "";
                    if (!string.IsNullOrEmpty(authorImg))
                    {
                        if (!authorImg.ToLower().StartsWith("http"))
                        {
                            authorImg = _imageStore.UriFor(authorImg, "profiles");
                        }
                    }
                    else
                    {
                        authorImg = "/photodb/profile.jpg";
                    }
                    comment.AuthorImage = authorImg;

                    if (!string.IsNullOrEmpty(cmntAuthor.FirstName))
                    {
                        authorName = cmntAuthor.FirstName;
                    }
                    if (!string.IsNullOrEmpty(cmntAuthor.MiddleName))
                    {
                        authorName = authorName + " " + cmntAuthor.MiddleName;
                    }
                    if (!string.IsNullOrEmpty(cmntAuthor.LastName))
                    {
                        authorName = authorName + " " + cmntAuthor.LastName;
                    }

                    authorName = authorName.Trim();
                    if (string.IsNullOrEmpty(authorName))
                    {
                        authorName = cmntAuthor.UserName;
                        if (string.IsNullOrEmpty(authorName))
                        {
                            authorName = comment.DisplayName;
                        }
                    }

                    comment.DisplayName = authorName;
                }
            }
            if (model.IsAdmin)
            {
                model.ProgenyLocations = new List<Location>();
                model.ProgenyLocations = await _locationsHttpClient.GetProgenyLocations(model.ProgenyId, userAccessLevel);
                model.LocationsList = new List<SelectListItem>();
                if (model.ProgenyLocations.Any())
                {
                    foreach (Location loc in model.ProgenyLocations)
                    {
                        SelectListItem selectListItem = new SelectListItem();
                        selectListItem.Text = loc.Name;
                        selectListItem.Value = loc.LocationId.ToString();
                        model.LocationsList.Add(selectListItem);
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
            }

            return View(model);
        }

        public async Task<IActionResult> AddPicture()
        {
            UploadPictureViewModel model = new UploadPictureViewModel();
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
                            { Text = accessList.Single(p => p.Id == prog.Id).NickName, Value = prog.Id.ToString() };
                        if (prog.Id == model.CurrentUser.ViewChild)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }


                model.Owners = userEmail;
                model.Author = model.CurrentUser.UserId;
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
        public async Task<IActionResult> UploadPictures(UploadPictureViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            bool isAdmin = false;
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (progeny != null)
            {
                if (progeny.IsInAdminList(model.CurrentUser.UserEmail))
                {
                    isAdmin = true;
                }
            }
            if (!isAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Home",
                    action = "Index"
                });
            }

            List<Picture> pictureList = new List<Picture>();
            UploadPictureViewModel result = new UploadPictureViewModel();
            result.LanguageId = model.LanguageId;
            result.FileLinks = new List<string>();
            result.FileNames = new List<string>();
            if (model.Files.Any())
            {
                List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
                foreach (IFormFile formFile in model.Files)
                {
                    Picture picture = new Picture();
                    picture.ProgenyId = model.ProgenyId;
                    picture.AccessLevel = model.AccessLevel;
                    picture.Author = model.CurrentUser.UserId;
                    picture.Owners = model.Owners;
                    picture.TimeZone = model.CurrentUser.Timezone;
                    await using (Stream stream = formFile.OpenReadStream())
                    {
                        picture.PictureLink = await _imageStore.SaveImage(stream);
                    }

                    Picture newPicture = await _mediaHttpClient.AddPicture(picture);

                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = newPicture.ProgenyId;
                    tItem.AccessLevel = newPicture.AccessLevel;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Photo;
                    tItem.ItemId = newPicture.PictureId.ToString();
                    tItem.CreatedBy = model.CurrentUser.UserId;
                    tItem.CreatedTime = DateTime.UtcNow;
                    tItem.ProgenyTime = newPicture.PictureTime ?? DateTime.UtcNow;

                    await _timelineHttpClient.AddTimeLineItem(tItem);

                    string authorName = "";
                    if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
                    {
                        authorName = model.CurrentUser.FirstName;
                    }
                    if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
                    {
                        authorName = authorName + " " + model.CurrentUser.MiddleName;
                    }
                    if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
                    {
                        authorName = authorName + " " + model.CurrentUser.LastName;
                    }

                    authorName = authorName.Trim();
                    if (string.IsNullOrEmpty(authorName))
                    {
                        authorName = model.CurrentUser.UserName;
                    }

                    foreach (UserAccess ua in usersToNotif)
                    {
                        if (ua.AccessLevel <= newPicture.AccessLevel)
                        {
                            UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                            if (uaUserInfo.UserId != "Unknown")
                            {
                                string picTimeString;
                                if (newPicture.PictureTime.HasValue)
                                {
                                    DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(newPicture.PictureTime.Value,
                                        TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                                    picTimeString = "Photo taken: " + picTime.ToString("dd-MMM-yyyy HH:mm");
                                }
                                else
                                {
                                    picTimeString = "Photo taken: Unknown";
                                }
                                WebNotification notification = new WebNotification();
                                notification.To = uaUserInfo.UserId;
                                notification.From = authorName;
                                notification.Message = picTimeString + "\r\n";
                                notification.DateTime = DateTime.UtcNow;
                                notification.Icon = model.CurrentUser.ProfilePicture;
                                notification.Title = "A photo was added for " + progeny.NickName;
                                notification.Link = "/Pictures/Picture/" + newPicture.PictureId + "?childId=" + progeny.Id;
                                notification.Type = "Notification";

                                notification = await _webNotificationsService.SaveNotification(notification);

                                await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaphoto" + progeny.Id);
                            }
                        }
                    }

                    pictureList.Add(newPicture);
                }
            }

            if (pictureList.Any())
            {
                foreach (Picture pic in pictureList)
                {
                    result.FileLinks.Add(_imageStore.UriFor(pic.PictureLink600));
                    result.FileNames.Add(_imageStore.UriFor(pic.PictureLink600));
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

            return View(result);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPicture(PictureViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                string userEmail = User.GetEmail();
                model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
                Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
                if (progeny != null)
                {
                    if (progeny.IsInAdminList(model.CurrentUser.UserEmail))
                    {
                        model.IsAdmin = true;
                    }
                }
            }

            if (!model.IsAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Pictures",
                    action = "Picture",
                    id = model.PictureId,
                    childId = model.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            Picture newPicture = await _mediaHttpClient.GetPicture(model.PictureId, model.CurrentUser.Timezone);

            newPicture.AccessLevel = model.AccessLevel;
            newPicture.Author = model.Author;
            if (model.PictureTime != null)
            {
                newPicture.PictureTime = TimeZoneInfo.ConvertTimeToUtc(model.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            if (!string.IsNullOrEmpty(model.Tags))
            {
                newPicture.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            if (!string.IsNullOrEmpty(model.Location))
            {
                newPicture.Location = model.Location;
            }
            if (!string.IsNullOrEmpty(model.Longtitude))
            {
                newPicture.Longtitude = model.Longtitude.Replace(',', '.');
            }
            if (!string.IsNullOrEmpty(model.Latitude))
            {
                newPicture.Latitude = model.Latitude.Replace(',', '.');
            }
            if (!string.IsNullOrEmpty(model.Altitude))
            {
                newPicture.Altitude = model.Altitude.Replace(',', '.');
            }

            await _mediaHttpClient.UpdatePicture(newPicture);

            TimeLineItem tItem = await _timelineHttpClient.GetTimeLineItem(newPicture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo);
            if (tItem != null)
            {
                if (newPicture.PictureTime.HasValue)
                {
                    tItem.ProgenyTime = newPicture.PictureTime.Value;
                }
                else
                {
                    tItem.ProgenyTime = DateTime.UtcNow;
                }
                tItem.AccessLevel = newPicture.AccessLevel;
                await _timelineHttpClient.UpdateTimeLineItem(tItem);
            }

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.PictureId, childId = model.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeletePicture(int pictureId)
        {
            PictureViewModel model = new PictureViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Picture picture = await _mediaHttpClient.GetPicture(pictureId, model.CurrentUser.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(picture.ProgenyId);

            if (progeny != null)
            {
                if (progeny.IsInAdminList(model.CurrentUser.UserEmail))
                {
                    model.IsAdmin = true;
                }
            }
            if (!picture.PictureLink600.ToLower().StartsWith("http"))
            {
                picture.PictureLink600 = _imageStore.UriFor(picture.PictureLink600);
            }

            ViewBag.NotAuthorized = "You do not have sufficient access rights to modify this picture.";
            model.ProgenyId = picture.ProgenyId;
            model.PictureId = pictureId;
            model.PictureLink = picture.PictureLink600;
            model.PictureTime = picture.PictureTime;
            if (model.PictureTime.HasValue)
            {
                model.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(model.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePicture(PictureViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Picture picture = await _mediaHttpClient.GetPicture(model.PictureId, model.CurrentUser.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(picture.ProgenyId);
            bool pictureDeleted = false;
            if (progeny != null)
            {
                if (progeny.IsInAdminList(model.CurrentUser.UserEmail))
                {
                    pictureDeleted = await _mediaHttpClient.DeletePicture(model.PictureId);

                }
            }


            if (pictureDeleted)
            {
                TimeLineItem tItem = await _timelineHttpClient.GetTimeLineItem(picture.PictureId.ToString(),
                    (int)KinaUnaTypes.TimeLineType.Photo);
                if (tItem != null)
                {
                    await _timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                }

                string authorName = "";
                if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
                {
                    authorName = model.CurrentUser.FirstName;
                }
                if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
                {
                    authorName = authorName + " " + model.CurrentUser.MiddleName;
                }
                if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
                {
                    authorName = authorName + " " + model.CurrentUser.LastName;
                }

                authorName = authorName.Trim();
                if (string.IsNullOrEmpty(authorName))
                {
                    authorName = model.CurrentUser.UserName;
                }
                List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
                foreach (UserAccess ua in usersToNotif)
                {
                    if (ua.AccessLevel == 0)
                    {
                        UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                        if (uaUserInfo.UserId != "Unknown")
                        {
                            string picTimeString;
                            if (picture.PictureTime.HasValue)
                            {
                                DateTime picTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
                                    TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                                picTimeString = "Photo taken: " + picTime.ToString("dd-MMM-yyyy HH:mm");
                            }
                            else
                            {
                                picTimeString = "Photo taken: Unknown";
                            }
                            WebNotification notification = new WebNotification();
                            notification.To = uaUserInfo.UserId;
                            notification.From = Constants.AppName;
                            notification.Message = "Photo deleted by " + authorName + "\r\nPhoto ID: " + model.PictureId + "\r\n" + picTimeString + "\r\n";
                            notification.DateTime = DateTime.UtcNow;
                            notification.Icon = "/images/kinaunalogo48x48.png";
                            notification.Title = "Photo deleted for " + progeny.NickName;
                            notification.Link = "";
                            notification.Type = "Notification";

                            _ = await _webNotificationsService.SaveNotification(notification);
                        }
                    }
                }
            }
            // Todo: else, error, show info


            // Todo: show confirmation info, instead of gallery page.
            return RedirectToAction("Index", "Pictures");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPictureComment(CommentViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            Comment cmnt = new Comment();

            cmnt.CommentThreadNumber = model.CommentThreadNumber;
            cmnt.CommentText = model.CommentText;
            cmnt.Author = model.CurrentUser.UserId;
            cmnt.DisplayName = model.CurrentUser.UserName + "(" + model.CurrentUser.FirstName + " " + model.CurrentUser.MiddleName + " " + model.CurrentUser.LastName + ")";
            cmnt.Created = DateTime.UtcNow;
            cmnt.ItemType = (int)KinaUnaTypes.TimeLineType.Photo;
            cmnt.ItemId = model.ItemId.ToString();
            cmnt.Progeny = progeny;
            bool commentAdded = await _mediaHttpClient.AddPictureComment(cmnt);

            if (commentAdded)
            {

                Picture pic = await _mediaHttpClient.GetPicture(model.ItemId, model.CurrentUser.Timezone);
                if (progeny != null)
                {
                    string imgLink = Constants.WebAppUrl + "/Pictures/Picture/" + model.ItemId + "?childId=" + model.ProgenyId;
                    List<string> emails = progeny.Admins.Split(",").ToList();

                    foreach (string toMail in emails)
                    {
                        await _emailSender.SendEmailAsync(toMail, "New Comment on " + progeny.NickName + "'s Picture",
                           "A comment was added to " + progeny.NickName + "'s picture by " + cmnt.DisplayName + ":<br/><br/>" + cmnt.CommentText + "<br/><br/>Picture Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
                    }
                    string authorName = "";
                    if (!string.IsNullOrEmpty(model.CurrentUser.FirstName))
                    {
                        authorName = model.CurrentUser.FirstName;
                    }
                    if (!string.IsNullOrEmpty(model.CurrentUser.MiddleName))
                    {
                        authorName = authorName + " " + model.CurrentUser.MiddleName;
                    }
                    if (!string.IsNullOrEmpty(model.CurrentUser.LastName))
                    {
                        authorName = authorName + " " + model.CurrentUser.LastName;
                    }

                    authorName = authorName.Trim();
                    if (string.IsNullOrEmpty(authorName))
                    {
                        authorName = model.CurrentUser.UserName;
                        if (string.IsNullOrEmpty(authorName))
                        {
                            authorName = cmnt.DisplayName;
                        }
                    }
                    List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
                    foreach (UserAccess ua in usersToNotif)
                    {
                        if (ua.AccessLevel <= pic.AccessLevel)
                        {
                            string commentTxtStr = cmnt.CommentText;
                            if (cmnt.CommentText.Length > 99)
                            {
                                commentTxtStr = cmnt.CommentText.Substring(0, 100) + "...";
                            }
                            UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                            if (uaUserInfo.UserId != "Unknown")
                            {
                                WebNotification notification = new WebNotification();
                                notification.To = uaUserInfo.UserId;
                                notification.From = authorName;
                                notification.Message = commentTxtStr;
                                notification.DateTime = DateTime.UtcNow;
                                notification.Icon = model.CurrentUser.ProfilePicture;
                                notification.Title = "New comment on " + progeny.NickName + "'s photo";
                                notification.Link = "/Pictures/Picture/" + model.ItemId + "?childId=" + model.ProgenyId;
                                notification.Type = "Notification";

                                notification = await _webNotificationsService.SaveNotification(notification);

                                await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacomment" + model.ItemId);
                            }
                        }
                    }
                }
            }
            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.ItemId, childId = model.ProgenyId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePictureComment(int commentThreadNumber, int commentId, int pictureId, int progenyId)
        {
            await _mediaHttpClient.DeletePictureComment(commentId);

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = pictureId, childId = progenyId });
        }
    }
}