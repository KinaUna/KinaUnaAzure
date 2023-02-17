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

namespace KinaUnaWeb.Controllers
{
    public class VideosController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly ILocationsHttpClient _locationsHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly ITimelineHttpClient _timelineHttpClient;
        private readonly IEmailSender _emailSender;
        private readonly string _defaultUser = Constants.DefaultUserEmail;
        private readonly IWebNotificationsService _webNotificationsService;
        public VideosController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient,
            IUserAccessHttpClient userAccessHttpClient, ILocationsHttpClient locationsHttpClient, IPushMessageSender pushMessageSender,
            ITimelineHttpClient timelineHttpClient, IEmailSender emailSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _locationsHttpClient = locationsHttpClient;
            _pushMessageSender = pushMessageSender;
            _timelineHttpClient = timelineHttpClient;
            _emailSender = emailSender;
            _webNotificationsService = webNotificationsService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int id = 1, int pageSize = 8, int childId = 0, int sortBy = 1, string tagFilter = "")
        {
            VideoPageViewModel model = new VideoPageViewModel();

            if (id < 1)
            {
                id = 1;
            }

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

            bool userIsProgenyAdmin = false;
            if (progeny.IsInAdminList(userEmail))
            {
                userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            model = await _mediaHttpClient.GetVideoPage(pageSize, id, progeny.Id, userAccessLevel, sortBy, tagFilter, model.CurrentUser.Timezone);
            model.Progeny = progeny;
            model.IsAdmin = userIsProgenyAdmin;
            model.SortBy = sortBy;
            
            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Video(int id, int childId = 0, string tagFilter = "", int sortBy = 1)
        {
            VideoViewModel model = new VideoViewModel();
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

            VideoViewModel video = await _mediaHttpClient.GetVideoViewModel(id, userAccessLevel, sortBy, model.CurrentUser.Timezone);
            
            
            model.VideoId = video.VideoId;
            model.VideoType = video.VideoType;
            model.VideoTime = video.VideoTime;
            model.ProgenyId = video.ProgenyId;
            model.Progeny = progeny;
            model.Owners = video.Owners;
            model.VideoLink = video.VideoLink;
            model.ThumbLink = video.ThumbLink;
            model.AccessLevel = video.AccessLevel;
            model.Author = video.Author;
            model.AccessLevelListEn[video.AccessLevel].Selected = true;
            model.AccessLevelListDa[video.AccessLevel].Selected = true;
            model.AccessLevelListDe[video.AccessLevel].Selected = true;
            model.CommentThreadNumber = video.CommentThreadNumber;
            model.Tags = video.Tags;
            model.TagsList = video.TagsList;
            model.Location = video.Location;
            model.Latitude = video.Latitude;
            model.Longtitude = video.Longtitude;
            model.Altitude = video.Altitude;
            model.VideoNumber = video.VideoNumber;
            model.VideoCount = video.VideoCount;
            model.PrevVideo = video.PrevVideo;
            model.NextVideo = video.NextVideo;
            model.CommentsList = video.CommentsList;
            model.CommentsCount = video.CommentsList?.Count ?? 0;
            model.TagFilter = tagFilter;
            model.SortBy = sortBy;
            model.UserId = HttpContext.User.FindFirst("sub")?.Value ?? _defaultUser;
            if (video.Duration != null)
            {
                model.DurationHours = video.Duration.Value.Hours.ToString();
                model.DurationMinutes = video.Duration.Value.Minutes.ToString();
                model.DurationSeconds = video.Duration.Value.Seconds.ToString();
            }
            if (model.VideoTime != null && progeny.BirthDay.HasValue)
            {
                PictureTime picTime = new PictureTime(progeny.BirthDay.Value,
                    TimeZoneInfo.ConvertTimeToUtc(model.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone)),
                    TimeZoneInfo.FindSystemTimeZoneById(progeny.TimeZone));
                model.VidTimeValid = true;
                model.VidTime = model.VideoTime.Value.ToString("dd MMMM yyyy HH:mm"); // Todo: Replace string format with global constant or user defined value
                model.VidYears = picTime.CalcYears();
                model.VidMonths = picTime.CalcMonths();
                model.VidWeeks = picTime.CalcWeeks();
                model.VidDays = picTime.CalcDays();
                model.VidHours = picTime.CalcHours();
                model.VidMinutes = picTime.CalcMinutes();
            }
            else
            {
                model.VidTimeValid = false;
                model.VidTime = "";
            }
            if (model.CommentsCount > 0)
            {
                foreach (Comment cmnt in model.CommentsList)
                {
                    UserInfo cmntAuthor = await _userInfosHttpClient.GetUserInfoByUserId(cmnt.Author);
                    string authorImg = cmntAuthor?.ProfilePicture ?? "";
                    string authorName = "";
                    if (!string.IsNullOrEmpty(authorImg))
                    {
                        authorImg = _imageStore.UriFor(authorImg, "profiles");
                    }
                    else
                    {
                        authorImg = "/photodb/profile.jpg";
                    }
                    cmnt.AuthorImage = authorImg;

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
                            authorName = cmnt.DisplayName;
                        }
                    }

                    cmnt.DisplayName = authorName;
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

        [AllowAnonymous]
        public IActionResult Youtube(string link)
        {
            return PartialView("Youtube", link);
        }

        public async Task<IActionResult> AddVideo()
        {
            UploadVideoViewModel model = new UploadVideoViewModel();
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
                model.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
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
        public async Task<IActionResult> UploadVideo(UploadVideoViewModel model)
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

            Video video = new Video();
            video.ProgenyId = model.ProgenyId;
            video.AccessLevel = model.AccessLevel;
            video.Author = model.CurrentUser.UserId;
            video.Owners = model.Owners;
            video.ThumbLink = Constants.WebAppUrl + "/videodb/moviethumb.png";
            video.VideoTime = DateTime.UtcNow;
            if (model.VideoTime != null)
            {
                video.VideoTime = TimeZoneInfo.ConvertTimeToUtc(model.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            video.VideoType = 2; // Todo: Replace with Enum or constant
            int.TryParse(model.DurationHours, out int durHours);
            int.TryParse(model.DurationMinutes, out int durMins);
            int.TryParse(model.DurationSeconds, out int durSecs);
            if (durHours + durMins + durSecs != 0)
            {
                video.Duration = new TimeSpan(durHours, durMins, durSecs);
            }
            if (model.FileLink.Contains("<iframe"))
            {
                string[] vLink1 = model.FileLink.Split('"');
                foreach (string str in vLink1)
                {
                    if (str.Contains("://"))
                    {
                        video.VideoLink = str;
                    }
                }
            }

            if (model.FileLink.Contains("watch?v"))
            {
                string str = model.FileLink.Split('=').Last();
                video.VideoLink = "https://www.youtube.com/embed/" + str;
            }

            if (model.FileLink.StartsWith("https://youtu.be"))
            {
                string str = model.FileLink.Split('/').Last();
                video.VideoLink = "https://www.youtube.com/embed/" + str;
            }

            video.ThumbLink = "https://i.ytimg.com/vi/" + video.VideoLink.Split("/").Last() + "/hqdefault.jpg";

            Video newVideo = await _mediaHttpClient.AddVideo(video);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = newVideo.ProgenyId;
            tItem.AccessLevel = newVideo.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Video;
            tItem.ItemId = newVideo.VideoId.ToString();
            tItem.CreatedBy = model.CurrentUser.UserId;
            tItem.CreatedTime = DateTime.UtcNow;
            if (newVideo.VideoTime.HasValue)
            {
                tItem.ProgenyTime = newVideo.VideoTime.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            await _timelineHttpClient.AddTimeLineItem(tItem);

            List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
            foreach (UserAccess userAccess in usersToNotif)
            {
                if (userAccess.AccessLevel <= newVideo.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(userAccess.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string vidTimeString;
                        if (newVideo.VideoTime.HasValue)
                        {
                            DateTime vidTime = TimeZoneInfo.ConvertTimeFromUtc(newVideo.VideoTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            vidTimeString = "Video recorded: " + vidTime.ToString("dd-MMM-yyyy HH:mm");
                        }
                        else
                        {
                            vidTimeString = "Video recorded: Unknown";
                        }
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = model.CurrentUser.FullName();
                        notification.Message = vidTimeString + "\r\n";
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A video was added for " + progeny.NickName;
                        notification.Link = "/Videos/Video/" + newVideo.VideoId + "?childId=" + model.ProgenyId;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavideo" + progeny.Id);
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVideo(VideoViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            model.CurrentUser = new UserInfo();
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
                    controller = "Videos",
                    action = "Video",
                    id = model.VideoId,
                    childId = model.ProgenyId,
                    sortBy = model.SortBy
                });
            }

            Video newVideo = await _mediaHttpClient.GetVideo(model.VideoId, model.CurrentUser.Timezone);

            newVideo.AccessLevel = model.AccessLevel;
            newVideo.Author = model.Author;
            if (model.VideoTime != null)
            {
                newVideo.VideoTime = TimeZoneInfo.ConvertTimeToUtc(model.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }

            int.TryParse(model.DurationHours, out int durHours);
            int.TryParse(model.DurationMinutes, out int durMins);
            int.TryParse(model.DurationSeconds, out int durSecs);
            if (durHours + durMins + durSecs != 0)
            {
                newVideo.Duration = new TimeSpan(durHours, durMins, durSecs);
            }

            if (!string.IsNullOrEmpty(model.Tags))
            {
                newVideo.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (!string.IsNullOrEmpty(model.Location))
            {
                newVideo.Location = model.Location;
            }
            if (!string.IsNullOrEmpty(model.Longtitude))
            {
                newVideo.Longtitude = model.Longtitude;
            }
            if (!string.IsNullOrEmpty(model.Latitude))
            {
                newVideo.Latitude = model.Latitude;
            }
            if (!string.IsNullOrEmpty(model.Altitude))
            {
                newVideo.Altitude = model.Altitude;
            }

            await _mediaHttpClient.UpdateVideo(newVideo);

            TimeLineItem tItem = await _timelineHttpClient.GetTimeLineItem(newVideo.VideoId.ToString(),
                (int)KinaUnaTypes.TimeLineType.Video);
            if (tItem != null)
            {
                if (newVideo.VideoTime.HasValue)
                {
                    tItem.ProgenyTime = newVideo.VideoTime.Value;
                }
                else
                {
                    tItem.ProgenyTime = DateTime.UtcNow;
                }
                tItem.AccessLevel = newVideo.AccessLevel;
                await _timelineHttpClient.UpdateTimeLineItem(tItem);
            }

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.VideoId, childId = model.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            VideoViewModel model = new VideoViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Video video = await _mediaHttpClient.GetVideo(videoId, model.CurrentUser.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(video.ProgenyId);

            if (progeny != null)
            {
                if (progeny.IsInAdminList(model.CurrentUser.UserEmail))
                {
                    model.IsAdmin = true;
                }
            }

            ViewBag.NotAuthorized = "You do not have sufficient access rights to modify this picture.";
            model.ProgenyId = video.ProgenyId;
            model.VideoId = videoId;
            model.ThumbLink = video.ThumbLink;
            model.VideoTime = video.VideoTime;
            if (video.VideoTime.HasValue)
            {
                video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(VideoViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Video video = await _mediaHttpClient.GetVideo(model.VideoId, model.CurrentUser.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(video.ProgenyId);
            bool videoDeleted = false;
            if (progeny != null)
            {
                if (progeny.IsInAdminList(model.CurrentUser.UserEmail))
                {
                    videoDeleted = await _mediaHttpClient.DeleteVideo(model.VideoId);

                }
            }

            if (videoDeleted)
            {
                TimeLineItem tItem = await _timelineHttpClient.GetTimeLineItem(video.VideoId.ToString(),
                    (int)KinaUnaTypes.TimeLineType.Video);
                if (tItem != null)
                {
                    await _timelineHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                }
            }
            // Todo: else, error, show info

            // Todo: show confirmation info, instead of gallery page.
            return RedirectToAction("Index", "Videos");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVideoComment(CommentViewModel model)
        {
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            Comment cmnt = new Comment();

            cmnt.CommentThreadNumber = model.CommentThreadNumber;
            cmnt.CommentText = model.CommentText;
            cmnt.Author = model.CurrentUser.UserId;
            cmnt.DisplayName = model.CurrentUser.UserName + "(" + model.CurrentUser.FirstName + " " + model.CurrentUser.MiddleName + " " + model.CurrentUser.LastName + ")";
            cmnt.Created = DateTime.UtcNow;
            cmnt.ItemType = (int)KinaUnaTypes.TimeLineType.Video;
            cmnt.ItemId = model.ItemId.ToString();
            cmnt.Progeny = progeny;
            bool commentAdded = await _mediaHttpClient.AddVideoComment(cmnt);

            if (commentAdded)
            {
                if (progeny != null)
                {
                    string imgLink = Constants.WebAppUrl + "/Videos/Video/" + model.ItemId + "?childId=" + model.ProgenyId;
                    List<string> emails = progeny.Admins.Split(",").ToList();
                    foreach (string toMail in emails)
                    {
                        await _emailSender.SendEmailAsync(toMail, "New Comment on " + progeny.NickName + "'s Picture",
                           "A comment was added to " + progeny.NickName + "'s picture by " + cmnt.DisplayName + ":<br/><br/>" + cmnt.CommentText + "<br/><br/>Picture Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
                    }
                    List<UserAccess> usersToNotif = await _userAccessHttpClient.GetProgenyAccessList(model.ProgenyId);
                    Video vid = await _mediaHttpClient.GetVideo(model.ItemId, model.CurrentUser.Timezone);
                    
                    foreach (UserAccess ua in usersToNotif)
                    {
                        if (ua.AccessLevel <= vid.AccessLevel)
                        {
                            UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                            if (uaUserInfo.UserId != "Unknown")
                            {
                                string commentTxtStr = cmnt.CommentText;
                                if (cmnt.CommentText.Length > 99)
                                {
                                    commentTxtStr = cmnt.CommentText.Substring(0, 100) + "...";
                                }
                                WebNotification notification = new WebNotification();
                                notification.To = uaUserInfo.UserId;
                                notification.From = model.CurrentUser.FullName();
                                notification.Message = commentTxtStr;
                                notification.DateTime = DateTime.UtcNow;
                                notification.Icon = model.CurrentUser.ProfilePicture;
                                notification.Title = "New comment on " + progeny.NickName + "'s video";
                                notification.Link = "/Videos/Video/" + vid.VideoId + "?childId=" + model.ProgenyId;
                                notification.Type = "Notification";

                                notification = await _webNotificationsService.SaveNotification(notification);

                                await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                                    notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacomment" + vid.VideoId);
                            }
                        }
                    }
                }
            }

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.ItemId, childId = model.ProgenyId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideoComment(int commentThreadNumber, int commentId, int videoId, int progenyId)
        {
            await _mediaHttpClient.DeleteVideoComment(commentId);

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = videoId, childId = progenyId });
        }
    }
}