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
    [Authorize]
    public class AddItemController : Controller
    {
        private readonly ImageStore _imageStore;
        
        public AddItemController(ImageStore imageStore)
        {
            _imageStore = imageStore;
        }
        public IActionResult Index()
        {
            AboutViewModel model = new AboutViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            return View(model);
        }
        
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFile(FileItem model)
        {
            
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult> SaveRtfFile(IList<IFormFile> UploadFiles)
        {
            try
            {
                foreach (IFormFile file in UploadFiles)
                {
                    if (UploadFiles.Any())
                    {
                        string filename;
                        await using (Stream stream = file.OpenReadStream())
                        {
                            filename = await _imageStore.SaveImage(stream, BlobContainers.Notes);
                        }

                        string resultName = _imageStore.UriFor(filename, BlobContainers.Notes);
                        Response.Clear();
                        Response.ContentType = "application/json; charset=utf-8";
                        Response.Headers.Add("name", resultName);
                        Response.StatusCode = 204;
                    }
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
    }
}