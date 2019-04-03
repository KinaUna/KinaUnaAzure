using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using KinaUna.Data;

namespace KinaUnaWeb.Controllers
{
    [Authorize]
    public class AddItemController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IMediaHttpClient _mediaHttpClient;
        private int _progId;
        private readonly ImageStore _imageStore;
        private readonly WebDbContext _context;
        private readonly string _defaultUser = Constants.DefaultUserEmail;
        private readonly IEmailSender _emailSender;
        private readonly IPushMessageSender _pushMessageSender;

        public AddItemController(IProgenyHttpClient progenyHttpClient, IMediaHttpClient mediaHttpClient, ImageStore imageStore, WebDbContext context, IEmailSender emailSender, IPushMessageSender pushMessageSender)
        {
            _progenyHttpClient = progenyHttpClient;
            _mediaHttpClient = mediaHttpClient;
            _imageStore = imageStore;
            _context = context; // Todo: replace _context with httpClients
            _emailSender = emailSender;
            _pushMessageSender = pushMessageSender;
        }
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AddPicture()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;

            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            UploadPictureViewModel model = new UploadPictureViewModel();
            model.Userinfo = userinfo;
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
            {
                var accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                            {Text = accessList.Single(p => p.Id == prog.Id).NickName, Value = prog.Id.ToString()};
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }
                        model.ProgenyList.Add(selItem);
                    }
                }
                

                model.Owners = userEmail;
                model.Author = userinfo.UserId;
            }
            

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPictures(UploadPictureViewModel model)
        {
            bool isAdmin = false;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            var userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (progeny != null)
            {
                if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
            result.FileLinks = new List<string>();
            result.FileNames = new List<string>();
            if (model.Files.Any())
            {
                List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
                foreach (IFormFile formFile in model.Files)
                {
                    Picture picture = new Picture();
                    picture.ProgenyId = model.ProgenyId;
                    picture.AccessLevel = model.AccessLevel;
                    picture.Author = userinfo.UserId;
                    picture.Owners = model.Owners;
                    picture.TimeZone = userinfo.Timezone;
                    using (var stream = formFile.OpenReadStream())
                    {
                        picture.PictureLink = await _imageStore.SaveImage(stream);
                    }

                    Picture newPicture = await _mediaHttpClient.AddPicture(picture);

                    TimeLineItem tItem = new TimeLineItem();
                    tItem.ProgenyId = newPicture.ProgenyId;
                    tItem.AccessLevel = newPicture.AccessLevel;
                    tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Photo;
                    tItem.ItemId = newPicture.PictureId.ToString();
                    tItem.CreatedBy = userinfo.UserId;
                    tItem.CreatedTime = DateTime.UtcNow;
                    if (newPicture.PictureTime.HasValue)
                    {
                        tItem.ProgenyTime = newPicture.PictureTime.Value;
                    }
                    else
                    {
                        tItem.ProgenyTime = DateTime.UtcNow;
                    }

                    await _progenyHttpClient.AddTimeLineItem(tItem);
                    //await _context.TimeLineDb.AddAsync(tItem);
                    //await _context.SaveChangesAsync();

                    string authorName = "";
                    if (!String.IsNullOrEmpty(userinfo.FirstName))
                    {
                        authorName = userinfo.FirstName;
                    }
                    if (!String.IsNullOrEmpty(userinfo.MiddleName))
                    {
                        authorName = authorName + " " + userinfo.MiddleName;
                    }
                    if (!String.IsNullOrEmpty(userinfo.LastName))
                    {
                        authorName = authorName + " " + userinfo.LastName;
                    }

                    authorName = authorName.Trim();
                    if (String.IsNullOrEmpty(authorName))
                    {
                        authorName = userinfo.UserName;
                    }

                    foreach (UserAccess ua in usersToNotif)
                    {
                        if (ua.AccessLevel <= newPicture.AccessLevel)
                        {
                            UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                            if (uaUserInfo.UserId != "Unknown")
                            {
                                string picTimeString;
                                if (newPicture.PictureTime.HasValue)
                                {
                                    var picTime = TimeZoneInfo.ConvertTimeFromUtc(newPicture.PictureTime.Value,
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
                                notification.Icon = userinfo.ProfilePicture;
                                notification.Title = "A photo was added for " + progeny.NickName;
                                notification.Link = "/Pictures/Picture/" + newPicture.PictureId + "?childId=" + progeny.Id;
                                notification.Type = "Notification";
                                await _context.WebNotificationsDb.AddAsync(notification);
                                await _context.SaveChangesAsync();

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
            return View(result);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPicture(PictureViewModel model)
        {
            UserInfo userinfo = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
                userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
                Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
                if (progeny != null)
                {
                    if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
                    {
                        model.IsAdmin = true;
                    }
                }
            }

            if (!model.IsAdmin)
            {
                return RedirectToRoute(new
                {
                    controller = "Pictures", action = "Picture", id = model.PictureId, childId = model.ProgenyId, sortBy = model.SortBy
                });
            }

            Picture newPicture = await _mediaHttpClient.GetPicture(model.PictureId, userinfo.Timezone);

            newPicture.AccessLevel = model.AccessLevel;
            newPicture.Author = model.Author;
            if (model.PictureTime != null)
            {
                newPicture.PictureTime = TimeZoneInfo.ConvertTimeToUtc(model.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }

            if (!String.IsNullOrEmpty(model.Tags))
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

            TimeLineItem tItem = await _progenyHttpClient.GetTimeLineItem(newPicture.PictureId.ToString(), (int)KinaUnaTypes.TimeLineType.Photo); // _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == newPicture.PictureId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Photo);
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
                await _progenyHttpClient.UpdateTimeLineItem(tItem);
                //_context.TimeLineDb.Update(tItem);
                //await _context.SaveChangesAsync();
            }

            return RedirectToRoute(new { controller = "Pictures", action = "Picture", id = model.PictureId, childId = model.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeletePicture(int pictureId)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Picture picture = await _mediaHttpClient.GetPicture(pictureId, userinfo.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(picture.ProgenyId);
            PictureViewModel model = new PictureViewModel();
            if (progeny != null)
            {
                if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
                model.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(model.PictureTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePicture(PictureViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Picture picture = await _mediaHttpClient.GetPicture(model.PictureId, userinfo.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(picture.ProgenyId);
            bool pictureDeleted = false;
            if (progeny != null)
            {
                if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
                {
                    pictureDeleted = await _mediaHttpClient.DeletePicture(model.PictureId);

                }
            }

            
            if (pictureDeleted)
            {
                TimeLineItem tItem = await _progenyHttpClient.GetTimeLineItem(picture.PictureId.ToString(),
                    (int) KinaUnaTypes.TimeLineType.Photo); // _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == picture.PictureId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Photo);
                if (tItem != null)
                {
                    await _progenyHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    //_context.TimeLineDb.Remove(tItem);
                    //await _context.SaveChangesAsync();
                }

                string authorName = "";
                if (!String.IsNullOrEmpty(userinfo.FirstName))
                {
                    authorName = userinfo.FirstName;
                }
                if (!String.IsNullOrEmpty(userinfo.MiddleName))
                {
                    authorName = authorName + " " + userinfo.MiddleName;
                }
                if (!String.IsNullOrEmpty(userinfo.LastName))
                {
                    authorName = authorName + " " + userinfo.LastName;
                }

                authorName = authorName.Trim();
                if (String.IsNullOrEmpty(authorName))
                {
                    authorName = userinfo.UserName;
                }
                List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
                foreach (UserAccess ua in usersToNotif)
                {
                    if (ua.AccessLevel == 0)
                    {
                        UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                        if (uaUserInfo.UserId != "Unknown")
                        {
                            string picTimeString;
                            if (picture.PictureTime.HasValue)
                            {
                                var picTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
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
                            await _context.WebNotificationsDb.AddAsync(notification);
                            await _context.SaveChangesAsync();
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
            string userEmail = HttpContext.User.FindFirst("email")?.Value;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            
            Comment cmnt = new Comment();

            cmnt.CommentThreadNumber = model.CommentThreadNumber;
            cmnt.CommentText = model.CommentText;
            cmnt.Author = userinfo.UserId;
            cmnt.DisplayName = userinfo.UserName + "(" + userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + ")";
            cmnt.Created = DateTime.UtcNow;

            bool commentAdded = await _mediaHttpClient.AddPictureComment(cmnt);

            if (commentAdded)
            {
                Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
                Picture pic = await _mediaHttpClient.GetPicture(model.ItemId, userinfo.Timezone);
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
                    if (!String.IsNullOrEmpty(userinfo.FirstName))
                    {
                        authorName = userinfo.FirstName;
                    }
                    if (!String.IsNullOrEmpty(userinfo.MiddleName))
                    {
                        authorName = authorName + " " + userinfo.MiddleName;
                    }
                    if (!String.IsNullOrEmpty(userinfo.LastName))
                    {
                        authorName = authorName + " " + userinfo.LastName;
                    }

                    authorName = authorName.Trim();
                    if (String.IsNullOrEmpty(authorName))
                    {
                        authorName = userinfo.UserName;
                        if (String.IsNullOrEmpty(authorName))
                        {
                            authorName = cmnt.DisplayName;
                        }
                    }
                    List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
                    foreach (UserAccess ua in usersToNotif)
                    {
                        if (ua.AccessLevel <= pic.AccessLevel)
                        {
                            string commentTxtStr = cmnt.CommentText;
                            if (cmnt.CommentText.Length > 99)
                            {
                                commentTxtStr = cmnt.CommentText.Substring(0, 100) + "...";
                            }
                            UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                            if (uaUserInfo.UserId != "Unknown")
                            {
                                WebNotification notification = new WebNotification();
                                notification.To = uaUserInfo.UserId;
                                notification.From = authorName;
                                notification.Message = commentTxtStr;
                                notification.DateTime = DateTime.UtcNow;
                                notification.Icon = userinfo.ProfilePicture;
                                notification.Title = "New comment on " + progeny.NickName + "'s photo";
                                notification.Link = "/Pictures/Picture/" + model.ItemId + "?childId=" + model.ProgenyId;
                                notification.Type = "Notification";
                                await _context.WebNotificationsDb.AddAsync(notification);
                                await _context.SaveChangesAsync();

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



        // Videos
        public async Task<IActionResult> AddVideo()
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;

            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }

            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            UploadVideoViewModel model = new UploadVideoViewModel();
            model.Userinfo = userinfo;
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        { Text = accessList.Single(p => p.Id == prog.Id).NickName, Value = prog.Id.ToString() };
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }
                        model.ProgenyList.Add(selItem);
                    }
                }
                model.Owners = userEmail;
                model.Author = userinfo.UserId;
                model.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVideo(UploadVideoViewModel model)
        {
            bool isAdmin = false;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (progeny != null)
            {
                if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
            video.Author = userinfo.UserId;
            video.Owners = model.Owners;
            video.ThumbLink = Constants.WebAppUrl + "/videodb/moviethumb.png";
            video.VideoTime = DateTime.UtcNow;
            if (model.VideoTime != null)
            {
                video.VideoTime = TimeZoneInfo.ConvertTimeToUtc(model.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            video.VideoType = 2; // Todo: Replace with Enum or constant
            Int32.TryParse(model.DurationHours, out var durHours);
            Int32.TryParse(model.DurationMinutes, out var durMins);
            Int32.TryParse(model.DurationSeconds, out var durSecs);
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
            tItem.CreatedBy = userinfo.UserId;
            tItem.CreatedTime = DateTime.UtcNow;
            if (newVideo.VideoTime.HasValue)
            {
                tItem.ProgenyTime = newVideo.VideoTime.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            await _progenyHttpClient.AddTimeLineItem(tItem);
            //await _context.TimeLineDb.AddAsync(tItem);
            //await _context.SaveChangesAsync();

            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= newVideo.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string vidTimeString;
                        if (newVideo.VideoTime.HasValue)
                        {
                            var vidTime = TimeZoneInfo.ConvertTimeFromUtc(newVideo.VideoTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            vidTimeString = "Video recorded: " + vidTime.ToString("dd-MMM-yyyy HH:mm");
                        }
                        else
                        {
                            vidTimeString = "Video recorded: Unknown";
                        }
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = vidTimeString + "\r\n";
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A video was added for " + progeny.NickName;
                        notification.Link = "/Videos/Video/" + newVideo.VideoId + "?childId=" + model.ProgenyId;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavideo" + progeny.Id);
                    }
                }
            }

            return View(newVideo);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVideo(VideoViewModel model)
        {
            UserInfo userinfo = new UserInfo();
            if (User.Identity.IsAuthenticated)
            {
                string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
                userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
                Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
                if (progeny != null)
                {
                    if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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

            Video newVideo = await _mediaHttpClient.GetVideo(model.VideoId, userinfo.Timezone);

            newVideo.AccessLevel = model.AccessLevel;
            newVideo.Author = model.Author;
            if (model.VideoTime != null)
            {
                newVideo.VideoTime = TimeZoneInfo.ConvertTimeToUtc(model.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }

            Int32.TryParse(model.DurationHours, out var durHours);
            Int32.TryParse(model.DurationMinutes, out var durMins);
            Int32.TryParse(model.DurationSeconds, out var durSecs);
            if (durHours + durMins + durSecs != 0)
            {
                newVideo.Duration = new TimeSpan(durHours, durMins, durSecs);
            }

            if (!String.IsNullOrEmpty(model.Tags))
            {
                newVideo.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (!String.IsNullOrEmpty(model.Location))
            {
                newVideo.Location = model.Location;
            }
            if (!String.IsNullOrEmpty(model.Longtitude))
            {
                newVideo.Longtitude = model.Longtitude;
            }
            if (!String.IsNullOrEmpty(model.Latitude))
            {
                newVideo.Latitude = model.Latitude;
            }
            if (!String.IsNullOrEmpty(model.Altitude))
            {
                newVideo.Altitude = model.Altitude;
            }
            
            await _mediaHttpClient.UpdateVideo(newVideo);

            TimeLineItem tItem = await _progenyHttpClient.GetTimeLineItem(newVideo.VideoId.ToString(),
                (int) KinaUnaTypes.TimeLineType.Video); // _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId ==newVideo.VideoId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Video);
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
                await _progenyHttpClient.UpdateTimeLineItem(tItem);
                //_context.TimeLineDb.Update(tItem);
                //await _context.SaveChangesAsync();
            }

            return RedirectToRoute(new { controller = "Videos", action = "Video", id = model.VideoId, childId = model.ProgenyId, tagFilter = model.TagFilter, sortBy = model.SortBy });
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DeleteVideo(int videoId)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Video video = await _mediaHttpClient.GetVideo(videoId, userinfo.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(video.ProgenyId);
            VideoViewModel model = new VideoViewModel();
            if (progeny != null)
            {
                if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
                video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(video.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVideo(VideoViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Video video = await _mediaHttpClient.GetVideo(model.VideoId, userinfo.Timezone);
            Progeny progeny = await _progenyHttpClient.GetProgeny(video.ProgenyId);
            bool videoDeleted = false;
            if (progeny != null)
            {
                if (progeny.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
                {
                    videoDeleted = await _mediaHttpClient.DeleteVideo(model.VideoId);

                }
            }

            if (videoDeleted)
            {
                TimeLineItem tItem = await _progenyHttpClient.GetTimeLineItem(video.VideoId.ToString(),
                    (int) KinaUnaTypes.TimeLineType.Video); // _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == video.VideoId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Video);
                if (tItem != null)
                {
                    await _progenyHttpClient.DeleteTimeLineItem(tItem.TimeLineId);
                    //_context.TimeLineDb.Remove(tItem);
                    //await _context.SaveChangesAsync();
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
            string userEmail = HttpContext.User.FindFirst("email")?.Value;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Comment cmnt = new Comment();

            cmnt.CommentThreadNumber = model.CommentThreadNumber;
            cmnt.CommentText = model.CommentText;
            cmnt.Author = userinfo.UserId;
            cmnt.DisplayName = userinfo.UserName + "(" + userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + ")";
            cmnt.Created = DateTime.UtcNow;

            bool commentAdded = await _mediaHttpClient.AddVideoComment(cmnt);

            if (commentAdded)
            {
                Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
                if (progeny != null)
                {
                    string imgLink = Constants.WebAppUrl + "/Videos/Video/" + model.ItemId + "?childId=" + model.ProgenyId;
                    List<string> emails = progeny.Admins.Split(",").ToList();
                    foreach (string toMail in emails)
                    {
                         await _emailSender.SendEmailAsync(toMail, "New Comment on " + progeny.NickName + "'s Picture",
                            "A comment was added to " + progeny.NickName + "'s picture by " + cmnt.DisplayName + ":<br/><br/>" + cmnt.CommentText + "<br/><br/>Picture Link: <a href=\"" + imgLink + "\">" + imgLink + "</a>");
                    }
                    List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
                    Video vid = await _mediaHttpClient.GetVideo(model.ItemId, userinfo.Timezone);
                    string authorName = "";
                    if (!String.IsNullOrEmpty(userinfo.FirstName))
                    {
                        authorName = userinfo.FirstName;
                    }
                    if (!String.IsNullOrEmpty(userinfo.MiddleName))
                    {
                        authorName = authorName + " " + userinfo.MiddleName;
                    }
                    if (!String.IsNullOrEmpty(userinfo.LastName))
                    {
                        authorName = authorName + " " + userinfo.LastName;
                    }

                    authorName = authorName.Trim();
                    if (String.IsNullOrEmpty(authorName))
                    {
                        authorName = userinfo.UserName;
                        if (String.IsNullOrEmpty(authorName))
                        {
                            authorName = cmnt.DisplayName;
                        }
                    }
                    foreach (UserAccess ua in usersToNotif)
                    {
                        if (ua.AccessLevel <= vid.AccessLevel)
                        {
                            UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                            if (uaUserInfo.UserId != "Unknown")
                            {
                                string commentTxtStr = cmnt.CommentText;
                                if (cmnt.CommentText.Length > 99)
                                {
                                    commentTxtStr = cmnt.CommentText.Substring(0, 100) + "...";
                                }
                                WebNotification notification = new WebNotification();
                                notification.To = uaUserInfo.UserId;
                                notification.From = authorName;
                                notification.Message = commentTxtStr;
                                notification.DateTime = DateTime.UtcNow;
                                notification.Icon = userinfo.ProfilePicture;
                                notification.Title = "New comment on " + progeny.NickName + "'s video";
                                notification.Link = "/Videos/Video/" + vid.VideoId + "?childId=" + model.ProgenyId;
                                notification.Type = "Notification";
                                await _context.WebNotificationsDb.AddAsync(notification);
                                await _context.SaveChangesAsync();

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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddNote()
        {
            NoteViewModel model = new NoteViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName, Value = prog.Id.ToString()
                        };
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            model.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            model.PathName = userinfo.UserId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNote(NoteViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            var progAdminList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
            if (!progAdminList.Any())
            {
                // Todo: Show that no children are available to add note for.
                return RedirectToAction("Index");
            }
            
            Note noteItem = new Note();
            noteItem.Title = model.Title;
            noteItem.ProgenyId = model.ProgenyId;
            noteItem.CreatedDate = TimeZoneInfo.ConvertTimeToUtc(model.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            noteItem.Content = model.Content;
            noteItem.Category = model.Category;
            noteItem.AccessLevel = model.AccessLevel;
            noteItem.Owner = userinfo.UserId;

            await _progenyHttpClient.AddNote(noteItem);
            //await _context.NotesDb.AddAsync(noteItem);
            //await _context.SaveChangesAsync();
            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= noteItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Title: " + noteItem.Title + "\r\nCategory: " + noteItem.Category;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new note was added for " + progeny.NickName;
                        notification.Link = "/Notes?childId=" + model.ProgenyId;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunanote" + progeny.Id);
                    }
                }
            }

            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> EditNote(int itemId)
        {
            NoteViewModel model = new NoteViewModel();
            Note note = await _progenyHttpClient.GetNote(itemId); // _context.NotesDb.SingleAsync(n => n.NoteId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.NoteId = note.NoteId;
            model.ProgenyId = note.ProgenyId;
            model.AccessLevel = note.AccessLevel;
            model.Category = note.Category;
            model.Title = note.Title;
            model.CreatedDate = TimeZoneInfo.ConvertTimeFromUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            model.Content = note.Content;
            model.Owner = note.Owner;
            if (model.Owner.Contains("@"))
            {
                model.Owner = userinfo.UserId;
            }
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            model.PathName = userinfo.UserId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNote(NoteViewModel note)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                Note model = new Note();
                model.NoteId = note.NoteId;
                model.ProgenyId = note.ProgenyId;
                model.AccessLevel = note.AccessLevel;
                model.Category = note.Category;
                model.Title = note.Title;
                model.CreatedDate = TimeZoneInfo.ConvertTimeToUtc(note.CreatedDate, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                model.Content = note.Content;
                model.Owner = note.Owner;

                await _progenyHttpClient.UpdateNote(model);
                //_context.NotesDb.Update(model);
                //await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteNote(int itemId)
        {
            Note model = await _progenyHttpClient.GetNote(itemId); // _context.NotesDb.SingleAsync(n => n.NoteId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNote(Note model)
        {
            Note note = await _progenyHttpClient.GetNote(model.NoteId); // _context.NotesDb.SingleAsync(n => n.NoteId == model.NoteId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(note.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteNote(model.NoteId);
            //_context.NotesDb.Remove(note);
            //await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Notes");
        }

        [HttpGet]
        public async Task<IActionResult> AddEvent()
        {
            CalendarItemViewModel model = new CalendarItemViewModel();
            model.AllDay = false;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEvent(CalendarItemViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            CalendarItem eventItem = new CalendarItem();
            eventItem.ProgenyId = model.ProgenyId;
            eventItem.Title = model.Title;
            eventItem.Notes = model.Notes;
            if (model.StartTime != null && model.EndTime != null)
            {
                eventItem.StartTime = TimeZoneInfo.ConvertTimeToUtc(model.StartTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                eventItem.EndTime = TimeZoneInfo.ConvertTimeToUtc(model.EndTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            else
            {
                return View();
            }
            eventItem.Location = model.Location;
            eventItem.Context = model.Context;
            eventItem.AllDay = model.AllDay;
            eventItem.AccessLevel = model.AccessLevel;
            eventItem.Author = userinfo.UserId;
            eventItem = await _progenyHttpClient.AddCalendarItem(eventItem);
            //await _context.CalendarDb.AddAsync(eventItem);
            //await _context.SaveChangesAsync();

            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= eventItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        var startTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        string eventTimeString = "\r\nStart: " + startTime.ToString("dd-MMM-yyyy HH:mm");
                        
                        var endTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        eventTimeString = eventTimeString + "\r\nEnd: " + endTime.ToString("dd-MMM-yyyy HH:mm");
                        
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = eventItem.Title + eventTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new calendar event was added for " + progeny.NickName;
                        notification.Link = "/Calendar/ViewEvent?eventId=" + eventItem.EventId + "&childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacalendar" + progeny.Id);
                    }
                }
            }

            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int itemId)
        {

            CalendarItemViewModel model = new CalendarItemViewModel();
            CalendarItem eventItem = await _progenyHttpClient.GetCalendarItem(itemId); // _context.CalendarDb.SingleAsync(e => e.EventId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = eventItem.ProgenyId;
            model.Progeny = prog;
            model.EventId = eventItem.EventId;
            model.AccessLevel = eventItem.AccessLevel;
            model.Author = eventItem.Author;
            model.Title = eventItem.Title;
            model.Notes = eventItem.Notes;
            if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
            {
                model.StartTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                model.EndTime = TimeZoneInfo.ConvertTimeFromUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            model.Location = eventItem.Location;
            model.Context = eventItem.Context;
            model.AllDay = eventItem.AllDay;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(CalendarItemViewModel eventItem)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                CalendarItem model = new CalendarItem();
                model.ProgenyId = eventItem.ProgenyId;
                model.EventId = eventItem.EventId;
                model.AccessLevel = eventItem.AccessLevel;
                model.Author = eventItem.Author;
                model.Title = eventItem.Title;
                model.Notes = eventItem.Notes;
                if (eventItem.StartTime.HasValue && eventItem.EndTime.HasValue)
                {
                    model.StartTime = TimeZoneInfo.ConvertTimeToUtc(eventItem.StartTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                    model.EndTime = TimeZoneInfo.ConvertTimeToUtc(eventItem.EndTime.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
                model.Location = eventItem.Location;
                model.Context = eventItem.Context;
                model.AllDay = eventItem.AllDay;
                await _progenyHttpClient.UpdateCalendarItem(model);
                //_context.CalendarDb.Update(model);
                //await _context.SaveChangesAsync();



            }
            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteEvent(int itemId)
        {
            CalendarItem model = await _progenyHttpClient.GetCalendarItem(itemId); // _context.CalendarDb.SingleAsync(e => e.EventId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(CalendarItem model)
        {
            CalendarItem eventItem = await _progenyHttpClient.GetCalendarItem(model.EventId); // _context.CalendarDb.SingleAsync(e => e.EventId == model.EventId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(eventItem.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteCalendarItem(model.EventId);
            //_context.CalendarDb.Remove(eventItem);
            //await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Calendar");
        }

        [HttpGet]
        public async Task<IActionResult> AddVocabulary()
        {
            VocabularyItemViewModel model = new VocabularyItemViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            model.Progeny = accessList[0];
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVocabulary(VocabularyItemViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            vocabItem.Date = model.Date;
            vocabItem.Description = model.Description;
            vocabItem.Language = model.Language;
            vocabItem.SoundsLike = model.SoundsLike;
            vocabItem.AccessLevel = model.AccessLevel;
            vocabItem.Author = userinfo.UserId;

            await _progenyHttpClient.AddWord(vocabItem);
            //await _context.VocabularyDb.AddAsync(vocabItem);
            //await _context.SaveChangesAsync();

            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= vocabItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string vocabTimeString = "";
                        if (vocabItem.Date.HasValue)
                        {
                            var startTime = TimeZoneInfo.ConvertTimeFromUtc(vocabItem.Date.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                            vocabTimeString = "\r\nDate: " + startTime.ToString("dd-MMM-yyyy");
                        }
                        
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Word: " + vocabItem.Word + "\r\nLanguage: " + vocabItem.Language + vocabTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new word was added for " + progeny.NickName;
                        notification.Link = "/Vocabulary?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

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
            VocabularyItem vocab = await _progenyHttpClient.GetWord(itemId); // _context.VocabularyDb.SingleAsync(v => v.WordId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(vocab.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVocabulary(VocabularyItemViewModel vocab)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(vocab.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                VocabularyItem model = new VocabularyItem();
                model.Author = vocab.Author;
                model.ProgenyId = vocab.ProgenyId;
                model.AccessLevel = vocab.AccessLevel;
                model.Date = vocab.Date;
                if (model.Date == null)
                {
                    model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
                model.DateAdded = vocab.DateAdded;
                model.Description = vocab.Description;
                model.Language = vocab.Language;
                model.SoundsLike = vocab.SoundsLike;
                model.Word = vocab.Word;
                model.WordId = vocab.WordId;

                await _progenyHttpClient.UpdateWord(model);
                //_context.VocabularyDb.Update(model);
                //await _context.SaveChangesAsync();

            }
            return RedirectToAction("Index", "Vocabulary");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVocabulary(int itemId)
        {
            VocabularyItem model = await _progenyHttpClient.GetWord(itemId); // _context.VocabularyDb.SingleAsync(v => v.WordId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVocabulary(VocabularyItem model)
        {
            VocabularyItem vocab = await _progenyHttpClient.GetWord(model.WordId); // _context.VocabularyDb.SingleAsync(v => v.WordId == model.WordId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteWord(vocab.WordId);
            //_context.VocabularyDb.Remove(vocab);
            //await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Vocabulary");
        }


        [HttpGet]
        public async Task<IActionResult> AddSkill()
        {
            SkillViewModel model = new SkillViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(SkillViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Skill skillItem = new Skill();
            skillItem.ProgenyId = model.ProgenyId;
            skillItem.Category = model.Category;
            skillItem.Description = model.Description;
            skillItem.Name = model.Name;
            skillItem.SkillAddedDate = DateTime.UtcNow;
            if (model.SkillFirstObservation == null)
            {
                model.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            skillItem.SkillFirstObservation = model.SkillFirstObservation;
            skillItem.AccessLevel = model.AccessLevel;
            skillItem.Author = userinfo.UserId;

            await _progenyHttpClient.AddSkill(skillItem);
            //await _context.SkillsDb.AddAsync(skillItem);
            //await _context.SaveChangesAsync();
            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= skillItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        string skillTimeString = "\r\nDate: " + skillItem.SkillFirstObservation.Value.ToString("dd-MMM-yyyy");
                        
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Skill: " + skillItem.Name + "\r\nCategory: " + skillItem.Category + skillTimeString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new skill was added for " + progeny.NickName;
                        notification.Link = "/Skills?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunaskill" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> EditSkill(int itemId)
        {
            SkillViewModel model = new SkillViewModel();
            Skill skill = await _progenyHttpClient.GetSkill(itemId); // _context.SkillsDb.SingleAsync(s => s.SkillId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(skill.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = skill.ProgenyId;
            model.AccessLevel = skill.AccessLevel;
            model.Author = skill.Author;
            model.Category = skill.Category;
            model.Description = skill.Description;
            model.Name = skill.Name;
            model.SkillAddedDate = skill.SkillAddedDate;
            if (skill.SkillFirstObservation == null)
            {
                skill.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            model.SkillFirstObservation = skill.SkillFirstObservation;
            model.SkillId = skill.SkillId;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(SkillViewModel skill)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(skill.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Skill model = new Skill();

                model.ProgenyId = skill.ProgenyId;
                model.AccessLevel = skill.AccessLevel;
                model.Author = skill.Author;
                model.Category = skill.Category;
                model.Description = skill.Description;
                model.Name = skill.Name;
                model.SkillAddedDate = skill.SkillAddedDate;
                if (skill.SkillFirstObservation == null)
                {
                    skill.SkillFirstObservation = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
                model.SkillFirstObservation = skill.SkillFirstObservation;
                model.SkillId = skill.SkillId;

                await _progenyHttpClient.UpdateSkill(model);
                //_context.SkillsDb.Update(model);
                //await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSkill(int itemId)
        {
            Skill model = await _progenyHttpClient.GetSkill(itemId); // _context.SkillsDb.SingleAsync(s => s.SkillId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(Skill model)
        {
            Skill skill = await _progenyHttpClient.GetSkill(model.SkillId); // _context.SkillsDb.SingleAsync(s => s.SkillId == model.SkillId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteSkill(skill.SkillId);
            //_context.SkillsDb.Remove(skill);
            //await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Skills");
        }

        [HttpGet]
        public async Task<IActionResult> AddFriend()
        {
            FriendViewModel model = new FriendViewModel();
            List<string> tagsList = new List<string>();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);

                        var friendsList1 = await _progenyHttpClient.GetFriendsList(prog.Id, 0); // _context.FriendsDb.Where(f => f.ProgenyId == prog.Id).ToList();
                        foreach (Friend frn in friendsList1)
                        {
                            if (!String.IsNullOrEmpty(frn.Tags))
                            {
                                List<string> fvmTags = frn.Tags.Split(',').ToList();
                                foreach (string tagstring in fvmTags)
                                {
                                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                    {
                                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
            }
            tagItems = tagItems + "]";
            ViewBag.TagsList = tagItems;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddFriend(FriendViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            Friend friendItem = new Friend();
            friendItem.ProgenyId = model.ProgenyId;
            friendItem.Description = model.Description;
            friendItem.FriendAddedDate = DateTime.UtcNow;
            if (model.FriendSince == null)
            {
                model.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            friendItem.FriendSince = model.FriendSince;
            friendItem.Name = model.Name;
            friendItem.AccessLevel = model.AccessLevel;
            friendItem.Type = model.Type;
            friendItem.Context = model.Context;
            friendItem.Notes = model.Notes;
            friendItem.Author = userinfo.UserId;
            if (!String.IsNullOrEmpty(model.Tags))
            {
                friendItem.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (model.File != null)
            {
                using (var stream = model.File.OpenReadStream())
                {
                    friendItem.PictureLink = await _imageStore.SaveImage(stream, "friends");

                }
            }
            else
            {
                friendItem.PictureLink = Constants.ProfilePictureUrl;
            }

            await _progenyHttpClient.AddFriend(friendItem);
            // await _context.FriendsDb.AddAsync(friendItem);
            // await _context.SaveChangesAsync();
            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= friendItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new friend was added for " + progeny.NickName;
                        notification.Link = "/Friends?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunafriend" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> EditFriend(int itemId)
        {
            FriendViewModel model = new FriendViewModel();
            Friend friend = await _progenyHttpClient.GetFriend(itemId); // _context.FriendsDb.SingleAsync(f => f.FriendId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = friend.ProgenyId;
            model.AccessLevel = friend.AccessLevel;
            model.Author = friend.Author;
            if (friend.FriendSince == null)
            {
                friend.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            model.FriendAddedDate = friend.FriendAddedDate;
            model.Description = friend.Description;
            model.Name = friend.Name;
            model.FriendId = friend.FriendId;
            model.FriendSince = friend.FriendSince;
            model.PictureLink = friend.PictureLink;
            if (!friend.PictureLink.ToLower().StartsWith("http"))
            {
                model.PictureLink = _imageStore.UriFor(friend.PictureLink, "friends");
            }
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            model.Type = friend.Type;
            model.FriendTypeListEn[model.Type].Selected = true;
            model.FriendTypeListDa[model.Type].Selected = true;
            model.FriendTypeListDe[model.Type].Selected = true;
            model.Context = friend.Context;
            model.Notes = friend.Notes;
            model.Tags = friend.Tags;

            List<string> tagsList = new List<string>();
            var friendsList1 = await _progenyHttpClient.GetFriendsList(model.ProgenyId, 0); // _context.FriendsDb.Where(f => f.ProgenyId == model.ProgenyId).ToList();
            foreach (Friend frn in friendsList1)
            {
                if (!String.IsNullOrEmpty(frn.Tags))
                {
                    List<string> fvmTags = frn.Tags.Split(',').ToList();
                    foreach (string tagstring in fvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems = tagItems + "]";
            }

            ViewBag.TagsList = tagItems;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditFriend(FriendViewModel friend)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Friend model = await _progenyHttpClient.GetFriend(friend.FriendId); // _context.FriendsDb.SingleOrDefaultAsync(f => f.FriendId == friend.FriendId);
                model.AccessLevel = friend.AccessLevel;
                model.Author = friend.Author;
                model.Description = friend.Description;
                model.Name = friend.Name;
                model.FriendId = friend.FriendId;
                if (friend.FriendSince == null)
                {
                    friend.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
                }
                model.FriendSince = friend.FriendSince;
                model.Type = friend.Type;
                model.Context = friend.Context;
                model.Notes = friend.Notes;
                if (!String.IsNullOrEmpty(friend.Tags))
                {
                    model.Tags = friend.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
                }
                if (friend.File != null && friend.File.Name != String.Empty)
                {
                    string oldPictureLink = friend.PictureLink;
                    friend.FileName = friend.File.FileName;
                    using (var stream = friend.File.OpenReadStream())
                    {
                        friend.PictureLink = await _imageStore.SaveImage(stream, "friends");
                    }

                    if (!oldPictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(oldPictureLink, "friends");
                    }
                }

                await _progenyHttpClient.UpdateFriend(model);
                //_context.FriendsDb.Update(model);
                //await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFriend(int itemId)
        {
            Friend model = await _progenyHttpClient.GetFriend(itemId); // _context.FriendsDb.SingleAsync(f => f.FriendId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFriend(Friend model)
        {
            Friend friend = await _progenyHttpClient.GetFriend(model.FriendId); //_context.FriendsDb.SingleAsync(f => f.FriendId == model.FriendId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteFriend(model.FriendId);
            //_context.FriendsDb.Remove(friend);
            //await _context.SaveChangesAsync();
            
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> AddMeasurement()
        {
            MeasurementViewModel model = new MeasurementViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }

            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeasurement(MeasurementViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
            measurementItem.Author = userinfo.UserId;

            await _progenyHttpClient.AddMeasurement(measurementItem);
            //await _context.MeasurementsDb.AddAsync(measurementItem);
            //await _context.SaveChangesAsync();
            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= measurementItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Height: " + measurementItem.Height + "\r\nWeight: " + measurementItem.Weight;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new measurement was added for " + progeny.NickName;
                        notification.Link = "/Measurements?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

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
            Measurement measurement = await _progenyHttpClient.GetMeasurement(itemId); // _context.MeasurementsDb.SingleAsync(m => m.MeasurementId == itemId);

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMeasurement(MeasurementViewModel measurement)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Measurement model = new Measurement();
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

                await _progenyHttpClient.UpdateMeasurement(model);
                //_context.MeasurementsDb.Update(model);
                //await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteMeasurement(int itemId)
        {
            Measurement model = await _progenyHttpClient.GetMeasurement(itemId); // _context.MeasurementsDb.SingleAsync(m => m.MeasurementId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMeasurement(Measurement model)
        {
            Measurement measurement = await _progenyHttpClient.GetMeasurement(model.MeasurementId); // _context.MeasurementsDb.SingleAsync(m => m.MeasurementId == model.MeasurementId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(measurement.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteMeasurement(model.MeasurementId);
            //_context.MeasurementsDb.Remove(measurement);
            //await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Measurements");
        }

        [HttpGet]
        public async Task<IActionResult> AddContact()
        {
            ContactViewModel model = new ContactViewModel();

            List<string> tagsList = new List<string>();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            foreach (var item in accessList)
            {
                var contactsList1 = await _progenyHttpClient.GetContactsList(item.Id, 0); // _context.ContactsDb.Where(c => c.ProgenyId == item.Id).ToList();
                foreach (Contact cont in contactsList1)
                {
                    if (!String.IsNullOrEmpty(cont.Tags))
                    {
                        List<string> cvmTags = cont.Tags.Split(',').ToList();
                        foreach (string tagstring in cvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            ViewBag.TagsList = tagItems;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddContact(ContactViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Contact contactItem = new Contact();
            contactItem.FirstName = model.FirstName;
            contactItem.MiddleName = model.MiddleName;
            contactItem.LastName = model.LastName;
            contactItem.DisplayName = model.DisplayName;
            contactItem.Email1 = model.Email1;
            contactItem.Email2 = model.Email2;
            contactItem.PhoneNumber = model.PhoneNumber;
            contactItem.MobileNumber = model.MobileNumber;
            contactItem.Notes = model.Notes;
            contactItem.Website = model.Website;
            contactItem.Active = true;
            contactItem.Context = model.Context;
            contactItem.AccessLevel = model.AccessLevel;
            contactItem.Author = userinfo.UserId;
            contactItem.ProgenyId = model.ProgenyId;
            contactItem.DateAdded = DateTime.UtcNow;
            if (!String.IsNullOrEmpty(model.Tags))
            {
                contactItem.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            if (model.File != null)
            {
                model.FileName = model.File.FileName;
                using (var stream = model.File.OpenReadStream())
                {
                    model.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                }
            }
            else
            {
                contactItem.PictureLink = Constants.ProfilePictureUrl;
            }

            if (model.AddressLine1 + model.AddressLine2 + model.City + model.Country + model.PostalCode + model.State !=
                "")
            {
                Address address = new Address();
                address.AddressLine1 = model.AddressLine1;
                address.AddressLine2 = model.AddressLine2;
                address.City = model.City;
                address.PostalCode = model.PostalCode;
                address.State = model.State;
                address.Country = model.Country;
                contactItem.Address = address;
            }

            await _progenyHttpClient.AddContact(contactItem);
            // await _context.ContactsDb.AddAsync(contactItem);
            // await _context.SaveChangesAsync();

            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= contactItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + contactItem.DisplayName + "\r\nContext: " + contactItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new contact was added for " + progeny.NickName;
                        notification.Link = "/Contacts/ContactDetails?contactId=" + contactItem.ContactId + "&childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunacontact" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> EditContact(int itemId)
        {
            ContactViewModel model = new ContactViewModel();
            Contact contact = await _progenyHttpClient.GetContact(itemId); // _context.ContactsDb.SingleAsync(c => c.ContactId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ContactId = contact.ContactId;
            model.Active = contact.Active;
            model.ProgenyId = contact.ProgenyId;
            model.AccessLevel = contact.AccessLevel;
            model.Author = contact.Author;
            model.FirstName = contact.FirstName;
            model.MiddleName = contact.MiddleName;
            model.LastName = contact.LastName;
            model.DisplayName = contact.DisplayName;
            if (contact.AddressIdNumber != null)
            {
                model.AddressIdNumber = contact.AddressIdNumber;
                model.Address = await _progenyHttpClient.GetAddress(model.AddressIdNumber.Value); // _context.AddressDb.SingleAsync(c => c.AddressId == model.AddressIdNumber);
                model.AddressLine1 = model.Address.AddressLine1;
                model.AddressLine2 = model.Address.AddressLine2;
                model.City = model.Address.City;
                model.PostalCode = model.Address.PostalCode;
                model.State = model.Address.State;
                model.Country = model.Address.Country;
            }
            model.Email1 = contact.Email1;
            model.Email2 = contact.Email2;
            model.PhoneNumber = contact.PhoneNumber;
            model.MobileNumber = contact.MobileNumber;
            model.Website = contact.Website;
            model.Notes = contact.Notes;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            model.Context = contact.Context;
            model.Notes = contact.Notes;
            model.PictureLink = contact.PictureLink;
            if (!contact.PictureLink.ToLower().StartsWith("http"))
            {
                model.PictureLink = _imageStore.UriFor(contact.PictureLink, "contacts");
            }
            DateTime tempTime = contact.DateAdded ?? DateTime.UtcNow;
            model.DateAdded = TimeZoneInfo.ConvertTimeFromUtc(tempTime, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            model.Tags = contact.Tags;

            List<string> tagsList = new List<string>();
            var contactsList1 = await _progenyHttpClient.GetContactsList(model.ProgenyId, 0); // _context.ContactsDb.Where(c => c.ProgenyId == model.ProgenyId).ToList();
            foreach (Contact cont in contactsList1)
            {
                if (!String.IsNullOrEmpty(cont.Tags))
                {
                    List<string> cvmTags = cont.Tags.Split(',').ToList();
                    foreach (string tagstring in cvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems = tagItems + "]";
            }

            ViewBag.TagsList = tagItems;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditContact(ContactViewModel contact)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Contact model = await _progenyHttpClient.GetContact(contact.ContactId); // _context.ContactsDb.SingleOrDefaultAsync(c => c.ContactId == contact.ContactId);
                model.ContactId = contact.ContactId;
                model.ProgenyId = contact.ProgenyId;
                model.Active = contact.Active;
                model.AccessLevel = contact.AccessLevel;
                model.Author = contact.Author;
                model.FirstName = contact.FirstName;
                model.MiddleName = contact.MiddleName;
                model.LastName = contact.LastName;
                model.DisplayName = contact.DisplayName;
                model.DateAdded = contact.DateAdded;
                model.AddressIdNumber = contact.AddressIdNumber;
                if (contact.AddressLine1 + contact.AddressLine2 + contact.City + contact.Country + contact.PostalCode +
                    contact.State != "")
                {
                    Address address = new Address();
                    address.AddressLine1 = contact.AddressLine1;
                    address.AddressLine2 = contact.AddressLine2;
                    address.City = contact.City;
                    address.PostalCode = contact.PostalCode;
                    address.State = contact.State;
                    address.Country = contact.Country;
                    model.Address = address;
                }
                
                model.Email1 = contact.Email1;
                model.Email2 = contact.Email2;
                model.PhoneNumber = contact.PhoneNumber;
                model.MobileNumber = contact.MobileNumber;
                model.Notes = contact.Notes;
                model.Context = contact.Context;
                model.Website = contact.Website;
                if (contact.File != null && contact.File.Name != String.Empty)
                {
                    string oldPictureLink = contact.PictureLink;
                    contact.FileName = contact.File.FileName;
                    using (var stream = contact.File.OpenReadStream())
                    {
                        model.PictureLink = await _imageStore.SaveImage(stream, "contacts");
                    }

                    if (!oldPictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(oldPictureLink, "contacts");
                    }
                }
                
                if (model.DateAdded == null)
                {
                    model.DateAdded = DateTime.UtcNow;
                }
                if (!String.IsNullOrEmpty(contact.Tags))
                {
                    model.Tags = contact.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
                }

                await _progenyHttpClient.UpdateContact(model);
                // _context.ContactsDb.Update(model);
                // await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteContact(int itemId)
        {
            Contact model = await _progenyHttpClient.GetContact(itemId); // _context.ContactsDb.SingleAsync(c => c.ContactId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact(Contact model)
        {
            Contact contact = await _progenyHttpClient.GetContact(model.ContactId); // _context.ContactsDb.SingleAsync(c => c.ContactId == model.ContactId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(contact.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            
            await _progenyHttpClient.DeleteContact(contact.ContactId); // _context.ContactsDb.Remove(contact));
            // await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Contacts");
        }

        [HttpGet]
        public async Task<IActionResult> AddVaccination()
        {
            VaccinationViewModel model = new VaccinationViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVaccination(VaccinationViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Vaccination vacItem = new Vaccination();
            vacItem.VaccinationName = model.VaccinationName;
            vacItem.ProgenyId = model.ProgenyId;
            vacItem.VaccinationDescription = model.VaccinationDescription;
            vacItem.VaccinationDate = model.VaccinationDate;
            vacItem.Notes = model.Notes;
            vacItem.AccessLevel = model.AccessLevel;
            vacItem.Author = userinfo.UserId;

            await _progenyHttpClient.AddVaccination(vacItem);
            //await _context.VaccinationsDb.AddAsync(vacItem);
            //await _context.SaveChangesAsync();
            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= vacItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + vacItem.VaccinationName + "\r\nContext: " + vacItem.VaccinationDate.ToString("dd-MMM-yyyy");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new vaccination was added for " + progeny.NickName;
                        notification.Link = "/Vaccinations?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunavaccination" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> EditVaccination(int itemId)
        {
            VaccinationViewModel model = new VaccinationViewModel();
            Vaccination vaccination = await _progenyHttpClient.GetVaccination(itemId); // _context.VaccinationsDb.SingleAsync(v => v.VaccinationId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(vaccination.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.VaccinationId = vaccination.VaccinationId;
            model.ProgenyId = vaccination.ProgenyId;
            model.AccessLevel = vaccination.AccessLevel;
            model.Author = vaccination.Author;
            model.VaccinationName = vaccination.VaccinationName;
            model.VaccinationDate = vaccination.VaccinationDate;
            model.VaccinationDescription = vaccination.VaccinationDescription;
            model.Notes = vaccination.Notes;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVaccination(VaccinationViewModel vaccination)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(vaccination.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Vaccination model = new Vaccination();
                model.VaccinationId = vaccination.VaccinationId;
                model.ProgenyId = vaccination.ProgenyId;
                model.AccessLevel = vaccination.AccessLevel;
                model.Author = vaccination.Author;
                model.VaccinationName = vaccination.VaccinationName;
                model.VaccinationDate = vaccination.VaccinationDate;
                model.VaccinationDescription = vaccination.VaccinationDescription;
                model.Notes = vaccination.Notes;

                await _progenyHttpClient.UpdateVaccination(model);
                //_context.VaccinationsDb.Update(model);
                //await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteVaccination(int itemId)
        {
            Vaccination model = await _progenyHttpClient.GetVaccination(itemId); // _context.VaccinationsDb.SingleAsync(v => v.VaccinationId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVaccination(Vaccination model)
        {
            Vaccination vaccination = await _progenyHttpClient.GetVaccination(model.VaccinationId); // _context.VaccinationsDb.SingleAsync(v => v.VaccinationId == model.VaccinationId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteVaccination(vaccination.VaccinationId);
            //_context.VaccinationsDb.Remove(vaccination);
            //await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Vaccinations");
        }

        [HttpGet]
        public async Task<IActionResult> AddSleep()
        {
            SleepViewModel model = new SleepViewModel();

            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }
            List<Progeny> accessList = new List<Progeny>();
            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
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
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);
                    }
                }
            }
            model.Progeny = accessList[0];
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSleep(SleepViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Sleep sleepItem = new Sleep();
            sleepItem.ProgenyId = model.ProgenyId;
            sleepItem.Progeny = prog;
            sleepItem.CreatedDate = DateTime.UtcNow;
            if (model.SleepStart.HasValue && model.SleepEnd.HasValue)
            {
                sleepItem.SleepStart = TimeZoneInfo.ConvertTimeToUtc(model.SleepStart.Value, TimeZoneInfo.FindSystemTimeZoneById(sleepItem.Progeny.TimeZone));
                sleepItem.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(model.SleepEnd.Value, TimeZoneInfo.FindSystemTimeZoneById(sleepItem.Progeny.TimeZone));
            }
            sleepItem.SleepRating = model.SleepRating;
            if (sleepItem.SleepRating == 0)
            {
                sleepItem.SleepRating = 3;
            }
            sleepItem.SleepNotes = model.SleepNotes;
            sleepItem.AccessLevel = model.AccessLevel;
            sleepItem.Author = userinfo.UserId;

            sleepItem = await _progenyHttpClient.AddSleep(sleepItem);
            //await _context.SleepDb.AddAsync(sleepItem);
            //await _context.SaveChangesAsync();
            
            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(sleepItem.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= sleepItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        var sleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepStart,
                            TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));
                        var sleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleepItem.SleepEnd,
                            TimeZoneInfo.FindSystemTimeZoneById(uaUserInfo.Timezone));

                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Start: " + sleepStart.ToString("dd-MMM-yyyy HH:mm") + "\r\nEnd: " +sleepEnd.ToString("dd-MMM-yyyy HH:mm");
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "Sleep Added for " + prog.NickName;
                        notification.Link = "/Sleep?childId=" + prog.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title, notification.Message,
                            Constants.WebAppUrl + notification.Link, "kinaunasleep" + prog.Id);
                    }
                }
            }
            
            // Todo: send notification to others.
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> EditSleep(int itemId)
        {
            SleepViewModel model = new SleepViewModel();
            Sleep sleep = await _progenyHttpClient.GetSleepItem(itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = sleep.ProgenyId;
            model.Progeny = prog;
            model.SleepId = sleep.SleepId;
            model.AccessLevel = sleep.AccessLevel;
            model.Author = sleep.Author;
            model.CreatedDate = sleep.CreatedDate;
            model.SleepStart = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepStart, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
            model.SleepEnd = TimeZoneInfo.ConvertTimeFromUtc(sleep.SleepEnd, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
            model.SleepRating = sleep.SleepRating;
            if (model.SleepRating == 0)
            {
                model.SleepRating = 3;
            }
            model.SleepNotes = sleep.SleepNotes;
            model.Progeny = prog;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;
            ViewBag.RatingList = new List<SelectListItem>();
            SelectListItem selItem1 = new SelectListItem();
            selItem1.Text = "1";
            selItem1.Value = "1";
            SelectListItem selItem2 = new SelectListItem();
            selItem2.Text = "2";
            selItem2.Value = "2";
            SelectListItem selItem3 = new SelectListItem();
            selItem3.Text = "3";
            selItem3.Value = "3";
            SelectListItem selItem4 = new SelectListItem();
            selItem4.Text = "4";
            selItem4.Value = "4";
            SelectListItem selItem5 = new SelectListItem();
            selItem5.Text = "5";
            selItem5.Value = "5";
            ViewBag.RatingList.Add(selItem1);
            ViewBag.RatingList.Add(selItem2);
            ViewBag.RatingList.Add(selItem3);
            ViewBag.RatingList.Add(selItem4);
            ViewBag.RatingList.Add(selItem5);
            ViewBag.RatingList[model.SleepRating - 1].Selected = true;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSleep(SleepViewModel sleep)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            if (ModelState.IsValid)
            {
                Sleep model = new Sleep();
                model.ProgenyId = sleep.ProgenyId;
                model.Progeny = prog;
                model.SleepId = sleep.SleepId;
                model.AccessLevel = sleep.AccessLevel;
                model.Author = sleep.Author;
                model.CreatedDate = sleep.CreatedDate;
                if (sleep.SleepStart.HasValue && sleep.SleepEnd.HasValue)
                {
                    model.SleepStart = TimeZoneInfo.ConvertTimeToUtc(sleep.SleepStart.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
                    model.SleepEnd = TimeZoneInfo.ConvertTimeToUtc(sleep.SleepEnd.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
                }
                model.SleepRating = sleep.SleepRating;
                if (model.SleepRating == 0)
                {
                    model.SleepRating = 3;
                }
                model.SleepNotes = sleep.SleepNotes;
                await _progenyHttpClient.UpdateSleep(model);
                //_context.SleepDb.Update(model);
                //await _context.SaveChangesAsync();
                
            }
            return RedirectToAction("Index", "Sleep");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteSleep(int itemId)
        {
            Sleep model = await _progenyHttpClient.GetSleepItem(itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSleep(Sleep model)
        {
            Sleep sleep = await _progenyHttpClient.GetSleepItem(model.SleepId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(sleep.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteSleepItem(model.SleepId);
            //_context.SleepDb.Remove(sleep);
            //await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Sleep");
        }

        public async Task<IActionResult> AddLocation()
        {
            LocationViewModel model = new LocationViewModel();

            List<string> tagsList = new List<string>();
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    model.Progeny = accessList[0];
                    foreach (Progeny prog in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == prog.Id).NickName,
                            Value = prog.Id.ToString()
                        };
                        if (prog.Id == _progId)
                        {
                            selItem.Selected = true;
                            model.Progeny = prog;
                        }

                        model.ProgenyList.Add(selItem);

                        var locList1 = _context.LocationsDb.Where(l => l.ProgenyId == prog.Id).ToList();
                        foreach (Location loc in locList1)
                        {
                            if (!String.IsNullOrEmpty(loc.Tags))
                            {
                                List<string> locTags = loc.Tags.Split(',').ToList();
                                foreach (string tagstring in locTags)
                                {
                                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                    {
                                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            model.TagsList = tagItems;
            model.Latitude = 30.94625288456589;
            model.Longitude = -54.10861860580418;
            model.Date = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLocation(LocationViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            model.Progeny = prog;
            Location locItem = new Location();
            locItem.Latitude = model.Latitude;
            locItem.Longitude = model.Longitude;
            locItem.Name = model.Name;
            locItem.HouseNumber = model.HouseNumber;
            locItem.StreetName = model.StreetName;
            locItem.District = model.District;
            locItem.City = model.City;
            locItem.PostalCode = model.PostalCode;
            locItem.County = model.County;
            locItem.State = model.State;
            locItem.Country = model.Country;
            locItem.Notes = model.Notes;
            if (model.Date.HasValue)
            {
                locItem.Date = TimeZoneInfo.ConvertTimeToUtc(model.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
            }
            if (!String.IsNullOrEmpty(model.Tags))
            {
                locItem.Tags = model.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }
            locItem.ProgenyId = model.ProgenyId;
            locItem.DateAdded = DateTime.UtcNow;
            locItem.Author = userinfo.UserId;
            locItem.AccessLevel = model.AccessLevel;

            await _progenyHttpClient.AddLocation(locItem);
            //await _context.LocationsDb.AddAsync(locItem);
            //await _context.SaveChangesAsync();

            string authorName = "";
            if (!String.IsNullOrEmpty(userinfo.FirstName))
            {
                authorName = userinfo.FirstName;
            }
            if (!String.IsNullOrEmpty(userinfo.MiddleName))
            {
                authorName = authorName + " " + userinfo.MiddleName;
            }
            if (!String.IsNullOrEmpty(userinfo.LastName))
            {
                authorName = authorName + " " + userinfo.LastName;
            }

            authorName = authorName.Trim();
            if (String.IsNullOrEmpty(authorName))
            {
                authorName = userinfo.UserName;
            }
            List<UserAccess> usersToNotif = await _progenyHttpClient.GetProgenyAccessList(model.ProgenyId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= locItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _progenyHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        DateTime tempDate = DateTime.UtcNow;
                        if (locItem.Date.HasValue)
                        {
                            tempDate = TimeZoneInfo.ConvertTimeFromUtc(locItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(model.Progeny.TimeZone));
                        }

                        string dateString = tempDate.ToString("dd-MMM-yyyy");
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Name: " + locItem.Name + "\r\nDate: " + dateString;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = userinfo.ProfilePicture;
                        notification.Title = "A new location was added for " + progeny.NickName;
                        notification.Link = "/Locations?childId=" + progeny.Id;
                        notification.Type = "Notification";
                        await _context.WebNotificationsDb.AddAsync(notification);
                        await _context.SaveChangesAsync();

                        await _pushMessageSender.SendMessage(uaUserInfo.UserId, notification.Title,
                            notification.Message, Constants.WebAppUrl + notification.Link, "kinaunalocation" + progeny.Id);
                    }
                }
            }
            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> EditLocation(int itemId)
        {
            LocationViewModel model = new LocationViewModel();
            List<string> tagsList = new List<string>();
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (userinfo == null)
            {
                return RedirectToAction("Index");
            }
            if (userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (User.Identity.IsAuthenticated && userEmail != null && userinfo.UserId != null)
            {
                List<Progeny> accessList = await _progenyHttpClient.GetProgenyAdminList(userEmail);
                if (accessList.Any())
                {
                    foreach (Progeny chld in accessList)
                    {
                        SelectListItem selItem = new SelectListItem()
                        {
                            Text = accessList.Single(p => p.Id == chld.Id).NickName,
                            Value = chld.Id.ToString()
                        };
                        if (chld.Id == _progId)
                        {
                            selItem.Selected = true;
                        }

                        model.ProgenyList.Add(selItem);

                        var locList1 = await _progenyHttpClient.GetLocationsList(chld.Id, 0); // _context.LocationsDb.Where(l => l.ProgenyId == chld.Id).ToList();
                        foreach (Location loc in locList1)
                        {
                            if (!String.IsNullOrEmpty(loc.Tags))
                            {
                                List<string> locTags = loc.Tags.Split(',').ToList();
                                foreach (string tagstring in locTags)
                                {
                                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                    {
                                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Location locItem = await _progenyHttpClient.GetLocation(itemId); // _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            model.Progeny = prog;
            model.LocationId = locItem.LocationId;
            model.Latitude = locItem.Latitude;
            model.Longitude = locItem.Longitude;
            model.Name = locItem.Name;
            model.HouseNumber = locItem.HouseNumber;
            model.StreetName = locItem.StreetName;
            model.District = locItem.District;
            model.City = locItem.City;
            model.PostalCode = locItem.PostalCode;
            model.County = locItem.County;
            model.State = locItem.State;
            model.Country = locItem.Country;
            model.Notes = locItem.Notes;
            if (locItem.Date.HasValue)
            {
                model.Date = TimeZoneInfo.ConvertTimeFromUtc(locItem.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }

            model.Tags = locItem.Tags;
            model.ProgenyId = locItem.ProgenyId;
            model.DateAdded = locItem.DateAdded;
            model.Author = locItem.Author;
            model.AccessLevel = locItem.AccessLevel;
            model.AccessLevelListEn[model.AccessLevel].Selected = true;
            model.AccessLevelListDa[model.AccessLevel].Selected = true;
            model.AccessLevelListDe[model.AccessLevel].Selected = true;

            string tagItems = "[";
            if (tagsList.Any())
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);

            }
            tagItems = tagItems + "]";
            model.TagsList = tagItems;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLocation(LocationViewModel model)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Location locItem = await _progenyHttpClient.GetLocation(model.LocationId); // _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == model.LocationId);
            model.Progeny = prog;
            locItem.Latitude = model.Latitude;
            locItem.Longitude = model.Longitude;
            locItem.Name = model.Name;
            locItem.HouseNumber = model.HouseNumber;
            locItem.StreetName = model.StreetName;
            locItem.District = model.District;
            locItem.City = model.City;
            locItem.PostalCode = model.PostalCode;
            locItem.County = model.County;
            locItem.State = model.State;
            locItem.Country = model.Country;
            locItem.Notes = model.Notes;
            if (model.Date.HasValue)
            {
                locItem.Date = TimeZoneInfo.ConvertTimeToUtc(model.Date.Value, TimeZoneInfo.FindSystemTimeZoneById(userinfo.Timezone));
            }
            if (!String.IsNullOrEmpty(model.Tags))
            {
                locItem.Tags = model.Tags.Trim().TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            locItem.AccessLevel = model.AccessLevel;

            await _progenyHttpClient.UpdateLocation(locItem);
            //_context.LocationsDb.Update(locItem);
            //await _context.SaveChangesAsync();

            

            return RedirectToAction("Index", "Locations");
        }

        public async Task<IActionResult> DeleteLocation(int itemId)
        {

            Location model = await _progenyHttpClient.GetLocation(itemId); // _context.LocationsDb.SingleAsync(l => l.LocationId == itemId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLocation(Location model)
        {

            Location locItem = await _progenyHttpClient.GetLocation(model.LocationId); // _context.LocationsDb.SingleAsync(l => l.LocationId == model.LocationId);
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);

            Progeny prog = await _progenyHttpClient.GetProgeny(locItem.ProgenyId);
            if (!prog.Admins.ToUpper().Contains(userinfo.UserEmail.ToUpper()))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _progenyHttpClient.DeleteLocation(locItem.LocationId);
            //_context.LocationsDb.Remove(locItem);
            //await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Locations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFile(FileItem model)
        {
            
            throw new NotImplementedException();
        }
    }
}