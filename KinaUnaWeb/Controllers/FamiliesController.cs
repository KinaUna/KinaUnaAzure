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
    public class FamiliesController(IFamiliesHttpClient familiesHttpClient, IViewModelSetupService viewModelSetupService, IProgenyHttpClient progenyHttpClient) : Controller
    {
        public async Task<IActionResult> Index()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamiliesViewModel model = new FamiliesViewModel(baseModel);

            model.Families = await familiesHttpClient.GetMyFamilies();

            if (model.Families.Count == 0)
            {
                Family family = new Family();
                model.Families.Add(family);
            }
            else
            {
                foreach (Family family in model.Families)
                {
                    if (family.FamilyMembers.Count > 0)
                    {
                        foreach (FamilyMember familyMember in family.FamilyMembers)
                        {
                            if (familyMember.ProgenyId > 0)
                            {
                                familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                            }
                        }
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Members()
        {
            BaseItemsViewModel baseModel = await viewModelSetupService.SetupViewModel(Request.GetLanguageIdFromCookie(), User.GetEmail(), 0);
            FamiliesViewModel model = new FamiliesViewModel(baseModel);

            model.Families = await familiesHttpClient.GetMyFamilies();

            if (model.Families.Count == 0)
            {
                Family family = new Family();
                model.Families.Add(family);
            }
            else
            {
                foreach (Family family in model.Families)
                {
                    if (family.FamilyMembers.Count > 0)
                    {
                        foreach (FamilyMember familyMember in family.FamilyMembers)
                        {
                            if (familyMember.ProgenyId > 0)
                            {
                                familyMember.Progeny = await progenyHttpClient.GetProgeny(familyMember.ProgenyId);
                            }
                        }
                    }
                }
            }

            return View(model);
        }

        public async Task<IActionResult> UserAccess()
        {
            return View();
        }
    }
}
