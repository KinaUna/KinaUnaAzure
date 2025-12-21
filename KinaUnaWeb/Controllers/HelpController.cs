using KinaUna.Data.Extensions;
using KinaUna.Data.Models.Support;
using KinaUnaWeb.Services.HttpClients;
using KinaUnaWeb.Services.HttpClients.Support;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    public class HelpController(IHelpHttpClient helpHttpClient, IUserInfosHttpClient userInfosHttpClient) : Controller
    {
        /// <summary>
        /// Returns a partial view containing help content for a specified page element and language.
        /// </summary>
        /// <remarks>Use this method to dynamically retrieve localized help information for specific UI
        /// elements. The returned partial view can be rendered in response to user actions, such as requesting help for
        /// a particular control.</remarks>
        /// <param name="page">The name of the page for which help content is requested. Cannot be null or empty.</param>
        /// <param name="element">The identifier of the element on the page for which help content is requested. Cannot be null or empty.</param>
        /// <param name="languageId">The identifier of the language in which the help content should be provided. Must be a valid language ID.</param>
        /// <returns>A <see cref="PartialViewResult"/> containing the help content for the specified page element and language.</returns>
        public async Task<IActionResult> HelpDetails(string page, string element, int languageId)
        {
            HelpContent helpContent = await helpHttpClient.GetHelpContent(page, element, languageId);
            return PartialView("HelpContentPartialView", helpContent);
        }


        public async Task<IActionResult> AddHelpContent()
        {
            UserInfo userInfo = await userInfosHttpClient.GetExtendedUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            HelpContent model = new();
            return PartialView("_AddHelpContentPartial", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddHelpContent(HelpContent helpContent)
        {
            UserInfo userInfo = await userInfosHttpClient.GetExtendedUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                HelpContent addedHelpContent = await helpHttpClient.AddHelpContent(helpContent);
                return RedirectToAction("HelpDetails", new { page = addedHelpContent.Page, element = addedHelpContent.Element, languageId = addedHelpContent.LanguageId });
            }

            return PartialView("_AddHelpContentPartial", helpContent);
        }

        [HttpGet]
        public async Task<IActionResult> EditHelpContent(int helpContentId)
        {
            UserInfo userInfo = await userInfosHttpClient.GetExtendedUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            HelpContent helpContent = await helpHttpClient.GetHelpContentById(helpContentId);
            return PartialView("_EditHelpContentPartial", helpContent);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHelpContent(HelpContent helpContent)
        {
            UserInfo userInfo = await userInfosHttpClient.GetExtendedUserInfoByUserId(User.GetUserId());
            if (userInfo == null || !userInfo.IsKinaUnaAdmin)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                HelpContent updatedHelpContent = await helpHttpClient.UpdateHelpContent(helpContent);
                return RedirectToAction("HelpDetails", new { page = updatedHelpContent.Page, element = updatedHelpContent.Element, languageId = updatedHelpContent.LanguageId });
            }

            return PartialView("_EditHelpContentPartial", helpContent);
        }
    }
}
