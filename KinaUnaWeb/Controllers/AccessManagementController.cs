using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUnaWeb.Models.FamiliesViewModels;
using KinaUnaWeb.Models.ProgeniesViewModels;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Access Management Controller. Handles UserAccess to Progeny.
    /// </summary>
    /// <param name="userInfosHttpClient"></param>
    /// <param name="userAccessHttpClient"></param>
    public class AccessManagementController(IUserInfosHttpClient userInfosHttpClient, IUserAccessHttpClient userAccessHttpClient,
        IProgenyHttpClient progenyHttpClient, IFamiliesHttpClient familiesHttpClient, IUserGroupsHttpClient userGroupsHttpClient, ITimelineHttpClient timelineHttpClient)
        : Controller
    {
        /// <summary>
        /// Index page for the AccessManagementController. Redirects to the FamilyController Index page.
        /// </summary>
        /// <returns>Redirect to the Family Index page.</returns>
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Families");
        }
        
        [HttpPost]
        public async Task<IActionResult> ItemPermissionsModal([FromBody] TimeLineItem timelineItem)
        {
            int.TryParse(timelineItem.ItemId, out int itemId);
            KinaUnaTypes.TimeLineType itemType = (KinaUnaTypes.TimeLineType)timelineItem.ItemType;

            if (timelineItem.ProgenyId > 0)
            {
                ProgenyItemPermissionsViewModel model = new()
                {
                    LanguageId = Request.GetLanguageIdFromCookie(),
                    ItemId = itemId,
                    ItemType = itemType,
                    ProgenyId = timelineItem.ProgenyId,
                    UserGroupsList = await userGroupsHttpClient.GetUserGroupsForProgeny(timelineItem.ProgenyId),
                    ProgenyPermissionsList = await progenyHttpClient.GetProgenyPermissionsList(timelineItem.ProgenyId)
                };
                model.SetInitialPermissionLevelsSelectListItems();
                
                if (model.ItemId > 0)
                {
                    model.TimeLineItem = await timelineHttpClient.GetTimeLineItem(timelineItem.ItemId, timelineItem.ItemType);

                    if (model.IsUserAccessManager && model.TimeLineItem?.ProgenyId == model.ProgenyId)
                    {
                        model.ItemPermissionsList = await userAccessHttpClient.GetTimelineItemPermissionsList(itemType, itemId);
                        int permissionType = 0;
                        foreach (TimelineItemPermission permission in model.ItemPermissionsList)
                        {
                            if (permission.InheritPermissions)
                            {
                                permissionType = 0;
                            }
                            else
                                permissionType = permission.PermissionLevel switch
                                {
                                    PermissionLevel.CreatorOnly => 1,
                                    PermissionLevel.Private => 2,
                                    _ => 3
                                };

                            if (string.IsNullOrWhiteSpace(permission.UserId) || model.UserList.Exists(u => u.UserId == permission.UserId))
                            {
                                continue;
                            }

                            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(permission.UserId);
                            model.UserList.Add(userInfo);
                        }
                        model.SetPermissionTypeSelectListItems(permissionType);
                    }
                    else
                    {
                        model.SetPermissionTypeSelectListItems(0);
                    }
                }
                else
                {
                    model.SetPermissionTypeSelectListItems(0);
                }
                
                foreach (UserGroup group in model.UserGroupsList)
                {
                    foreach (UserGroupMember member in group.Members)
                    {
                        if (string.IsNullOrWhiteSpace(member.UserId) || model.UserList.Exists(u => u.UserId == member.UserId))
                        {
                            continue;
                        }
                        UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(member.UserId);
                        model.UserList.Add(userInfo);
                    }
                }
                
                return PartialView("_ProgenyItemPermissionsPartial", model);
            }
            else
            {
                FamilyItemPermissionsViewModel model = new()
                {
                    LanguageId = Request.GetLanguageIdFromCookie(),
                    ItemId = itemId,
                    ItemType = itemType,
                    FamilyId = timelineItem.FamilyId,
                    UserGroupsList = await userGroupsHttpClient.GetUserGroupsForFamily(timelineItem.FamilyId),
                    FamilyPermissionsList = await familiesHttpClient.GetFamilyPermissionsList(timelineItem.FamilyId)
                };
                model.SetInitialPermissionLevelsSelectListItems();

                if (model.ItemId > 0)
                {
                    model.TimeLineItem = await timelineHttpClient.GetTimeLineItem(timelineItem.ItemId, timelineItem.ItemType);
                    if (model.IsUserAccessManager && model.TimeLineItem?.FamilyId == model.FamilyId)
                    {
                        model.ItemPermissionsList = await userAccessHttpClient.GetTimelineItemPermissionsList(itemType, itemId);
                        int permissionType = 0;
                        foreach (TimelineItemPermission permission in model.ItemPermissionsList)
                        {
                            if (permission.InheritPermissions)
                            {
                                permissionType = 0;
                            }
                            else
                                permissionType = permission.PermissionLevel switch
                                {
                                    PermissionLevel.CreatorOnly => 1,
                                    PermissionLevel.Private => 2,
                                    _ => 3
                                };

                            if (string.IsNullOrWhiteSpace(permission.UserId) || model.UserList.Exists(u => u.UserId == permission.UserId))
                            {
                                continue;
                            }

                            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(permission.UserId);
                            model.UserList.Add(userInfo);
                        }

                        model.SetPermissionTypeSelectListItems(permissionType);
                    }
                    else
                    {
                        model.SetPermissionTypeSelectListItems(0);
                    }
                }
                else
                {
                    model.SetPermissionTypeSelectListItems(0);
                }

                if (model.ItemId > 0)
                {
                    if (model.IsUserAccessManager && model.TimeLineItem?.FamilyId == model.FamilyId)
                    {
                        model.ItemPermissionsList = await userAccessHttpClient.GetTimelineItemPermissionsList(itemType, itemId);
                        foreach (TimelineItemPermission permission in model.ItemPermissionsList)
                        {
                            if (string.IsNullOrWhiteSpace(permission.UserId) || model.UserList.Exists(u => u.UserId == permission.UserId))
                            {
                                continue;
                            }
                            UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(permission.UserId);
                            model.UserList.Add(userInfo);
                        }
                    }
                }
                
                foreach (UserGroup group in model.UserGroupsList)
                {
                    foreach (UserGroupMember member in group.Members)
                    {
                        if (string.IsNullOrWhiteSpace(member.UserId) || model.UserList.Exists(u => u.UserId == member.UserId))
                        {
                            continue;
                        }

                        UserInfo userInfo = await userInfosHttpClient.GetUserInfoByUserId(member.UserId);
                        model.UserList.Add(userInfo);
                    }
                }

                return PartialView("_FamilyItemPermissionsPartial", model);
            }
        }
    }
}