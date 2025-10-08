using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamiliesViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class FamiliesController(IFamiliesHttpClient familiesHttpClient, IViewModelSetupService viewModelSetupService,
        IProgenyHttpClient progenyHttpClient, IUserGroupsHttpClient userGroupsHttpClient, IUserInfosHttpClient userInfosHttpClient, ImageStore imageStore) : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamiliesViewModel model = new(baseModel);

            return View(model);
        }

        public async Task<IActionResult> FamiliesList()
        {
            List<Family> families = await familiesHttpClient.GetMyFamilies();
            if (families.Count != 0)
            {
                return Json(families);
            }

            Family family = new();
            families.Add(family);
            return Json(families);
        }

        public async Task<IActionResult> FamilyElement(int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, familyId, false);
            FamilyElementViewModel model = new(baseModel)
            {
                Family = await familiesHttpClient.GetFamily(familyId)
            };

            if (model.Family.FamilyMembers.Count <= 0)
            {
                return PartialView("_FamilyElementPartial", model);
            }
            
            foreach (FamilyMember familyMember in model.Family.FamilyMembers)
            {
                if (familyMember.ProgenyId > 0)
                {
                    familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(familyMember.UserId))
                    {
                        familyMember.UserInfo = await userInfosHttpClient.GetUserInfoByUserId(familyMember.UserId);
                    }
                }
            }

            return PartialView("_FamilyElementPartial", model);
        }

        public async Task<IActionResult> FamilyDetails(int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, familyId, false);
            FamilyDetailsViewModel model = new(baseModel)
            {
                Family = await familiesHttpClient.GetFamily(familyId)
            };
            
            if (model.Family.IsInAdminList(model.CurrentUser.UserEmail))
            {
                model.IsCurrentUserFamilyAdmin = true;
            }

            return PartialView("_FamilyDetailsPartial", model);
        }

        public async Task<IActionResult> AddFamily()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamilyDetailsViewModel model = new(baseModel);

            Family family = new();
            model.Family = family;

            return PartialView("_AddFamilyPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFamily([FromForm] Family family)
        {
            Family addedFamily = await familiesHttpClient.AddFamily(family);

            return Json(addedFamily);
        }

        public async Task<IActionResult> EditFamily(int familyId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, familyId, false);
            FamilyDetailsViewModel model = new(baseModel)
            {
                Family = await familiesHttpClient.GetFamily(familyId)
            };

            return PartialView("_EditFamilyPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFamily([FromForm] Family family)
        {
            Family updatedFamily = await familiesHttpClient.UpdateFamily(family);

            return Json(updatedFamily);
        }

        public async Task<IActionResult> Members()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0,0, false);
            FamiliesViewModel model = new(baseModel)
            {
                Families = await familiesHttpClient.GetMyFamilies()
            };

            if (model.Families.Count == 0)
            {
                Family family = new();
                model.Families.Add(family);
            }
            
            return View(model);
        }

        public async Task<IActionResult> FamilyMemberElement(int familyMemberId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = await familiesHttpClient.GetFamilyMember(familyMemberId)
            };

            Family family = await familiesHttpClient.GetFamily(model.FamilyMember.FamilyId);
            if (family.IsInAdminList(model.CurrentUser.UserEmail))
            {
                model.IsCurrentUserFamilyAdmin = true;
            }

            return PartialView("_FamilyMemberElementPartial", model);
        }

        public async Task<IActionResult> FamilyMemberDetails(int familyMemberId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = await familiesHttpClient.GetFamilyMember(familyMemberId)
            };

            Family family = await familiesHttpClient.GetFamily(model.FamilyMember.FamilyId);
            if (family.IsInAdminList(model.CurrentUser.UserEmail))
            {
                model.IsCurrentUserFamilyAdmin = true;
            }

            return PartialView("_FamilyMemberDetailsPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<FamilyMember> AddFamilyMember([FromForm] FamilyMember familyMember)
        {
            FamilyMember addedFamilyMember = await familiesHttpClient.AddFamilyMember(familyMember);
            return addedFamilyMember;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<FamilyMember> UpdateFamilyMember([FromForm] FamilyMember familyMember)
        {
            FamilyMember updatedFamilyMember = await familiesHttpClient.UpdateFamilyMember(familyMember);
            return updatedFamilyMember;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFamilyMember(int familyMemberId)
        {
            bool result = await familiesHttpClient.DeleteFamilyMember(familyMemberId);
            return Json(result);
        }
        
        public async Task<IActionResult> UserAccess()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            UserGroupsViewModel model = new(baseModel)
            {
                Families = await familiesHttpClient.GetMyFamilies()
            };
            model.Progenies = await progenyHttpClient.GetProgenyAdminList(model.CurrentUser.UserEmail);

            foreach (Family family in model.Families)
            {
                List<UserGroup> userGroupsList = await userGroupsHttpClient.GetUserGroupsForFamily(family.FamilyId);
                model.UserGroups.AddRange(userGroupsList);
            }
            foreach (Progeny progeny in model.Progenies)
            {
                List<UserGroup> userGroupsList = await userGroupsHttpClient.GetUserGroupsForProgeny(progeny.Id);
                model.UserGroups.AddRange(userGroupsList);
            }

            return View(model);
        }

        /// <summary>
        /// Profile picture for a family. If the family has no picture or the user has no access to the picture, a default image is returned.
        /// Images are stored in Azure Blob Storage, this provides a static URL for profile pictures.
        /// </summary>
        /// <param name="id">The Id of the family to get the profile picture for.</param>
        /// <returns>FileContentResult with the image data.</returns>
        [AllowAnonymous]
        public async Task<FileContentResult> ProfilePicture(int id)
        {
            Family family = await familiesHttpClient.GetFamily(id);
            if (family == null || family.FamilyId == 0 || string.IsNullOrEmpty(family.PictureLink))
            {
                MemoryStream fileContentNoAccess = await imageStore.GetStream("868b62e2-6978-41a1-97dc-1cc1116f65a6.jpg");
                byte[] fileContentBytesNoAccess = fileContentNoAccess.ToArray();
                return new FileContentResult(fileContentBytesNoAccess, "image/jpeg");
            }

            MemoryStream fileContent = await imageStore.GetStream(family.PictureLink, BlobContainers.Family);
            byte[] fileContentBytes = fileContent.ToArray();

            return new FileContentResult(fileContentBytes, family.GetPictureFileContentType());
        }
    }
}
