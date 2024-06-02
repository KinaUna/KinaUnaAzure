﻿using KinaUnaWeb.Models;
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
    public class AddItemController(ImageStore imageStore) : Controller
    {
        public IActionResult Index()
        {
            AboutViewModel model = new()
            {
                LanguageId = Request.GetLanguageIdFromCookie()
            };
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFile(FileItem model)
        {
            
            throw new NotImplementedException();
        }

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
    }
}