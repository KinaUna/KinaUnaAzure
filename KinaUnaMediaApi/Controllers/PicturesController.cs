using ImageMagick;
using KinaUnaMediaApi.Models.ViewModels;
using KinaUnaMediaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PicturesController : ControllerBase
    {
        private readonly MediaDbContext _context;
        private readonly ProgenyDbContext _progenyDbContext;
        private readonly ImageStore _imageStore;
        private readonly IDataService _dataService;

        public PicturesController(MediaDbContext context, ProgenyDbContext progenyDbContext, ImageStore imageStore, IDataService dataService)
        {
            _context = context;
            _progenyDbContext = progenyDbContext;
            _imageStore = imageStore;
            _dataService = dataService;
        }
        
        // GET api/pictures/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public IActionResult Page([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(progenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems; 
            if (tagFilter != "")
            {
                allItems = _dataService.GetPicturesList(progenyId); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.PictureTime).ToListAsync();
                allItems = allItems
                    .Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.ToUpper().Contains(tagFilter.ToUpper()))
                    .OrderBy(p => p.PictureTime).ToList();
            }
            else
            {
                allItems = _dataService.GetPicturesList(progenyId); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToListAsync();
                allItems = allItems
                    .Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            List<string> tagsList = new List<string>();
            foreach (Picture pic in allItems)
            {
                if (sortBy == 1)
                {
                    pic.PictureNumber = picCount - pictureCounter + 1;
                }
                else
                {
                    pic.PictureNumber = pictureCounter;
                }
                
                pictureCounter++;
                if (!String.IsNullOrEmpty(pic.Tags))
                {
                    List<string> pvmTags = pic.Tags.Split(',').ToList();
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }
            
            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = _dataService.GetCommentsList(pic.CommentThreadNumber); // await _context.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == pic.CommentThreadNumber).ToListAsync();
            }
            PicturePageViewModel model = new PicturePageViewModel();
            model.PicturesList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public IActionResult PictureViewModel(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {
            Picture picture = _dataService.GetPicture(id); // await _context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == picture.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                PictureViewModel model = new PictureViewModel();
                model.PictureId = picture.PictureId;
                model.PictureTime = picture.PictureTime;
                model.ProgenyId = picture.ProgenyId;
                model.Owners = picture.Owners;
                model.PictureLink = picture.PictureLink1200;
                model.AccessLevel = picture.AccessLevel;
                model.Author = picture.Author;
                model.AccessLevelListEn[picture.AccessLevel].Selected = true;
                model.AccessLevelListDa[picture.AccessLevel].Selected = true;
                model.AccessLevelListDe[picture.AccessLevel].Selected = true;
                model.CommentThreadNumber = picture.CommentThreadNumber;
                model.Tags = picture.Tags;
                model.Location = picture.Location;
                model.Latitude = picture.Latitude;
                model.Longtitude = picture.Longtitude;
                model.Altitude = picture.Altitude;
                model.PictureNumber = 1;
                model.PictureCount = 1;
                model.CommentsList = _dataService.GetCommentsList(picture.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == picture.CommentThreadNumber).ToListAsync();
                model.TagsList = "";
                List<string> tagsList = new List<string>();
                List<Picture> pictureList = _dataService.GetPicturesList(picture.ProgenyId); // await _context.PicturesDb.AsNoTracking()
                pictureList = pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
                if (pictureList.Any())
                {
                    int currentIndex = 0;
                    int indexer = 0;
                    foreach (Picture pic in pictureList)
                    {
                        if (pic.PictureId == picture.PictureId)
                        {
                            currentIndex = indexer;
                        }
                        indexer++;
                        if (!String.IsNullOrEmpty(pic.Tags))
                        {
                            List<string> pvmTags = pic.Tags.Split(',').ToList();
                            foreach (string tagstring in pvmTags)
                            {
                                if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                                {
                                    tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                                }
                            }
                        }
                    }
                    model.PictureNumber = currentIndex + 1;
                    model.PictureCount = pictureList.Count;
                    if(currentIndex > 0)
                    {
                        model.PrevPicture = pictureList[currentIndex - 1].PictureId;
                    }
                    else
                    {
                        model.PrevPicture = pictureList.Last().PictureId;
                    }

                    if (currentIndex + 1 < pictureList.Count)
                    {
                        model.NextPicture = pictureList[currentIndex + 1].PictureId;
                    }
                    else
                    {
                        model.NextPicture = pictureList.First().PictureId;
                    }

                    if (sortBy == 1)
                    {
                        int tempVal = model.NextPicture;
                        model.NextPicture = model.PrevPicture;
                        model.PrevPicture = tempVal;
                    }
                   
                }
                string tagItems = "[";
                if (tagsList.Any())
                {
                    foreach (string tagstring in tagsList)
                    {
                        tagItems = tagItems + "'" + tagstring + "',";
                    }

                    tagItems = tagItems.Remove(tagItems.Length - 1);
                    tagItems = tagItems + "]";
                }

                model.TagsList = tagItems;
                return Ok(model);
            }

            return NotFound();
        }

        // GET api/pictures/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id}/{accessLevel}")]
        public IActionResult Progeny(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(id, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == id && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = _dataService.GetPicturesList(id); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == id && p.AccessLevel >= accessLevel).ToListAsync();
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Any())
            {
                return Ok(picturesList);
            }
            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            picturesList.Add(tempPicture);
            return Ok(picturesList);
        }

        // GET api/pictures/bylink/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> ByLink(string id)
        {
            Picture picture = await _context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureLink == id);
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == picture.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                picture.Comments = _dataService.GetCommentsList(picture.CommentThreadNumber); // await _context.CommentsDb.Where(c => c.CommentThreadNumber == picture.CommentThreadNumber).ToListAsync();
                
                return Ok(picture);
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("{id}")]
        public IActionResult GetPicture(int id)
        {
            Picture result = _dataService.GetPicture(id); // await _context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }
                return Ok(result);
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        // POST api/pictures
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Picture model)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _progenyDbContext.UserAccessDb.SingleOrDefault(u =>
                u.ProgenyId == model.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            var memoryStream = await _imageStore.GetStream(model.PictureLink);
            memoryStream.Position = 0;
            
            using (MagickImage image = new MagickImage(memoryStream))
            {
                ExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    int rotation;
                    try
                    {

                        ExifValue gpsLongtitude = profile.GetValue(ExifTag.GPSLongitude);
                        ExifValue gpsLatitude = profile.GetValue(ExifTag.GPSLatitude);
                        ExifValue gpsAltitude = profile.GetValue(ExifTag.GPSAltitude);

                        if (gpsLongtitude != null && gpsLatitude != null)
                        {
                            Rational[] longValues = gpsLongtitude.Value as Rational[];
                            Rational[] latValues = gpsLatitude.Value as Rational[];


                            if (longValues != null && (longValues[0].Denominator != 0 && longValues[1].Denominator != 0 &&
                                                       longValues[2].Denominator != 0))
                            {
                                double long0 = longValues[0].Numerator / (double)longValues[0].Denominator;
                                double long1 = longValues[1].Numerator / (double)longValues[1].Denominator;
                                double long2 = longValues[2].Numerator / (double)longValues[2].Denominator;
                                model.Longtitude = (long0 + long1 / 60.0 + long2 / 3600).ToString(CultureInfo.CurrentCulture);
                            }
                            else
                            {
                                model.Longtitude = "";
                            }

                            if (latValues != null && (latValues[0].Denominator != 0 && latValues[1].Denominator != 0 &&
                                                      latValues[2].Denominator != 0))
                            {
                                double lat0 = latValues[0].Numerator / (double)latValues[0].Denominator;
                                double lat1 = latValues[1].Numerator / (double)latValues[1].Denominator;
                                double lat2 = latValues[2].Numerator / (double)latValues[2].Denominator;
                                model.Latitude = (lat0 + lat1 / 60.0 + lat2 / 3600).ToString(CultureInfo.CurrentCulture);
                            }
                            else
                            {
                                model.Latitude = "";
                            }
                        }
                        else
                        {
                            model.Longtitude = "";
                            model.Latitude = "";
                        }

                        if (gpsAltitude != null)
                        {
                            Rational altValues = (Rational)gpsAltitude.Value;
                            if (altValues.Denominator != 0)
                            {
                                double alt0 = altValues.Numerator / (double)altValues.Denominator;
                                model.Altitude = alt0.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                model.Altitude = "";
                            }
                        }
                        else
                        {
                            model.Altitude = "";
                        }


                    }
                    catch (ArgumentNullException)
                    {
                        model.Longtitude = "";
                        model.Latitude = "";
                        model.Altitude = "";
                    }
                    catch (NullReferenceException)
                    {
                        model.Longtitude = "";
                        model.Latitude = "";
                        model.Altitude = "";
                    }
                    catch (Exception)
                    {
                        model.Longtitude = "";
                        model.Latitude = "";
                        model.Altitude = "";
                    }

                    try
                    {
                        rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation).Value);
                        switch (rotation)
                        {
                            case 1:
                                model.PictureRotation = 0;
                                break;
                            case 3:
                                model.PictureRotation = 180;
                                break;
                            case 6:
                                model.PictureRotation = 90;
                                break;
                            case 8:
                                model.PictureRotation = 270;
                                break;

                        }
                    }
                    catch (ArgumentNullException)
                    {
                        model.PictureRotation = 0;
                    }
                    catch (NullReferenceException)
                    {
                        model.PictureRotation = 0;
                    }


                    try
                    {
                        var date = (string)profile.GetValue(ExifTag.DateTimeOriginal).Value;
                        model.PictureTime = new DateTime(
                            int.Parse(date.Substring(0, 4)), // year
                            int.Parse(date.Substring(5, 2)), // month
                            int.Parse(date.Substring(8, 2)), // day
                            int.Parse(date.Substring(11, 2)), // hour
                            int.Parse(date.Substring(14, 2)), // minute
                            int.Parse(date.Substring(17, 2)) // second
                        );
                        // Todo: Check if timezone can be extracted and UTC time found?
                    }
                    catch (FormatException)
                    {
                        model.PictureTime = null;
                    }
                    catch (OverflowException)
                    {
                        model.PictureTime = null;
                    }
                    catch (ArgumentNullException)
                    {
                        model.PictureTime = null;
                    }
                    catch (NullReferenceException)
                    {
                        model.PictureTime = null;
                    }

                    try
                    {
                        model.PictureWidth = Convert.ToInt32(profile.GetValue(ExifTag.PixelXDimension).Value);
                        model.PictureHeight = Convert.ToInt32(profile.GetValue(ExifTag.PixelYDimension).Value);
                    }
                    catch (FormatException)
                    {
                        model.PictureWidth = image.Width;
                        model.PictureHeight = image.Height;
                    }
                    catch (OverflowException)
                    {
                        model.PictureWidth = image.Width;
                        model.PictureHeight = image.Height;
                    }
                    catch (ArgumentNullException)
                    {
                        model.PictureWidth = image.Width;
                        model.PictureHeight = image.Height;
                    }
                    catch (NullReferenceException)
                    {
                        model.PictureWidth = image.Width;
                        model.PictureHeight = image.Height;
                    }
                }
                else
                {
                    model.PictureWidth = image.Width;
                    model.PictureHeight = image.Height;

                }
                if (model.PictureRotation != null)
                {
                    if (model.PictureRotation != 0)
                    {
                        image.Rotate((int)model.PictureRotation);
                    }

                }

                if (model.PictureWidth > 600)
                {
                    int newWidth = 600;
                    int newHeight = (600 / model.PictureWidth) * model.PictureHeight;

                    image.Resize(newWidth, newHeight);
                }

                image.Strip();

                using (MemoryStream memStream = new MemoryStream())
                {
                    image.Write(memStream);
                    memStream.Position = 0;
                    model.PictureLink600 = await _imageStore.SaveImage(memStream);
                }
            }

            using (MagickImage image = new MagickImage(memoryStream))
            {
                if (model.PictureRotation != null)
                {
                    if (model.PictureRotation != 0)
                    {
                        image.Rotate((int)model.PictureRotation);
                    }

                }

                if (model.PictureWidth > 1200)
                {
                    int newWidth = 1200;
                    int newHeight = (1200 / model.PictureWidth) * model.PictureHeight;

                    image.Resize(newWidth, newHeight);
                }

                image.Strip();
                
                using (MemoryStream memStream = new MemoryStream())
                {
                    image.Write(memStream);
                    memStream.Position = 0;
                    model.PictureLink1200 = await _imageStore.SaveImage(memStream);
                }
            }

            if (model.PictureTime != null)
            {
                model.PictureTime = TimeZoneInfo.ConvertTimeToUtc(model.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(model.TimeZone));
            }

            if (model.Longtitude != "" && model.Latitude != "")
            {
                model.Location = model.Latitude + ", " + model.Longtitude;
            }

            CommentThread commentThread = new CommentThread();
            await _context.CommentThreadsDb.AddAsync(commentThread);
            await _context.SaveChangesAsync();
            commentThread.CommentThreadId = commentThread.Id;
            _context.CommentThreadsDb.Update(commentThread);
            await _context.SaveChangesAsync();
            model.CommentThreadNumber = commentThread.CommentThreadId;

            await _context.PicturesDb.AddAsync(model);
            await _context.SaveChangesAsync();
            _dataService.SetPicture(model.PictureId);

            return Ok(model);
        }

        // PUT api/pictures/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Picture value)
        {
            Picture picture = await _context.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == id);

            // Todo: more validation of the values
            if (picture == null)
            {
                return NotFound();
            }

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                u.ProgenyId == picture.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            picture.Location = value.Location;
            picture.Tags = value.Tags;
            picture.AccessLevel = value.AccessLevel;
            picture.Altitude = value.Altitude;
            picture.Author = value.Author;
            picture.PictureTime = value.PictureTime;
            picture.Latitude = value.Latitude;
            picture.Longtitude = value.Longtitude;
            picture.PictureLink600 = value.PictureLink600;
            picture.PictureLink1200 = value.PictureLink1200;
            picture.PictureLink = value.PictureLink;
            
            _context.PicturesDb.Update(picture);
            await _context.SaveChangesAsync();
            _dataService.SetPicture(picture.PictureId);

            return Ok(picture);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Picture picture = await _context.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == id);
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                    u.ProgenyId == picture.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                List<Comment> comments = _context.CommentsDb
                    .Where(c => c.CommentThreadNumber == picture.CommentThreadNumber).ToList();
                if (comments.Any())
                {
                    _context.CommentsDb.RemoveRange(comments);
                    _context.SaveChanges();
                    foreach (Comment deletedComment in comments)
                    {
                        _dataService.RemoveComment(deletedComment.CommentId, deletedComment.CommentThreadNumber);
                    }
                }

                CommentThread cmntThread =
                    _context.CommentThreadsDb.SingleOrDefault(c => c.CommentThreadId == picture.CommentThreadNumber);
                if (cmntThread != null)
                {
                    _context.CommentThreadsDb.Remove(cmntThread);
                    _context.SaveChanges();
                    _dataService.RemoveCommentsList(picture.CommentThreadNumber);
                }
                if (!picture.PictureLink.ToLower().StartsWith("http"))
                {
                    await _imageStore.DeleteImage(picture.PictureLink);
                    await _imageStore.DeleteImage(picture.PictureLink600);
                    await _imageStore.DeleteImage(picture.PictureLink1200);
                }
                _context.PicturesDb.Remove(picture);
                await _context.SaveChangesAsync();

                _dataService.RemovePicture(picture.PictureId, picture.ProgenyId);

                return NoContent();
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/pictures/random/[Progeny id]?accessLevel=5
        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public IActionResult Random(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(progenyId, userEmail); // _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = _dataService.GetPicturesList(progenyId); // await _context.PicturesDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).ToListAsync();
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Any())
            {
                Random r = new Random();
                var pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];

                return Ok(picture);
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny ;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]/{progenyId}/{accessLevel}")]
        public IActionResult RandomMobile(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(progenyId, userEmail); // _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = _dataService.GetPicturesList(progenyId); // await _context.PicturesDb.Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).ToListAsync();
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Any())
            {
                Random r = new Random();
                var pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                if (!picture.PictureLink.ToLower().StartsWith("http"))
                {
                    picture.PictureLink = _imageStore.UriFor(picture.PictureLink);
                    picture.PictureLink1200 = _imageStore.UriFor(picture.PictureLink1200);
                    picture.PictureLink600 = _imageStore.UriFor(picture.PictureLink600);
                }
                
                return Ok(picture);
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("[action]/{id}")]
        public IActionResult GetPictureMobile(int id)
        {
            Picture result = _dataService.GetPicture(id); // await _context.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); // _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == result.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                if (!result.PictureLink.ToLower().StartsWith("http"))
                {
                    result.PictureLink = _imageStore.UriFor(result.PictureLink);
                    result.PictureLink1200 = _imageStore.UriFor(result.PictureLink1200);
                    result.PictureLink600 = _imageStore.UriFor(result.PictureLink600);
                }
                return Ok(result);
            }

            Progeny progeny = new Progeny();
            progeny.Name = Constants.AppName;
            progeny.Admins = Constants.AdminEmail;
            progeny.NickName = Constants.AppName;
            progeny.BirthDay = new DateTime(2018, 2, 18, 18, 2, 0);

            progeny.Id = 0;
            progeny.TimeZone = Constants.DefaultTimezone;
            Picture tempPicture = new Picture();
            tempPicture.ProgenyId = 0;
            tempPicture.Progeny = progeny;
            tempPicture.AccessLevel = 5;
            tempPicture.PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg";
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]")]
        public IActionResult PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = _dataService.GetProgenyUserAccessForUser(progenyId, userEmail); // _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems;
            if (tagFilter != "")
            {
                allItems = _dataService.GetPicturesList(progenyId); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.PictureTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags.ToUpper().Contains(tagFilter.ToUpper())).OrderBy(p => p.PictureTime).ToList();
            }
            else
            {
                allItems = _dataService.GetPicturesList(progenyId); // await _context.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId && p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToListAsync();
                allItems = allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime).ToList();
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            List<string> tagsList = new List<string>();
            foreach (Picture pic in allItems)
            {
                if (sortBy == 1)
                {
                    pic.PictureNumber = picCount - pictureCounter + 1;
                }
                else
                {
                    pic.PictureNumber = pictureCounter;
                }

                pictureCounter++;
                if (!String.IsNullOrEmpty(pic.Tags))
                {
                    List<string> pvmTags = pic.Tags.Split(',').ToList();
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = _dataService.GetCommentsList(pic.CommentThreadNumber); // await _context.CommentsDb.AsNoTracking().Where(c => c.CommentThreadNumber == pic.CommentThreadNumber).ToListAsync();
                if (!pic.PictureLink.ToLower().StartsWith("http"))
                {
                    pic.PictureLink = _imageStore.UriFor(pic.PictureLink);
                }
                if (!pic.PictureLink1200.ToLower().StartsWith("http"))
                {
                    pic.PictureLink1200 = _imageStore.UriFor(pic.PictureLink1200);
                }
                if (!pic.PictureLink600.ToLower().StartsWith("http"))
                {
                    pic.PictureLink600 = _imageStore.UriFor(pic.PictureLink600);
                }
            }
            PicturePageViewModel model = new PicturePageViewModel();
            model.PicturesList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;
            model.TagFilter = tagFilter;
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        
        // Download pictures to StorageBlob from Url
        [HttpGet]
        [Route("[action]/{pictureId}")]
        public async Task<IActionResult> DownloadPicture(int pictureId)
        {
            Picture picture = await _context.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == pictureId);
            if (picture != null && picture.PictureLink.ToLower().StartsWith("http"))
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = _progenyDbContext.UserAccessDb.AsNoTracking().SingleOrDefault(u =>
                    u.ProgenyId == picture.ProgenyId && u.UserId.ToUpper() == userEmail.ToUpper());

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                using (Stream stream = GetStreamFromUrl(picture.PictureLink))
                {
                    picture.PictureLink = await _imageStore.SaveImage(stream);
                }

                using (Stream stream = GetStreamFromUrl(picture.PictureLink600))
                {
                    picture.PictureLink600 = await _imageStore.SaveImage(stream);
                }

                using (Stream stream = GetStreamFromUrl(picture.PictureLink1200))
                {
                    picture.PictureLink1200 = await _imageStore.SaveImage(stream);
                }

                _context.PicturesDb.Update(picture);
                await _context.SaveChangesAsync();
                return Ok(picture);
            }
            else
            {
                return NotFound();
            }
        }

        private static Stream GetStreamFromUrl(string url)
        {
            byte[] imageData;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(url);

            return new MemoryStream(imageData);
        }
    }
}
