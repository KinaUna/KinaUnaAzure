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

namespace KinaUnaWeb.Controllers
{
    public class FriendsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IFriendsHttpClient _friendsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        private readonly ImageStore _imageStore;
        
        public FriendsController(IProgenyHttpClient progenyHttpClient, ImageStore imageStore, IUserInfosHttpClient userInfosHttpClient, IFriendsHttpClient friendsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
            _userInfosHttpClient = userInfosHttpClient;
            _friendsHttpClient = friendsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }
        // GET: /<controller>/
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
    }
}