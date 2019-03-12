using KinaUnaWeb.Data;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.IDP;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class FriendsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly ImageStore _imageStore;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public FriendsController(WebDbContext context, IProgenyHttpClient progenyHttpClient, ImageStore imageStore)
        {
            _context = context;
            _progenyHttpClient = progenyHttpClient;
            _imageStore = imageStore;
        }
        // GET: /<controller>/
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }

            List<FriendViewModel> model = new List<FriendViewModel>();
            
            List<string> tagsList = new List<string>();
            List<Friend> friendsList = _context.FriendsDb.AsNoTracking().Where(w => w.ProgenyId == _progId).ToList();
            if (!string.IsNullOrEmpty(tagFilter))
            {
                friendsList = _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == _progId && f.Tags.Contains(tagFilter)).ToList();
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
                    friendViewModel.IsAdmin = _userIsProgenyAdmin;
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
                        model.Add(friendViewModel);
                    }



                }

                string tags = "";
                foreach (string tstr in tagsList)
                {
                    tags = tags + tstr + ",";
                }
                ViewBag.Tags = tags.TrimEnd(',');

            }
            else
            {
                FriendViewModel friendViewModel = new FriendViewModel();
                friendViewModel.ProgenyId = _progId;
                friendViewModel.Name = "No friends found.";
                friendViewModel.FriendAddedDate = DateTime.UtcNow;
                friendViewModel.FriendSince = DateTime.UtcNow;
                friendViewModel.Description = "The friends list is empty.";
                friendViewModel.IsAdmin = _userIsProgenyAdmin;
                model.Add(friendViewModel);
            }

            model[0].Progeny = progeny;
            ViewBag.TagFilter = tagFilter;
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> FriendDetails(int friendId, string tagFilter)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            Friend friend = await _context.FriendsDb.AsNoTracking().SingleAsync(f => f.FriendId == friendId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }


            FriendViewModel model = new FriendViewModel();
            
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
            var friendsList = _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == model.ProgenyId).ToList();
            foreach (Friend frn in friendsList)
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
            ViewBag.TagFilter = tagFilter;
            if (model.AccessLevel < userAccessLevel)
            {
                return RedirectToAction("Index");
            }
            model.IsAdmin = _userIsProgenyAdmin;
            return View(model);
        }
    }
}