using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using KinaUnaWeb.Models;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class FriendsController(ImageStore imageStore, IFriendsHttpClient friendsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            FriendsListViewModel model = new(baseModel);
            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);

            if (friendsList.Count == 0) return View(model);

            friendsList = [.. friendsList.OrderBy(f => f.FriendSince)];
                
            List<string> tagsList = [];
                
            foreach (Friend friend in friendsList)
            {
                FriendViewModel friendViewModel = new();
                friendViewModel.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);

                friendViewModel.FriendItem.PictureLink = imageStore.UriFor(friendViewModel.FriendItem.PictureLink, "friends");

                if (!string.IsNullOrEmpty(friendViewModel.Tags))
                {
                    List<string> friendTagsList = [.. friendViewModel.Tags.Split(',')];
                    foreach (string tagString in friendTagsList)
                    {
                        if (!tagsList.Contains(tagString.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagString.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }

                model.FriendViewModelsList.Add(friendViewModel);
            }

            model.SetTags(tagsList);
            model.TagFilter = tagFilter;

            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> FriendDetails(int friendId, string tagFilter)
        {
            Friend friend = await friendsHttpClient.GetFriend(friendId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new(baseModel);

            if (friend.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);
            
            
            model.FriendItem.PictureLink = imageStore.UriFor(model.FriendItem.PictureLink, "friends");

            List<string> tagsList = [];
            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
            foreach (Friend friendItem in friendsList)
            {
                if (string.IsNullOrEmpty(friendItem.Tags)) continue;

                List<string> friendItemTags = [.. friendItem.Tags.Split(',')];
                foreach (string tagstring in friendItemTags)
                {
                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                    {
                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                    }
                }
            }

            model.SetTagList(tagsList);
            model.TagFilter = tagFilter;
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddFriend()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FriendViewModel model = new(baseModel);
            
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await viewModelSetupService.GetProgenySelectList(model.CurrentUser);
            model.SetProgenyList();

            List<string> tagsList = [];
            foreach (SelectListItem item in model.ProgenyList)
            {
                if (!int.TryParse(item.Value, out int progenyId)) continue;

                List<Friend> friendsList = await friendsHttpClient.GetFriendsList(progenyId, 0);
                foreach (Friend friend in friendsList)
                {
                    if (string.IsNullOrEmpty(friend.Tags)) continue;

                    List<string> friendTagsList = [.. friend.Tags.Split(',')];
                    foreach (string tagstring in friendTagsList)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }
            
            model.SetTagList(tagsList);
            model.SetAccessLevelList();
            model.SetFriendTypeList();
            
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> AddFriend(FriendViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.FriendItem.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Friend friendItem = model.CreateFriend();
            
            if (model.File != null)
            {
                await using Stream stream = model.File.OpenReadStream();
                friendItem.PictureLink = await imageStore.SaveImage(stream, "friends");
            }
            else
            {
                friendItem.PictureLink = Constants.ProfilePictureUrl;
            }

            _ = await friendsHttpClient.AddFriend(friendItem);

            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> EditFriend(int itemId)
        {
            Friend friend = await friendsHttpClient.GetFriend(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            friend.PictureLink = imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);

            model.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);
            
            List<string> tagsList = [];
            List<Friend> friendsList1 = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId, 0);
            foreach (Friend friendItem in friendsList1)
            {
                if (string.IsNullOrEmpty(friendItem.Tags)) continue;

                List<string> friendTagsList = [.. friendItem.Tags.Split(',')];
                foreach (string tagstring in friendTagsList)
                {
                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                    {
                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                    }
                }
            }
            
            model.SetTagList(tagsList);
            model.SetAccessLevelList();
            model.SetFriendTypeList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        public async Task<IActionResult> EditFriend(FriendViewModel model)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.FriendItem.ProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Friend editedFriend = model.CreateFriend();

            if (model.File != null && model.File.Name != string.Empty)
            {
                Friend originalFriend = await friendsHttpClient.GetFriend(model.FriendItem.FriendId);
                model.FileName = model.File.FileName;
                await using (Stream stream = model.File.OpenReadStream())
                {
                    editedFriend.PictureLink = await imageStore.SaveImage(stream, "friends");
                }

                await imageStore.DeleteImage(originalFriend.PictureLink, "friends");
            }
            else
            {
                editedFriend.PictureLink = Constants.KeepExistingLink;
            }

            _ = await friendsHttpClient.UpdateFriend(editedFriend);

            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFriend(int itemId)
        {
            Friend friend = await friendsHttpClient.GetFriend(itemId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            friend.PictureLink = imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);
            model.FriendItem = friend;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFriend(FriendViewModel model)
        {
            Friend friend = await friendsHttpClient.GetFriend(model.FriendItem.FriendId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            _ = await friendsHttpClient.DeleteFriend(friend.FriendId);

            return RedirectToAction("Index", "Friends");
        }
    }
}