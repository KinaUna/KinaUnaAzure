using KinaUna.Data.Extensions;
using KinaUna.Data.Models.AccessManagement;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamiliesViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KinaUna.Data;

namespace KinaUnaWeb.Controllers
{
    [Authorize]
    public class FamilyMembersController(
        IFamiliesHttpClient familiesHttpClient,
        IProgenyHttpClient progenyHttpClient,
        IViewModelSetupService viewModelSetupService,
        IImageStore imageStore) : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
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

        [HttpGet]
        [Route("[controller]/[action]/{familyMemberId:int}")]
        public async Task<IActionResult> FamilyMemberElement(int familyMemberId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = await familiesHttpClient.GetFamilyMember(familyMemberId)
            };

            model.Family = await familiesHttpClient.GetFamily(model.FamilyMember.FamilyId);
            if (model.Family == null || model.Family.FamilyId == 0 || model.FamilyMember == null || model.FamilyMember.FamilyMemberId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            model.FamilyMember.PermissionLevel = model.FamilyMember.Progeny.ProgenyPerMission.PermissionLevel;

            return PartialView("_FamilyMemberElementPartial", model);
        }

        [HttpGet]
        [Route("[controller]/[action]/{familyMemberId:int}")]
        public async Task<IActionResult> FamilyMemberDetails(int familyMemberId)
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, 0, false);
            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = await familiesHttpClient.GetFamilyMember(familyMemberId)
            };

            model.Family = await familiesHttpClient.GetFamily(model.FamilyMember.FamilyId);
            if (model.Family == null || model.Family.FamilyId == 0 || model.FamilyMember == null || model.FamilyMember.FamilyMemberId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            model.FamilyMember.PermissionLevel = model.Family.FamilyPermission.PermissionLevel;
            

            return PartialView("_FamilyMemberDetailsPartial", model);
        }

        [HttpGet]
        [Route("[controller]/[action]/{familyId:int}")]
        public async Task<IActionResult> AddFamilyMember(int familyId)
        {
            Family family = await familiesHttpClient.GetFamily(familyId);
            if (family == null || family.FamilyId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0, familyId, false);
            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = new FamilyMember { FamilyId = familyId },
                Family = family
            };

            List<Progeny> progeniesNotInFamily = [];
            List<FamilyMember> familyMembers = family.FamilyMembers;
            List<int> progenyIdsInFamily = [];
            foreach (FamilyMember familyMember in familyMembers)
            {
                if (familyMember.ProgenyId > 0 && !progenyIdsInFamily.Contains(familyMember.ProgenyId))
                {
                    progenyIdsInFamily.Add(familyMember.ProgenyId);
                }
            }

            List<Progeny> allProgenies = await progenyHttpClient.GetProgeniesUserCanAccess(PermissionLevel.View);
            foreach (Progeny progeny in allProgenies)
            {
                if (!progenyIdsInFamily.Contains(progeny.Id))
                {
                    progeniesNotInFamily.Add(progeny);
                }
            }

            model.ProgenyList =
            [
                new SelectListItem()
                {
                    Text = "New family member",
                    Value = "0"
                }

            ];

            foreach (Progeny progeny in progeniesNotInFamily)
            {
                SelectListItem item = new()
                {
                    Text = progeny.NickName,
                    Value = progeny.Id.ToString()
                };
                model.ProgenyList.Add(item);
            }


            return PartialView("_AddFamilyMemberPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFamilyMember([FromForm] FamilyMemberDetailsViewModel model)
        {
            Progeny progeny;
            if (model.CurrentProgenyId == 0)
            {
                if (model.File != null)
                {
                    await using Stream stream = model.File.OpenReadStream();
                    string fileFormat = Path.GetExtension(model.File.FileName);
                    model.CurrentProgeny.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Progeny, fileFormat);
                }
                else
                {
                    model.CurrentProgeny.PictureLink = Constants.WebAppUrl + "/photodb/childcareicon.jpg"; // Todo: Find better image
                }

                if (!model.CurrentProgeny.IsInAdminList(User.GetEmail()))
                {
                    model.CurrentProgeny.AddToAdminList(User.GetEmail());
                }
                progeny = await progenyHttpClient.AddProgeny(model.CurrentProgeny);
                model.CurrentProgenyId = progeny.Id;
            }
            else
            {
                progeny = await progenyHttpClient.GetProgeny(model.CurrentProgenyId);
            } 

            FamilyMember familyMember = new()
            {
                FamilyId = model.CurrentFamilyId,
                ProgenyId = model.CurrentProgenyId,
                MemberType = model.MemberType,
                Email = progeny.Email
            };
            FamilyMember addedFamilyMember = await familiesHttpClient.AddFamilyMember(familyMember);
            return Json(addedFamilyMember);
        }

        [HttpGet("[controller]/[action]/{familyMemberId:int}")]
        public async Task<IActionResult> UpdateFamilyMember(int familyMemberId)
        {
            FamilyMember familyMember = await familiesHttpClient.GetFamilyMember(familyMemberId);
            if (familyMember == null || familyMember.FamilyMemberId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            Family family = await familiesHttpClient.GetFamily(familyMember.FamilyId);
            if (family == null || family.FamilyId == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (family.FamilyPermission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }

            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), familyMember.ProgenyId, family.FamilyId, false);

            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = familyMember,
                Family = family
            };
            model.FamilyMember.PermissionLevel = model.Family.FamilyPermission.PermissionLevel;

            model.MemberType = model.FamilyMember.MemberType;
            model.SetMemberTypeList();
            
            return PartialView("_UpdateFamilyMemberPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFamilyMember([FromForm] FamilyMemberDetailsViewModel model)
        {
            FamilyMember familyMember = await familiesHttpClient.GetFamilyMember(model.FamilyMember.FamilyMemberId);
            if (familyMember == null || familyMember.FamilyMemberId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            Family family = await familiesHttpClient.GetFamily(familyMember.FamilyId);
            if (family == null || family.FamilyId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (family.FamilyPermission.PermissionLevel < PermissionLevel.Edit)
            {
                return PartialView("_AccessDeniedPartial");
            }
            Progeny progeny = await progenyHttpClient.GetProgeny(model.CurrentProgenyId);
            if (progeny == null || progeny.Id == 0)
            {
                return PartialView("_NotFoundPartial");
            }

            if (familyMember.Progeny.ProgenyPerMission.PermissionLevel >= PermissionLevel.Edit)
            {
                if (model.File != null && model.File.Name != string.Empty)
                {
                    await using Stream stream = model.File.OpenReadStream();
                    string fileFormat = Path.GetExtension(model.File.FileName);
                    model.CurrentProgeny.PictureLink = await imageStore.SaveImage(stream, BlobContainers.Progeny, fileFormat);
                }
                else
                {
                    // If no new image is uploaded, keep the existing picture link.
                    model.CurrentProgeny.PictureLink = progeny.PictureLink;
                }

                if (progeny.PropertiesChanged(model.CurrentProgeny))
                {
                    progeny.CopyPropertiesForUpdate(model.CurrentProgeny);
                    await progenyHttpClient.UpdateProgeny(progeny);

                }

                if (familyMember.ProgenyInfo != null)
                {
                    ProgenyInfo progenyInfo = await progenyHttpClient.GetProgenyInfo(familyMember.ProgenyId);
                    if (progenyInfo.PropertiesChanged(model.FamilyMember.ProgenyInfo))
                    {
                        progenyInfo.CopyPropertiesForUpdate(model.FamilyMember.ProgenyInfo);
                        progenyInfo.Address.CopyPropertiesForUpdate(model.FamilyMember.ProgenyInfo.Address);

                        await progenyHttpClient.UpdateProgenyInfo(progenyInfo);
                    }
                }
                familyMember.Email = progeny.Email;
            }

            familyMember.MemberType = model.MemberType;
            

            FamilyMember updatedFamilyMember = await familiesHttpClient.UpdateFamilyMember(familyMember);
            
            return Json(updatedFamilyMember);
        }

        [HttpGet("[controller]/[action]/{familyMemberId:int}")]
        public async Task<IActionResult> DeleteFamilyMember(int familyMemberId)
        {
            FamilyMember familyMember = await familiesHttpClient.GetFamilyMember(familyMemberId);
            if (familyMember == null || familyMember.FamilyMemberId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            Family family = await familiesHttpClient.GetFamily(familyMember.FamilyId);
            if (family == null || family.FamilyId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (family.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), familyMember.ProgenyId, family.FamilyId, false);
            FamilyMemberDetailsViewModel model = new(baseModel)
            {
                FamilyMember = familyMember,
                Family = family
            };
            
            model.MemberType = model.FamilyMember.MemberType;
            model.SetMemberTypeList();

            return PartialView("_DeleteFamilyMemberPartial", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFamilyMember([FromForm] FamilyMemberDetailsViewModel model)
        {
            FamilyMember familyMember = await familiesHttpClient.GetFamilyMember(model.FamilyMember.FamilyMemberId);
            if (familyMember == null || familyMember.FamilyMemberId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            Family family = await familiesHttpClient.GetFamily(familyMember.FamilyId);
            if (family == null || family.FamilyId == 0)
            {
                return PartialView("_NotFoundPartial");
            }
            if (family.FamilyPermission.PermissionLevel < PermissionLevel.Admin)
            {
                return PartialView("_AccessDeniedPartial");
            }
            
            bool result = await familiesHttpClient.DeleteFamilyMember(model.FamilyMember.FamilyMemberId);
            return Json(result);
        }
    }
}
