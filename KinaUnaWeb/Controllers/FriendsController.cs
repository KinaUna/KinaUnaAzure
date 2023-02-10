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
using System.IO;

namespace KinaUnaWeb.Controllers
{
    public class FriendsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IFriendsHttpClient _friendsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IPushMessageSender _pushMessageSender;
        private readonly IWebNotificationsService _webNotificationsService;

        public FriendsController(IProgenyHttpClient progenyHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, IFriendsHttpClient friendsHttpClient,
            IUserAccessHttpClient userAccessHttpClient, IPushMessageSender pushMessageSender, IWebNotificationsService webNotificationsService)
        {
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _friendsHttpClient = friendsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
            _pushMessageSender = pushMessageSender;
            _webNotificationsService = webNotificationsService;
        }
        

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            FriendsListViewModel model = new FriendsListViewModel();
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

            List<string> tagsList = new List<string>();
            List<Friend> friendsList = await _friendsHttpClient.GetFriendsList(childId, userAccessLevel);
            if (!string.IsNullOrEmpty(tagFilter))
            {
                friendsList = friendsList.Where(c => c.Tags != null && c.Tags.ToUpper().Contains(tagFilter.ToUpper())).ToList();
            }

            friendsList = friendsList.OrderBy(f => f.FriendSince).ToList();
            if (friendsList.Count != 0)
            {
                foreach (Friend friend in friendsList)
                {
                    FriendViewModel friendViewModel = new FriendViewModel();
                    friendViewModel.ProgenyId = friend.ProgenyId;
                    friendViewModel.AccessLevel = friend.AccessLevel;
                    friendViewModel.FriendAddedDate = friend.FriendAddedDate;
                    friendViewModel.FriendSince = friend.FriendSince;
                    friendViewModel.Name = friend.Name;
                    friendViewModel.Description = friend.Description;
                    friendViewModel.IsAdmin = model.IsAdmin;
                    friendViewModel.FriendId = friend.FriendId;
                    friendViewModel.PictureLink = friend.PictureLink;
                    friendViewModel.Type = friend.Type;
                    friendViewModel.Context = friend.Context;
                    friendViewModel.Notes = friend.Notes;
                    friendViewModel.Tags = friend.Tags;
                    if (!String.IsNullOrEmpty(friendViewModel.Tags))
                    {
                        List<string> pvmTags = friendViewModel.Tags.Split(',').ToList();
                        foreach (string tagstring in pvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }
                    if (!friendViewModel.PictureLink.StartsWith("https://"))
                    {
                        friendViewModel.PictureLink = _imageStore.UriFor(friendViewModel.PictureLink, "friends");
                    }

                    if (friendViewModel.AccessLevel >= userAccessLevel)
                    {
                        model.FriendViewModelsList.Add(friendViewModel);
                    }
                }

                string tags = "";
                foreach (string tstr in tagsList)
                {
                    tags = tags + tstr + ",";
                }
                model.Tags = tags.TrimEnd(',');
            }
            else
            {
                FriendViewModel friendViewModel = new FriendViewModel();
                friendViewModel.ProgenyId = childId;
                friendViewModel.Name = "No friends found.";
                friendViewModel.FriendAddedDate = DateTime.UtcNow;
                friendViewModel.FriendSince = DateTime.UtcNow;
                friendViewModel.Description = "The friends list is empty.";
                friendViewModel.IsAdmin = model.IsAdmin;
                model.FriendViewModelsList.Add(friendViewModel);
            }

            model.Progeny = progeny;
            model.TagFilter = tagFilter;
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> FriendDetails(int friendId, string tagFilter)
        {
            FriendViewModel model = new FriendViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Friend friend = await _friendsHttpClient.GetFriend(friendId); 
            Progeny progeny = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(progeny.Id);

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


            
            
            model.ProgenyId = friend.ProgenyId;
            model.Context = friend.Context;
            model.Notes = friend.Notes;
            model.Type = friend.Type;
            model.AccessLevel = friend.AccessLevel;
            model.Description = friend.Description;
            model.FriendAddedDate = friend.FriendAddedDate;
            model.FriendId = friend.FriendId;
            model.FriendSince = friend.FriendSince;
            model.PictureLink = friend.PictureLink;
            model.Name = friend.Name;
            model.Tags = friend.Tags;
            model.Progeny = progeny;
            
            if (!model.PictureLink.StartsWith("https://"))
            {
                model.PictureLink = _imageStore.UriFor(model.PictureLink, "friends");
            }

            List<string> tagsList = new List<string>();
            List<Friend> friendsList = await _friendsHttpClient.GetFriendsList(model.ProgenyId, userAccessLevel);
            foreach (Friend frn in friendsList)
            {
                if (!string.IsNullOrEmpty(frn.Tags))
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
            ViewBag.TagFilter = tagFilter;
            if (model.AccessLevel < userAccessLevel)
            {
                return RedirectToAction("Index");
            }
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddFriend()
        {
            FriendViewModel model = new FriendViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            List<string> tagsList = new List<string>();

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

                        List<Friend> friendsList1 = await _friendsHttpClient.GetFriendsList(prog.Id, 0);
                        foreach (Friend frn in friendsList1)
                        {
                            if (!string.IsNullOrEmpty(frn.Tags))
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

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
                model.FriendTypeListEn = model.FriendTypeListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
                model.FriendTypeListEn = model.FriendTypeListDa;
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
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddFriend(FriendViewModel model)
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
            Friend friendItem = new Friend();
            friendItem.ProgenyId = model.ProgenyId;
            friendItem.Description = model.Description;
            friendItem.FriendAddedDate = DateTime.UtcNow;
            if (model.FriendSince == null)
            {
                model.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
            }
            friendItem.FriendSince = model.FriendSince;
            friendItem.Name = model.Name;
            friendItem.AccessLevel = model.AccessLevel;
            friendItem.Type = model.Type;
            friendItem.Context = model.Context;
            friendItem.Notes = model.Notes;
            friendItem.Author = model.CurrentUser.UserId;
            if (!string.IsNullOrEmpty(model.Tags))
            {
                friendItem.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
            }

            if (model.File != null)
            {
                using (Stream stream = model.File.OpenReadStream())
                {
                    friendItem.PictureLink = await _imageStore.SaveImage(stream, "friends");

                }
            }
            else
            {
                friendItem.PictureLink = Constants.ProfilePictureUrl;
            }

            await _friendsHttpClient.AddFriend(friendItem);

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
            Progeny progeny = await _progenyHttpClient.GetProgeny(model.ProgenyId);
            foreach (UserAccess ua in usersToNotif)
            {
                if (ua.AccessLevel <= friendItem.AccessLevel)
                {
                    UserInfo uaUserInfo = await _userInfosHttpClient.GetUserInfo(ua.UserId);
                    if (uaUserInfo.UserId != "Unknown")
                    {
                        WebNotification notification = new WebNotification();
                        notification.To = uaUserInfo.UserId;
                        notification.From = authorName;
                        notification.Message = "Friend: " + friendItem.Name + "\r\nContext: " + friendItem.Context;
                        notification.DateTime = DateTime.UtcNow;
                        notification.Icon = model.CurrentUser.ProfilePicture;
                        notification.Title = "A new friend was added for " + progeny.NickName;
                        notification.Link = "/Friends?childId=" + progeny.Id;
                        notification.Type = "Notification";

                        notification = await _webNotificationsService.SaveNotification(notification);

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
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            Friend friend = await _friendsHttpClient.GetFriend(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            model.ProgenyId = friend.ProgenyId;
            model.AccessLevel = friend.AccessLevel;
            model.Author = friend.Author;
            if (friend.FriendSince == null)
            {
                friend.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
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
            List<Friend> friendsList1 = await _friendsHttpClient.GetFriendsList(model.ProgenyId, 0);
            foreach (Friend frn in friendsList1)
            {
                if (!string.IsNullOrEmpty(frn.Tags))
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

            if (model.LanguageId == 2)
            {
                model.AccessLevelListEn = model.AccessLevelListDe;
                model.FriendTypeListEn = model.FriendTypeListDe;
            }

            if (model.LanguageId == 3)
            {
                model.AccessLevelListEn = model.AccessLevelListDa;
                model.FriendTypeListEn = model.FriendTypeListDa;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditFriend(FriendViewModel model)
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
                Friend editedFriend = await _friendsHttpClient.GetFriend(model.FriendId);
                editedFriend.AccessLevel = model.AccessLevel;
                editedFriend.Author = model.Author;
                editedFriend.Description = model.Description;
                editedFriend.Name = model.Name;
                editedFriend.FriendId = model.FriendId;
                if (model.FriendSince == null)
                {
                    model.FriendSince = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(model.CurrentUser.Timezone));
                }
                editedFriend.FriendSince = model.FriendSince;
                editedFriend.Type = model.Type;
                editedFriend.Context = model.Context;
                editedFriend.Notes = model.Notes;
                if (!string.IsNullOrEmpty(model.Tags))
                {
                    editedFriend.Tags = model.Tags.TrimEnd(',', ' ').TrimStart(',', ' ');
                }
                if (model.File != null && model.File.Name != string.Empty)
                {
                    string oldPictureLink = model.PictureLink;
                    model.FileName = model.File.FileName;
                    using (Stream stream = model.File.OpenReadStream())
                    {
                        editedFriend.PictureLink = await _imageStore.SaveImage(stream, "friends");
                    }

                    if (!oldPictureLink.ToLower().StartsWith("http"))
                    {
                        await _imageStore.DeleteImage(oldPictureLink, "friends");
                    }
                }

                await _friendsHttpClient.UpdateFriend(editedFriend);
            }
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFriend(int itemId)
        {
            FriendViewModel model = new FriendViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);

            model.Friend = await _friendsHttpClient.GetFriend(itemId);
            Progeny prog = await _progenyHttpClient.GetProgeny(model.Friend.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFriend(FriendViewModel model)
        {
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            Friend friend = await _friendsHttpClient.GetFriend(model.Friend.FriendId);
            Progeny prog = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            if (!prog.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _friendsHttpClient.DeleteFriend(friend.FriendId);

            return RedirectToAction("Index", "Friends");
        }
    }
}