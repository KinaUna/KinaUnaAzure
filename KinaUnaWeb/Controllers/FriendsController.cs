﻿using KinaUnaWeb.Models.ItemViewModels;
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
using KinaUnaWeb.Models;

namespace KinaUnaWeb.Controllers
{
    public class FriendsController : Controller
    {
        private readonly IFriendsHttpClient _friendsHttpClient;
        private readonly ImageStore _imageStore;
        private readonly IViewModelSetupService _viewModelSetupService;
        private readonly INotificationsService _notificationsService;

        public FriendsController(ImageStore imageStore, IFriendsHttpClient friendsHttpClient, IViewModelSetupService viewModelSetupService, INotificationsService notificationsService)
        {
            _imageStore = imageStore;
            _friendsHttpClient = friendsHttpClient;
            _viewModelSetupService = viewModelSetupService;
            _notificationsService = notificationsService;
        }
        
        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0, string tagFilter = "")
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), childId);
            FriendsListViewModel model = new FriendsListViewModel(baseModel);
            List<Friend> friendsList = await _friendsHttpClient.GetFriendsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
            
            if (friendsList.Count != 0)
            {
                friendsList = friendsList.OrderBy(f => f.FriendSince).ToList();
                
                List<string> tagsList = new List<string>();
                
                foreach (Friend friend in friendsList)
                {
                    FriendViewModel friendViewModel = new FriendViewModel();
                    friendViewModel.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);

                    friendViewModel.PictureLink = _imageStore.UriFor(friendViewModel.PictureLink, "friends");

                    if (!string.IsNullOrEmpty(friendViewModel.Tags))
                    {
                        List<string> friendTagsList = friendViewModel.Tags.Split(',').ToList();
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

                string allTagsString = "";
                foreach (string tagString in tagsList)
                {
                    allTagsString = allTagsString + tagString + ",";
                }
                model.Tags = allTagsString.TrimEnd(',');
                model.TagFilter = tagFilter;
            }
            else
            {
                FriendViewModel friendViewModel = new FriendViewModel();
                friendViewModel.CurrentProgenyId = model.CurrentProgenyId;
                friendViewModel.Name = "No friends found.";
                friendViewModel.FriendAddedDate = DateTime.UtcNow;
                friendViewModel.FriendSince = DateTime.UtcNow;
                friendViewModel.Description = "The friends list is empty.";
                friendViewModel.IsCurrentUserProgenyAdmin = model.IsCurrentUserProgenyAdmin;
                model.FriendViewModelsList.Add(friendViewModel);
            }

            return View(model);

        }

        [AllowAnonymous]
        public async Task<IActionResult> FriendDetails(int friendId, string tagFilter)
        {
            Friend friend = await _friendsHttpClient.GetFriend(friendId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new FriendViewModel(baseModel);

            if (friend.AccessLevel < model.CurrentAccessLevel)
            {
                return RedirectToAction("Index");
            }

            model.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);
            
            
            model.PictureLink = _imageStore.UriFor(model.PictureLink, "friends");

            List<string> tagsList = new List<string>();
            List<Friend> friendsList = await _friendsHttpClient.GetFriendsList(model.CurrentProgenyId, model.CurrentAccessLevel, tagFilter);
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

            model.TagsList = tagItems;
            model.TagFilter = tagFilter;
            
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AddFriend()
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FriendViewModel model = new FriendViewModel(baseModel);
            
            
            if (model.CurrentUser == null)
            {
                return RedirectToAction("Index");
            }

            model.ProgenyList = await _viewModelSetupService.GetProgenySelectList(model.CurrentUser);

            List<string> tagsList = new List<string>();
            foreach (SelectListItem item in model.ProgenyList)
            {
                if (int.TryParse(item.Value, out int progenyId))
                {
                    List<Friend> friendsList = await _friendsHttpClient.GetFriendsList(progenyId, 0);
                    foreach (Friend friend in friendsList)
                    {
                        if (!string.IsNullOrEmpty(friend.Tags))
                        {
                            List<string> friendTagsList = friend.Tags.Split(',').ToList();
                            foreach (string tagstring in friendTagsList)
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
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
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
                friendItem.PictureLink = await _imageStore.SaveImage(stream, "friends");
            }
            else
            {
                friendItem.PictureLink = Constants.ProfilePictureUrl;
            }

            friendItem = await _friendsHttpClient.AddFriend(friendItem);

            await _notificationsService.SendFriendNotification(friendItem, model.CurrentUser);
            
            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> EditFriend(int itemId)
        {
            Friend friend = await _friendsHttpClient.GetFriend(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new FriendViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            friend.PictureLink = _imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);

            model.SetPropertiesFromFriendItem(friend, model.IsCurrentUserProgenyAdmin);
            
            List<string> tagsList = new List<string>();
            List<Friend> friendsList1 = await _friendsHttpClient.GetFriendsList(model.CurrentProgenyId, 0);
            foreach (Friend friendItem in friendsList1)
            {
                if (!string.IsNullOrEmpty(friendItem.Tags))
                {
                    List<string> friendTagsList = friendItem.Tags.Split(',').ToList();
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
        public async Task<IActionResult> EditFriend(FriendViewModel model)
        {
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), model.CurrentProgenyId);
            model.SetBaseProperties(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            Friend editedFriend = model.CreateFriend();

            if (model.File != null && model.File.Name != string.Empty)
            {
                Friend originalFriend = await _friendsHttpClient.GetFriend(model.FriendId);
                model.FileName = model.File.FileName;
                await using (Stream stream = model.File.OpenReadStream())
                {
                    editedFriend.PictureLink = await _imageStore.SaveImage(stream, "friends");
                }

                await _imageStore.DeleteImage(originalFriend.PictureLink, "friends");
            }
            else
            {
                editedFriend.PictureLink = Constants.KeepExistingLink;
            }

            await _friendsHttpClient.UpdateFriend(editedFriend);

            return RedirectToAction("Index", "Friends");
        }

        [HttpGet]
        public async Task<IActionResult> DeleteFriend(int itemId)
        {
            Friend friend = await _friendsHttpClient.GetFriend(itemId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            FriendViewModel model = new FriendViewModel(baseModel);
            
            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            friend.PictureLink = _imageStore.UriFor(friend.PictureLink, BlobContainers.Friends);
            model.Friend = friend;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFriend(FriendViewModel model)
        {
            Friend friend = await _friendsHttpClient.GetFriend(model.Friend.FriendId);
            BaseItemsViewModel baseModel = await _viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), friend.ProgenyId);
            model.SetBaseProperties(baseModel);

            if (!model.CurrentProgeny.IsInAdminList(model.CurrentUser.UserEmail))
            {
                // Todo: Show no access info.
                return RedirectToAction("Index");
            }

            await _friendsHttpClient.DeleteFriend(friend.FriendId);

            return RedirectToAction("Index", "Friends");
        }
    }
}