using KinaUnaWeb.Models;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Extensions;
using KinaUnaWeb.Models.HomeViewModels;

namespace KinaUnaWeb.Controllers
{
    /// <summary>
    /// Controller for the AddItem view.
    /// This used to handle adding new items to the database, but should now only be used to save attached/embedded files in Rich Text editors.
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "ASP0019:Suggest using IHeaderDictionary.Append or the indexer", Justification = "From Syncfusion samples.")]
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
                    Response.Headers.Add("name", resultName);
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

            return PartialView("../Shared/_NotFoundPartial");
        }

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

            return PartialView("../Shared/_NotFoundPartial");
        }

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

            return PartialView("../Shared/_NotFoundPartial");
        }
    }
}