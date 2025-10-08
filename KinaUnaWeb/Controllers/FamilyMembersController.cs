using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models.Family;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.FamiliesViewModels;
using KinaUnaWeb.Services;
using KinaUnaWeb.Services.HttpClients;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    public class FamilyMembersController(
        IFamiliesHttpClient familiesHttpClient,
        IViewModelSetupService viewModelSetupService) : Controller
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

            return PartialView("_FamilyMemberElementPartial", model);
        }

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
    }
}
