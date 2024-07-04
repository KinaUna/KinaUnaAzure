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
using KinaUnaWeb.Models.TypeScriptModels.Friends;
using KinaUnaWeb.Services.HttpClients;

namespace KinaUnaWeb.Controllers
{
    public class FriendsController(ImageStore imageStore, IFriendsHttpClient friendsHttpClient, IViewModelSetupService viewModelSetupService) : Controller
    {
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "", int sort = 0, int sortBy = 0, int sortTags = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            FriendsListViewModel model = new(baseModel);
            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);

            model.TagFilter = tagFilter;

            model.FriendsPageParameters = new()
            {
                LanguageId = model.LanguageId,
                TagFilter = tagFilter,
                TotalItems = friendsList.Count,
                ProgenyId = model.CurrentProgenyId,
                Sort = sort,
                SortBy = sortBy,
                SortTags = sortTags
            };

            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> ViewFriend(int friendId, string tagFilter = "", bool partialView = false)
        {
            Friend friend = await friendsHttpClient.GetFriend(friendId);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new(baseModel);

            if (friend.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);
            
            
            model.FriendItem.PictureLink = model.FriendItem.GetProfilePictureUrl();

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
            
            model.FriendItem.Progeny = model.CurrentProgeny;
            model.FriendItem.Progeny.PictureLink = model.FriendItem.Progeny.GetProfilePictureUrl();

            if (partialView)
            {
                return PartialView("_FriendDetailsPartial", model);
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> FriendElement([FromBody] FriendItemParameters parameters)
        {
            parameters ??= new FriendItemParameters();

            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            FriendViewModel friendItemResponse = new()
            {
                LanguageId = parameters.LanguageId
            };

            if (parameters.FriendId == 0)
            {
                friendItemResponse.FriendItem = new Friend { FriendId = 0 };
            }
            else
            {
                friendItemResponse.FriendItem = await friendsHttpClient.GetFriend(parameters.FriendId);
                friendItemResponse.FriendItem.PictureLink = friendItemResponse.FriendItem.GetProfilePictureUrl();
                BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), friendItemResponse.FriendItem.ProgenyId);
                friendItemResponse.IsCurrentUserProgenyAdmin = baseModel.IsCurrentUserProgenyAdmin;
            }


            return PartialView("_FriendElementPartial", friendItemResponse);

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> FriendsList([FromBody] FriendsPageParameters parameters)
        {
            parameters ??= new FriendsPageParameters();
            if (parameters.LanguageId == 0)
            {
                parameters.LanguageId = Request.GetLanguageIdFromCookie();
            }

            if (parameters.CurrentPageNumber < 1)
            {
                parameters.CurrentPageNumber = 1;
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(parameters.LanguageId, User.GetEmail(), parameters.ProgenyId);
            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(parameters.ProgenyId, baseModel.CurrentAccessLevel, parameters.TagFilter);

            List<string> tagsList = [];
            
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

            if (parameters.SortTags == 1)
            {
                tagsList = [.. tagsList.OrderBy(t => t)];
            }

            if (parameters.SortBy == 0)
            {
                friendsList = [.. friendsList.OrderBy(f => f.FriendSince)];
            }
            else
            {
                friendsList = [.. friendsList.OrderBy(f => f.Name)];
            }

            if (parameters.Sort == 1)
            {
                friendsList.Reverse();
            }

            List<int> friendsIdList = friendsList.Select(f => f.FriendId).ToList();

            return Json(new FriendsPageResponse()
            {
                FriendsList = friendsIdList,
                PageNumber = parameters.CurrentPageNumber,
                TotalItems = friendsList.Count,
                TagsList = tagsList
            });
        }

        [AllowAnonymous]
        public async Task<FileContentResult> ProfilePicture(int id)
        {
            Friend friend = await friendsHttpClient.GetFriend(id);
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            ContactViewModel model = new(baseModel);

            if (string.IsNullOrEmpty(friend.PictureLink) || friend.AccessLevel < model.CurrentAccessLevel)
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("868b62e2-6978-41a1-97dc-1cc1116f65a6.jpg");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/jpeg");
            }

            MemoryStream fileContent = await imageStore.GetStream(friend.PictureLink, BlobContainers.Friends);
            byte[] fileContentBytes = fileContent.ToArray();

            return new FileContentResult(fileContentBytes, friend.GetPictureFileContentType());
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
                string fileFormat = Path.GetExtension(model.File.FileName);
                friendItem.PictureLink = await imageStore.SaveImage(stream, "friends", fileFormat);
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

            friend.PictureLink = friend.GetProfilePictureUrl();

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
            Friend originalFriend = await friendsHttpClient.GetFriend(model.FriendItem.FriendId);

            if (model.File != null && model.File.Name != string.Empty)
            {
                
                model.FileName = model.File.FileName;
                await using Stream stream = model.File.OpenReadStream();
                string fileFormat = Path.GetExtension(model.File.FileName);
                editedFriend.PictureLink = await imageStore.SaveImage(stream, "friends", fileFormat);
            }
            else
            {
                editedFriend.PictureLink = originalFriend.PictureLink;
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

            friend.PictureLink = friend.GetProfilePictureUrl();
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