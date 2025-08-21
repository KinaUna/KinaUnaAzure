using KinaUna.Data.Extensions;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.HomeViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Controller for adding items.
    /// This used to handle adding new items to the database.
    /// </summary>
    /// <param name="imageStore"></param>
    [Authorize]
    public class AddItemController(ImageStore imageStore) : Controller
    {
        /// <summary>
        /// Add Item Index page. Not used anymore.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            AboutViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie()
            };
            return View(model);
        }

        /// <summary>
        /// For deleting Note embedded image files from the Azure Blob Storage.
        /// </summary>
        /// <param name="model">FileItem object.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFile(FileItem model)
        {
            
            throw new NotImplementedException();
        }

        /// <summary>
        /// For saving embedded  image files in Note items to the Azure Blob Storage.
        /// The Note item uses the Syncfusion Rich Text Editor, which receives the url for the image from this method.
        /// </summary>
        /// <param name="UploadFiles">List of files to save.</param>
        /// <returns>Empty string, the file url is returned via the response header. </returns>
        [HttpPost]
        // ReSharper disable once InconsistentNaming
        public async Task<ActionResult> SaveRtfFile(IList<IFormFile> UploadFiles)
        {
            try
            {
                foreach (IFormFile file in UploadFiles)
                {
                    if (!UploadFiles.Any()) continue;

                    string filename;
                    await using (Stream stream = file.OpenReadStream())
                    {
                        string fileFormat = Path.GetExtension(file.FileName);
                        filename = await imageStore.SaveImage(stream, BlobContainers.Notes, fileFormat);
                    }

                    string resultName = imageStore.UriFor(filename, BlobContainers.Notes);
                    Response.Clear();
                    Response.ContentType = "application/json; charset=utf-8";
                    Response.Headers.Append("name", resultName);
                    Response.StatusCode = 204;
                }
            }
            catch (Exception)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.StatusCode = 204;
            }
            return Content("");
        }

        /// <summary>
        /// Returns the appropriate modal content for adding a new item based on the specified item type.
        /// </summary>
        /// <remarks>This method dynamically determines the appropriate action to redirect to based on the
        /// provided <paramref name="itemType"/>. If the item type is not recognized, a "Not Found" partial view is
        /// returned.</remarks>
        /// <param name="itemType">The type of item to add. Valid values include "user", "progeny", "note", "calendar", "sleep", "picture",
        /// "video", "vocabulary", "friend", "measurement", "contact", "skill", "vaccination", "location", and "todo".</param>
        /// <param name="progenyId">The identifier of the progeny associated with the item, if applicable.</param>
        /// <returns>An <see cref="IActionResult"/> that redirects to the appropriate action for adding the specified item type.
        /// If the <paramref name="itemType"/> is invalid, returns a partial view indicating that the requested content
        /// was not found.</returns>
        [HttpGet]
        public IActionResult GetAddItemModalContent(string itemType, int progenyId)
        {
            if (itemType == "user")
            {
                return RedirectToAction("AddAccess", "AccessManagement", new {progenyId});
            }

            if (itemType == "progeny")
            {
                return RedirectToAction("AddProgeny", "Progeny");
            }

            if (itemType == "note")
            {
                return RedirectToAction("AddNote", "Notes");
            }

            if (itemType == "calendar")
            {
                return RedirectToAction("AddEvent", "Calendar");
            }

            if(itemType == "sleep")
            {
                return RedirectToAction("AddSleep", "Sleep");
            }

            if (itemType == "picture")
            {
                return RedirectToAction("AddPicture", "Pictures");
            }

            if (itemType == "video")
            {
                return RedirectToAction("AddVideo", "Videos");
            }

            if (itemType == "vocabulary")
            {
                return RedirectToAction("AddVocabulary", "Vocabulary");
            }

            if (itemType == "friend")
            {
                return RedirectToAction("AddFriend", "Friends");
            }

            if (itemType == "measurement")
            {
                return RedirectToAction("AddMeasurement", "Measurements");
            }

            if (itemType == "contact")
            {
                return RedirectToAction("AddContact", "Contacts");
            }

            if (itemType == "skill")
            {
                return RedirectToAction("AddSkill", "Skills");
            }

            if (itemType == "vaccination")
            {
                return RedirectToAction("AddVaccination", "Vaccinations");
            }

            if (itemType == "location")
            {
                return RedirectToAction("AddLocation", "Locations");
            }

            if (itemType == "todo")
            {
                return RedirectToAction("AddTodo", "Todos");
            }
            
            return PartialView("../Shared/_NotFoundPartial");
        }

        /// <summary>
        /// Retrieves the appropriate modal content for editing an item based on its type.
        /// </summary>
        /// <remarks>This method dynamically determines the appropriate controller and action to handle
        /// the editing of the specified item type. If the item type is invalid or unsupported, a "Not Found" partial
        /// view is returned.</remarks>
        /// <param name="itemType">The type of the item to be edited. Valid values include "user", "progeny", "note", "calendar", "sleep",
        /// "vocabulary", "friend", "measurement", "contact", "skill", "vaccination", "location", "picture", "video",
        /// and "todo".</param>
        /// <param name="itemId">The unique identifier of the item to be edited.</param>
        /// <returns>An <see cref="IActionResult"/> that redirects to the appropriate edit action for the specified item type. If
        /// the item type is not recognized, returns a partial view indicating that the item was not found.</returns>
        [HttpGet]
        public IActionResult GetEditItemModalContent(string itemType, int itemId)
        {
            if (itemType == "user")
            {
                return RedirectToAction("EditAccess", "AccessManagement", new { accessId = itemId });
            }

            if (itemType == "progeny")
            {
                return RedirectToAction("EditProgeny", "Progeny", new { progenyId = itemId });
            }

            if (itemType == "note")
            {
                return RedirectToAction("EditNote", "Notes", new { itemId });
            }

            if (itemType == "calendar")
            {
                return RedirectToAction("EditEvent", "Calendar", new { itemId });
            }

            if (itemType == "sleep")
            {
                return RedirectToAction("EditSleep", "Sleep", new { itemId });
            }

            if (itemType == "vocabulary")
            {
                return RedirectToAction("EditVocabulary", "Vocabulary", new { itemId });
            }

            if (itemType == "friend")
            {
                return RedirectToAction("EditFriend", "Friends", new { itemId });
            }

            if (itemType == "measurement")
            {
                return RedirectToAction("EditMeasurement", "Measurements", new { itemId });
            }

            if (itemType == "contact")
            {
                return RedirectToAction("EditContact", "Contacts", new { itemId });
            }

            if (itemType == "skill")
            {
                return RedirectToAction("EditSkill", "Skills", new { itemId });
            }

            if (itemType == "vaccination")
            {
                return RedirectToAction("EditVaccination", "Vaccinations", new { itemId });
            }

            if (itemType == "location")
            {
                return RedirectToAction("EditLocation", "Locations", new { itemId });
            }

            if (itemType == "picture")
            {
                return RedirectToAction("Picture", "Pictures", new { id = itemId, partialView = true });
            }

            if (itemType == "video")
            {
                return RedirectToAction("Video", "Videos", new { id = itemId, partialView = true });
            }

            if (itemType == "todo")
            {
                return RedirectToAction("EditTodo", "Todos", new { itemId, partialView = true });
            }

            if (itemType == "subtask")
            {
                return RedirectToAction("EditSubtask", "Subtasks", new { itemId });
            }

            return PartialView("../Shared/_NotFoundPartial", new { itemId });
        }

        /// <summary>
        /// Retrieves the appropriate modal content for deleting an item based on its type.
        /// </summary>
        /// <remarks>This method determines the appropriate delete action based on the provided <paramref
        /// name="itemType"/>  and redirects to the corresponding controller action. If the item type is unrecognized, a
        /// "Not Found" partial view is returned.</remarks>
        /// <param name="itemType">The type of the item to delete. Valid values are "user", "progeny", or "note".</param>
        /// <param name="itemId">The unique identifier of the item to delete.</param>
        /// <returns>An <see cref="IActionResult"/> that redirects to the corresponding delete action for the specified item
        /// type,  or a partial view indicating that the item was not found if the type is invalid.</returns>
        [HttpGet]
        public IActionResult GetDeleteItemModalContent(string itemType, int itemId)
        {
            if (itemType == "user")
            {
                return RedirectToAction("DeleteAccess", "AccessManagement", new { accessId = itemId });
            }

            if (itemType == "progeny")
            {
                return RedirectToAction("DeleteProgeny", "Progeny", new { progenyId = itemId });
            }

            if (itemType == "note")
            {
                return RedirectToAction("DeleteNote", "Notes", new { noteId = itemId });
            }

            // Todo: Other item types can be added here as needed.

            return PartialView("../Shared/_NotFoundPartial");
        }
    }
}