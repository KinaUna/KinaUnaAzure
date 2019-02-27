using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class FriendsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly ImageStore _imageStore;
        private int _progId = 2;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = "testuser@niviaq.com";

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
                _progId = 2;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

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
                userAccessLevel = 0;
            }

            List<FriendViewModel> model = new List<FriendViewModel>();
            
            List<string> tagsList = new List<string>();
            List<Friend> friendsList = _context.FriendsDb.Where(w => w.ProgenyId == _progId).ToList();
            if (!string.IsNullOrEmpty(tagFilter))
            {
                friendsList = _context.FriendsDb.Where(f => f.ProgenyId == _progId && f.Tags.Contains(tagFilter)).ToList();
            }

            friendsList = friendsList.OrderBy(f => f.FriendSince).ToList();
            if (friendsList.Count != 0)
            {
                foreach (Friend f in friendsList)
                {
                    FriendViewModel fIvm = new FriendViewModel();
                    fIvm.ProgenyId = f.ProgenyId;
                    fIvm.AccessLevel = f.AccessLevel;
                    fIvm.FriendAddedDate = f.FriendAddedDate;
                    fIvm.FriendSince = f.FriendSince;
                    fIvm.Name = f.Name;
                    fIvm.Description = f.Description;
                    fIvm.IsAdmin = _userIsProgenyAdmin;
                    fIvm.FriendId = f.FriendId;
                    fIvm.PictureLink = f.PictureLink;
                    fIvm.Type = f.Type;
                    fIvm.Context = f.Context;
                    fIvm.Notes = f.Notes;
                    fIvm.Tags = f.Tags;
                    if (!String.IsNullOrEmpty(fIvm.Tags))
                    {
                        List<string> pvmTags = fIvm.Tags.Split(',').ToList();
                        foreach (string tagstring in pvmTags)
                        {
                            if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                            {
                                tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                            }
                        }
                    }
                    if (!fIvm.PictureLink.StartsWith("https://"))
                    {
                        fIvm.PictureLink = _imageStore.UriFor(fIvm.PictureLink, "friends");
                    }

                    if (fIvm.AccessLevel >= userAccessLevel)
                    {
                        model.Add(fIvm);
                    }



                }

                string tList = "";
                foreach (string tstr in tagsList)
                {
                    tList = tList + tstr + ",";
                }
                ViewBag.Tags = tList.TrimEnd(',');

            }
            else
            {
                FriendViewModel f = new FriendViewModel();
                f.ProgenyId = _progId;
                f.Name = "No friends found.";
                f.FriendAddedDate = DateTime.UtcNow;
                f.FriendSince = DateTime.UtcNow;
                f.Description = "The friends list is empty.";
                f.IsAdmin = _userIsProgenyAdmin;
                model.Add(f);
            }

            model[0].Progeny = progeny;
            ViewBag.TagFilter = tagFilter;
            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> FriendDetails(int friendId, string tagFilter)
        {
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            Friend friend = await _context.FriendsDb.SingleAsync(f => f.FriendId == friendId);
            Progeny progeny = await _progenyHttpClient.GetProgeny(friend.ProgenyId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

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
                userAccessLevel = 0;
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
            var friendsList1 = _context.FriendsDb.Where(f => f.ProgenyId == model.ProgenyId).ToList();
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