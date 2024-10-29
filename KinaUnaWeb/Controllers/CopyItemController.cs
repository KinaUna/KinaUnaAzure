using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Controller for the copying items.
    /// This used to handle copying items from one progeny to another or to the same progeny with different properties.
    /// </summary>
    [Authorize]
    public class CopyItemController : Controller
    {
        [HttpGet]
        public IActionResult GetCopyItemModalContent(string itemType, int itemId)
        {
            if (itemType == "note")
            {
                return RedirectToAction("CopyNote", "Notes", new { itemId });
            }

            if (itemType == "calendar")
            {
                return RedirectToAction("CopyEvent", "Calendar", new { itemId });
            }

            if (itemType == "sleep")
            {
                return RedirectToAction("CopySleep", "Sleep", new { itemId });
            }

            //if (itemType == "picture")
            //{
            //    return RedirectToAction("CopyPicture", "Pictures", new { itemId });
            //}

            //if (itemType == "video")
            //{
            //    return RedirectToAction("CopyVideo", "Videos", new { itemId });
            //}

            if (itemType == "vocabulary")
            {
                return RedirectToAction("CopyVocabulary", "Vocabulary", new { itemId });
            }

            if (itemType == "friend")
            {
                return RedirectToAction("CopyFriend", "Friends", new { itemId });
            }

            if (itemType == "measurement")
            {
                return RedirectToAction("CopyMeasurement", "Measurements", new { itemId });
            }

            if (itemType == "contact")
            {
                return RedirectToAction("CopyContact", "Contacts", new { itemId });
            }

            if (itemType == "skill")
            {
                return RedirectToAction("CopySkill", "Skills", new { itemId });
            }

            if (itemType == "vaccination")
            {
                return RedirectToAction("CopyVaccination", "Vaccinations", new { itemId });
            }

            //if (itemType == "location")
            //{
            //    return RedirectToAction("CopyLocation", "Locations", new { itemId });
            //}

            return PartialView("../Shared/_NotFoundPartial");
        }
       
    }
}