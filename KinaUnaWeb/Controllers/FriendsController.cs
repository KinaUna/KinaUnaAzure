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
    public class FriendsController(ImageStore imageStore,
        IFriendsHttpClient friendsHttpClient,
        IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient) : Controller
    {
        /// <summary>
        /// Index page for Friends. Shows a list of all friends for the current Progeny.
        /// </summary>
        /// <param name="childId">The Id of the Progeny to show Friends for.</param>
        /// <param name="tagFilter">Filter the list of Friends by Tags. Empty string includes all Friends.</param>
        /// <param name="sort">Sort order. 0 = oldest first, 1 = newest first.</param>
        /// <param name="sortBy">Property to sort Contacts by. 0 = FriendsSince, 1 = Name, 2 = FirstName, 3 = LastName.</param>
        /// <param name="sortTags">Sort the list of all tags. 0 = no sorting, 1 = sort alphabetically.</param>
        /// <param name="friendId">The FriendId of the Friend to show details for. If 0, no Friend pop-up is shown.</param>
        /// <returns>View with FriendsListViewModel.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "", int sort = 0, int sortBy = 0, int sortTags = 0, int friendId = 0)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            FriendsListViewModel model = new(baseModel);
            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId, tagFilter);

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

            model.FriendId = friendId;

            return View(model);

        }

        /// <summary>
        /// Shows details for a single friend.
        /// </summary>
        /// <param name="friendId">The FriendId of the Friend to display.</param>
        /// <param name="tagFilter">Filter tags. Empty string includes all tags.</param>
        /// <param name="partialView">Return partial view, for fetching HTML inline to show in a modal/popup.</param>
        /// <returns>View or PartialView with FriendViewModel.</returns>
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
            List<Friend> friendsList = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId, tagFilter);
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

        /// <summary>
        /// Gets a partial view with a Friend element, for friends lists to fetch HTML for each contact.
        /// </summary>
        /// <param name="parameters">FriendItemParameters object with the Contact details.</param>
        /// <returns>PartialView with FriendViewModel.</returns>
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
                friendItemResponse.FriendItem.Progeny = await progenyHttpClient.GetProgeny(friendItemResponse.FriendItem.ProgenyId);
            }


            return PartialView("_FriendElementPartial", friendItemResponse);

        }

        /// <summary>
        /// HttpPost endpoint for fetching a list of Friends.
        /// </summary>
        /// <param name="parameters">FriendsPageParameters object.</param>
        /// <returns>Json of FriendsPageResponse</returns>
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
            
            List<Friend> friendsList = []; // await friendsHttpClient.GetFriendsList(parameters.ProgenyId, baseModel.CurrentAccessLevel, parameters.TagFilter);
            
            foreach (int progenyId in parameters.Progenies)
            {
                List<Friend> friends = await friendsHttpClient.GetFriendsList(progenyId, parameters.TagFilter);
                friendsList.AddRange(friends);
            }

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

        /// <summary>
        /// Gets the image file for a Contact's profile picture.
        /// Checks if the user has access to the Contact. If not, returns a default image.
        /// </summary>
        /// <param name="id">The ContactId of the Contact to get a profile picture for.</param>
        /// <returns>FileContentResult with the image file.</returns>
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

        /// <summary>
        /// Page for adding a new Friend.
        /// </summary>
        /// <returns>View with FriendViewModel.</returns>
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

                List<Friend> friendsList = await friendsHttpClient.GetFriendsList(progenyId);
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

        /// <summary>
        /// HttpPost endpoint for adding a new Friend.
        /// </summary>
        /// <param name="model">FriendViewModel with the Friend to add.</param>
        /// <returns>Redirects to Friends/Index page.</returns>
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


        /// <summary>
        /// Page for editing a Friend.
        /// </summary>
        /// <param name="itemId">The FriendId of the Friend to edit.</param>
        /// <returns>View with FriendViewModel.</returns>
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
            List<Friend> friendsList1 = await friendsHttpClient.GetFriendsList(model.CurrentProgenyId);
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

        /// <summary>
        /// HttpPost endpoint for editing a Friend.
        /// </summary>
        /// <param name="model">FriendViewModel with the updated properties.</param>
        /// <returns>Redirects to Friends/Index page.</returns>
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

        /// <summary>
        /// Page for deleting a Friend.
        /// </summary>
        /// <param name="itemId">The FriendId of the Friend to delete.</param>
        /// <returns>View with FriendViewModel.</returns>
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

        /// <summary>
        /// HttpPost endpoint for deleting a Friend.
        /// </summary>
        /// <param name="model">FriendViewModel with the properties of the Friend to delete.</param>
        /// <returns>Redirects to Friends/Index.</returns>
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