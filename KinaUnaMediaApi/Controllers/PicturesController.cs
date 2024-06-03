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
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace KinaUnaMediaApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class PicturesController(
        ImageStore imageStore,
        IDataService dataService,
        AzureNotifications azureNotifications,
        IPicturesService picturesService,
        IVideosService videosService,
        ICommentsService commentsService,
        IConfiguration configuration)
        : ControllerBase
    {
        // GET api/pictures/page[?pageSize=3&pageIndex=10&progenyId=2&accessLevel=1&tagFilter=funny]
        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> Page([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems; 
            if (!string.IsNullOrEmpty(tagFilter))
            {
                allItems = await picturesService.GetPicturesList(progenyId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
            }
            else
            {
                allItems = await picturesService.GetPicturesList(progenyId);
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            List<string> tagsList = [];
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
                if (string.IsNullOrEmpty(pic.Tags)) continue;

                List<string> pvmTags = [.. pic.Tags.Split(',')];
                foreach (string tagstring in pvmTags)
                {
                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                    {
                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                    }
                }
            }
            
            List<Picture> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
            }
            PicturePageViewModel model = new()
            {
                PicturesList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy,
                TagFilter = tagFilter
            };
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> PictureViewModel(int id, int accessLevel, [FromQuery] int sortBy = 1)
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail); 

            if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            PictureViewModel model = new()
            {
                PictureId = picture.PictureId,
                PictureTime = picture.PictureTime,
                ProgenyId = picture.ProgenyId,
                Owners = picture.Owners,
                PictureLink = picture.PictureLink1200,
                AccessLevel = picture.AccessLevel,
                Author = picture.Author
            };
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
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber); 
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId); 
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
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
                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
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
                    (model.PrevPicture, model.NextPicture) = (model.NextPicture, model.PrevPicture);
                }

            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            model.TagsList = tagItems;
            return Ok(model);

        }

        // GET api/pictures/progeny/[id]/[accessLevel]
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        public async Task<IActionResult> Progeny(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(id); 
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Count != 0)
            {
                foreach (Picture pic in picturesList)
                {
                    pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
                    if (!pic.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                    {
                        pic.PictureLink = imageStore.UriFor(pic.PictureLink);
                    }
                    if (!pic.PictureLink1200.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                    {
                        pic.PictureLink1200 = imageStore.UriFor(pic.PictureLink1200);
                    }
                    if (!pic.PictureLink600.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                    {
                        pic.PictureLink600 = imageStore.UriFor(pic.PictureLink600);
                    }
                }

                return Ok(picturesList);
            }
            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),

                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
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
            Picture picture = await picturesService.GetPictureByLink(id);
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null && picture.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                picture.Comments = await commentsService.GetCommentsList(picture.CommentThreadNumber); 
                
                return Ok(picture);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),

                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPicture(int id)
        {
            Picture result = await picturesService.GetPicture(id); 
            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); 

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }
                return Ok(result);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),

                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
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
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(model.ProgenyId, userEmail);
            
            if (userAccess == null || userAccess.AccessLevel > 0)
            {
                return Unauthorized();
            }

            MemoryStream memoryStream = await imageStore.GetStream(model.PictureLink);
            memoryStream.Position = 0;
            
            using (MagickImage image = new(memoryStream))
            {
                IExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    try
                    {

                        IExifValue gpsLongtitude = profile.GetValue(ExifTag.GPSLongitude);
                        IExifValue gpsLatitude = profile.GetValue(ExifTag.GPSLatitude);
                        IExifValue gpsAltitude = profile.GetValue(ExifTag.GPSAltitude);

                        if (gpsLongtitude != null && gpsLatitude != null)
                        {
                            if (gpsLongtitude.GetValue() is Rational[] longValues && longValues[0].Denominator != 0 && longValues[1].Denominator != 0 &&
                                                       longValues[2].Denominator != 0)
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

                            if (gpsLatitude.GetValue() is Rational[] latValues && latValues[0].Denominator != 0 && latValues[1].Denominator != 0 &&
                                                      latValues[2].Denominator != 0)
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
                            Rational altValues = (Rational)gpsAltitude.GetValue();
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
                        int rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                        model.PictureRotation = rotation switch
                        {
                            1 => 0,
                            3 => 180,
                            6 => 90,
                            8 => 270,
                            _ => model.PictureRotation
                        };
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
                        string date = profile.GetValue(ExifTag.DateTimeOriginal)?.Value;
                        if (!string.IsNullOrEmpty(date))
                        {
                            model.PictureTime = new DateTime(
                                int.Parse(date[..4]), // year
                                int.Parse(date.Substring(5, 2)), // month
                                int.Parse(date.Substring(8, 2)), // day
                                int.Parse(date.Substring(11, 2)), // hour
                                int.Parse(date.Substring(14, 2)), // minute
                                int.Parse(date.Substring(17, 2)) // second
                            );
                            // Todo: Check if timezone can be extracted and UTC time found?
                        }
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
                        Number w = profile.GetValue(ExifTag.PixelXDimension).Value;
                        Number h = profile.GetValue(ExifTag.PixelYDimension).Value;
                        
                        model.PictureWidth = Convert.ToInt32((uint)w);
                        model.PictureHeight = Convert.ToInt32((uint)h);
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
                        image.Rotate(-(int)model.PictureRotation);
                    }

                }

                if (model.PictureWidth > 600)
                {
                    const int newWidth = 600;
                    int newHeight = (600 / model.PictureWidth) * model.PictureHeight;

                    image.Resize(newWidth, newHeight);
                }

                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                model.PictureLink600 = await imageStore.SaveImage(memStream);
            }

            using (MagickImage image = new(memoryStream))
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
                    const int newWidth = 1200;
                    int newHeight = (1200 / model.PictureWidth) * model.PictureHeight;

                    image.Resize(newWidth, newHeight);
                }

                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                model.PictureLink1200 = await imageStore.SaveImage(memStream);
            }

            if (model.PictureTime != null)
            {
                model.PictureTime = TimeZoneInfo.ConvertTimeToUtc(model.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(model.TimeZone));
            }

            if (model.Longtitude != "" && model.Latitude != "")
            {
                if (string.IsNullOrEmpty(model.Location))
                {
                    model.Location = model.Latitude + ", " + model.Longtitude;
                }
            }

            CommentThread commentThread = await commentsService.AddCommentThread();
            model.CommentThreadNumber = commentThread.Id;

            model = await picturesService.AddPicture(model);
           
            await picturesService.SetPicture(model.PictureId);
            await commentsService.SetCommentsList(model.CommentThreadNumber);

            Progeny prog = await dataService.GetProgeny(model.ProgenyId);
            UserInfo userinfo = await dataService.GetUserInfoByEmail(User.GetEmail());
            string title = "New Photo added for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " added a new photo for " + prog.NickName;
            TimeLineItem tItem = new()
            {
                ProgenyId = model.ProgenyId,
                ItemId = model.PictureId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                AccessLevel = model.AccessLevel
            };
            await azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(model);
        }

        // PUT api/pictures/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Picture value)
        {
            Picture picture = await picturesService.GetPicture(id);

            // Todo: more validation of the values
            if (picture == null)
            {
                return NotFound();
            }

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

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
            
            picture = await picturesService.UpdatePicture(picture);
            
            await picturesService.SetPicture(picture.PictureId);
            await commentsService.SetCommentsList(picture.CommentThreadNumber);

            Progeny prog = await dataService.GetProgeny(picture.ProgenyId);
            UserInfo userinfo = await dataService.GetUserInfoByEmail(User.GetEmail());
            string title = "Photo Edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a photo for " + prog.NickName;
            TimeLineItem tItem = new()
            {
                ProgenyId = picture.ProgenyId,
                ItemId = picture.PictureId.ToString(),
                ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                AccessLevel = picture.AccessLevel
            };
            await azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(picture);
        }

        // DELETE api/progeny/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Picture picture = await picturesService.GetPicture(id);
            if (picture != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                List<Comment> comments = await commentsService.GetCommentsList(picture.CommentThreadNumber);
                if (comments.Count != 0)
                {
                    foreach (Comment deletedComment in comments)
                    {
                        await commentsService.DeleteComment(deletedComment);
                        await commentsService.RemoveComment(deletedComment.CommentId, deletedComment.CommentThreadNumber);
                    }
                }

                CommentThread cmntThread = await commentsService.GetCommentThread(picture.CommentThreadNumber);
                if (cmntThread != null)
                {
                    await commentsService.RemoveCommentsList(picture.CommentThreadNumber);
                }
                if (!picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    await imageStore.DeleteImage(picture.PictureLink);
                    await imageStore.DeleteImage(picture.PictureLink600);
                    await imageStore.DeleteImage(picture.PictureLink1200);
                }

                await picturesService.DeletePicture(picture);
                await picturesService.RemovePicture(picture.PictureId, picture.ProgenyId);

                Progeny prog = await dataService.GetProgeny(picture.ProgenyId);
                UserInfo userinfo = await dataService.GetUserInfoByEmail(User.GetEmail());
                string title = "Photo deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a photo for " + prog.NickName;
                TimeLineItem tItem = new()
                {
                    ProgenyId = picture.ProgenyId,
                    ItemId = picture.PictureId.ToString(),
                    ItemType = (int)KinaUnaTypes.TimeLineType.Photo,
                    AccessLevel = 0
                };
                await azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

                return NoContent();
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/pictures/random/[Progeny id]?accessLevel=5
        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        public async Task<IActionResult> Random(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(progenyId, userEmail); 

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(progenyId);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Count != 0)
            {
                Random r = new();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];

                return Ok(picture);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),

                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);
            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]/{progenyId:int}/{accessLevel:int}")]
        public async Task<IActionResult> RandomMobile(int progenyId, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> picturesList = await picturesService.GetPicturesList(progenyId);
            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();
            if (picturesList.Count != 0)
            {
                Random r = new();
                int pictureNumber = r.Next(0, picturesList.Count);

                Picture picture = picturesList[pictureNumber];
                if (picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) return Ok(picture);

                picture.PictureLink = imageStore.UriFor(picture.PictureLink);
                picture.PictureLink1200 = imageStore.UriFor(picture.PictureLink1200);
                picture.PictureLink600 = imageStore.UriFor(picture.PictureLink600);

                return Ok(picture);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),
                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };

            Picture tempPicture = new()
            {
                ProgenyId = progenyId,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink = Constants.WebAppUrl + "/photodb/0/default_temp.jpg",
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg",
                PictureLink1200 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg",
                PictureTime = new DateTime(2018, 9, 1, 12, 00, 00)
            };
            return Ok(tempPicture);
        }

        // GET api/pictures/5
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetPictureMobile(int id)
        {
            Picture result = await picturesService.GetPicture(id);
            if (result != null)
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess == null && result.ProgenyId != Constants.DefaultChildId)
                {
                    return Unauthorized();
                }

                if (result.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) return Ok(result);

                result.PictureLink = imageStore.UriFor(result.PictureLink);
                result.PictureLink1200 = imageStore.UriFor(result.PictureLink1200);
                result.PictureLink600 = imageStore.UriFor(result.PictureLink600);
                return Ok(result);
            }

            Progeny progeny = new()
            {
                Name = Constants.AppName,
                Admins = configuration.GetValue<string>("AdminEmail"),
                NickName = Constants.AppName,
                BirthDay = new DateTime(2018, 2, 18, 18, 2, 0),

                Id = 0,
                TimeZone = Constants.DefaultTimezone
            };
            Picture tempPicture = new()
            {
                ProgenyId = 0,
                Progeny = progeny,
                AccessLevel = 5,
                PictureLink600 = Constants.WebAppUrl + "/photodb/0/default_temp.jpg"
            };
            tempPicture.PictureLink1200 = tempPicture.PictureLink600;
            tempPicture.PictureLink = tempPicture.PictureLink600;
            tempPicture.ProgenyId = progeny.Id;
            tempPicture.PictureTime = new DateTime(2018, 9, 1, 12, 00, 00);

            return Ok(tempPicture);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> PageMobile([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] string tagFilter = "", [FromQuery] int sortBy = 1)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(progenyId, userEmail); 

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Picture> allItems = await picturesService.GetPicturesList(progenyId);
            List<string> tagsList = [];
            foreach (Picture pic in allItems)
            {
                if (string.IsNullOrEmpty(pic.Tags)) continue;

                List<string> pvmTags = [.. pic.Tags.Split(',')];
                foreach (string tagstring in pvmTags)
                {
                    if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                    {
                        tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                    }
                }
            }
            if (!string.IsNullOrEmpty(tagFilter))
            {
                
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel && p.Tags != null && p.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).OrderBy(p => p.PictureTime)];
            }
            else
            {
                allItems = [.. allItems.Where(p => p.AccessLevel >= accessLevel).OrderBy(p => p.PictureTime)];
            }

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int pictureCounter = 1;
            int picCount = allItems.Count;
            
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
                
            }

            List<Picture> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            foreach (Picture pic in itemsOnPage)
            {
                pic.Comments = await commentsService.GetCommentsList(pic.CommentThreadNumber);
                if (!pic.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    pic.PictureLink = imageStore.UriFor(pic.PictureLink);
                }
                if (!pic.PictureLink1200.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    pic.PictureLink1200 = imageStore.UriFor(pic.PictureLink1200);
                }
                if (!pic.PictureLink600.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    pic.PictureLink600 = imageStore.UriFor(pic.PictureLink600);
                }
            }
            PicturePageViewModel model = new()
            {
                PicturesList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy,
                TagFilter = tagFilter
            };
            string tList = "";
            foreach (string tstr in tagsList)
            {
                tList = tList + tstr + ",";
            }
            model.TagsList = tList.TrimEnd(',');

            return Ok(model);
        }

        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> PictureViewModelMobile(int id, int accessLevel=5, [FromQuery] int sortBy = 1)
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null)
            {
                return Unauthorized();
            }

            PictureViewModel model = new()
            {
                PictureId = picture.PictureId,
                PictureTime = picture.PictureTime,
                ProgenyId = picture.ProgenyId,
                Owners = picture.Owners,
                PictureLink = picture.PictureLink1200
            };
            if (!model.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                model.PictureLink = imageStore.UriFor(model.PictureLink);
            }
            model.AccessLevel = picture.AccessLevel;
            model.Author = picture.Author;
            model.CommentThreadNumber = picture.CommentThreadNumber;
            model.Tags = picture.Tags;
            model.Location = picture.Location;
            model.Latitude = picture.Latitude;
            model.Longtitude = picture.Longtitude;
            model.Altitude = picture.Altitude;
            model.PictureNumber = 1;
            model.PictureCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= userAccess.AccessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
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
                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
                model.PictureNumber = currentIndex + 1;
                model.PictureCount = pictureList.Count;
                if (currentIndex > 0)
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
                    (model.NextPicture, model.PrevPicture) = (model.PrevPicture, model.NextPicture);
                }

            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            model.TagsList = tagItems;
            return Ok(model);

        }

        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> PictureViewModelMaui(int id)
        {
            Picture picture = await picturesService.GetPicture(id);

            if (picture == null) return NotFound();

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

            if (userAccess == null)
            {
                return Unauthorized();
            }

            PictureViewModel model = new()
            {
                PictureId = picture.PictureId,
                PictureTime = picture.PictureTime,
                ProgenyId = picture.ProgenyId,
                Owners = picture.Owners,
                PictureLink = picture.PictureLink1200
            };
            if (!model.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                model.PictureLink = imageStore.UriFor(model.PictureLink);
            }
            model.AccessLevel = picture.AccessLevel;
            model.Author = picture.Author;
            model.CommentThreadNumber = picture.CommentThreadNumber;
            model.Tags = picture.Tags;
            model.Location = picture.Location;
            model.Latitude = picture.Latitude;
            model.Longtitude = picture.Longtitude;
            model.Altitude = picture.Altitude;
            model.PictureNumber = 1;
            model.PictureCount = 1;
            model.CommentsList = await commentsService.GetCommentsList(picture.CommentThreadNumber);
            model.TagsList = "";
            List<string> tagsList = [];
            List<Picture> pictureList = await picturesService.GetPicturesList(picture.ProgenyId);
            pictureList = [.. pictureList.Where(p => p.AccessLevel >= userAccess.AccessLevel).OrderBy(p => p.PictureTime)];
            if (pictureList.Count != 0)
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
                    if (string.IsNullOrEmpty(pic.Tags)) continue;

                    List<string> pvmTags = [.. pic.Tags.Split(',')];
                    foreach (string tagstring in pvmTags)
                    {
                        if (!tagsList.Contains(tagstring.TrimStart(' ', ',').TrimEnd(' ', ',')))
                        {
                            tagsList.Add(tagstring.TrimStart(' ', ',').TrimEnd(' ', ','));
                        }
                    }
                }
                model.PictureNumber = currentIndex + 1;
                model.PictureCount = pictureList.Count;
                if (currentIndex > 0)
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

                (model.NextPicture, model.PrevPicture) = (model.PrevPicture, model.NextPicture);
            }
            string tagItems = "[";
            if (tagsList.Count != 0)
            {
                foreach (string tagstring in tagsList)
                {
                    tagItems = tagItems + "'" + tagstring + "',";
                }

                tagItems = tagItems.Remove(tagItems.Length - 1);
                tagItems += "]";
            }

            model.TagsList = tagItems;
            return Ok(model);

        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadPicture([FromForm]IFormFile file)
        {
            string pictureLink;
            await using (Stream stream = file.OpenReadStream())
            {
                pictureLink = await imageStore.SaveImage(stream);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadProgenyPicture([FromForm]IFormFile file)
        {
            string pictureLink;

            using (MagickImage image = new(file.OpenReadStream()))
            {
                IExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    int rotation;
                    try
                    {
                        rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                        switch (rotation)
                        {
                            case 1:
                                rotation = 0;
                                break;
                            case 3:
                                rotation = 180;
                                break;
                            case 6:
                                rotation = 90;
                                break;
                            case 8:
                                rotation = 270;
                                break;

                        }
                    }
                    catch (ArgumentNullException)
                    {
                        rotation = 0;
                    }
                    catch (NullReferenceException)
                    {
                        rotation = 0;
                    }

                    if (rotation != 0)
                    {
                        image.Rotate(rotation);
                    }
                }

                int newWidth = (180 / image.Height) * image.Width;
                int newHeight = 180;
                if (image.Width > image.Height)
                {
                    newWidth = 180;
                    newHeight = (180 / image.Width) * image.Height;
                }
                image.Resize(newWidth, newHeight);
                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                pictureLink = await imageStore.SaveImage(memStream, BlobContainers.Progeny);
            }
            
            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadProfilePicture([FromForm]IFormFile file)
        {
            string pictureLink;

            using (MagickImage image = new(file.OpenReadStream()))
            {
                IExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    int rotation;
                    try
                    {
                        rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                        switch (rotation)
                        {
                            case 1:
                                rotation = 0;
                                break;
                            case 3:
                                rotation = 180;
                                break;
                            case 6:
                                rotation = 90;
                                break;
                            case 8:
                                rotation = 270;
                                break;

                        }
                    }
                    catch (ArgumentNullException)
                    {
                        rotation = 0;
                    }
                    catch (NullReferenceException)
                    {
                        rotation = 0;
                    }

                    if (rotation != 0)
                    {
                        image.Rotate(rotation);
                    }
                }

                int newWidth = (180 / image.Height) * image.Width;
                int newHeight = 180;
                if (image.Width > image.Height)
                {
                    newWidth = 180;
                    newHeight = (180 / image.Width) * image.Height;
                }
                image.Resize(newWidth, newHeight);
                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                pictureLink = await imageStore.SaveImage(memStream, BlobContainers.Profiles);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadFriendPicture([FromForm]IFormFile file)
        {
            string pictureLink;

            using (MagickImage image = new(file.OpenReadStream()))
            {
                IExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    int rotation;
                    try
                    {
                        rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                        switch (rotation)
                        {
                            case 1:
                                rotation = 0;
                                break;
                            case 3:
                                rotation = 180;
                                break;
                            case 6:
                                rotation = 90;
                                break;
                            case 8:
                                rotation = 270;
                                break;

                        }
                    }
                    catch (ArgumentNullException)
                    {
                        rotation = 0;
                    }
                    catch (NullReferenceException)
                    {
                        rotation = 0;
                    }

                    if (rotation != 0)
                    {
                        image.Rotate(rotation);
                    }
                }

                int newWidth = (180 / image.Height) * image.Width;
                int newHeight = 180;
                if (image.Width > image.Height)
                {
                    newWidth = 180;
                    newHeight = (180 / image.Width) * image.Height;
                }
                image.Resize(newWidth, newHeight);
                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                pictureLink = await imageStore.SaveImage(memStream, BlobContainers.Friends);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadContactPicture([FromForm]IFormFile file)
        {
            string pictureLink;

            using (MagickImage image = new(file.OpenReadStream()))
            {
                IExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    int rotation;
                    try
                    {
                        rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                        switch (rotation)
                        {
                            case 1:
                                rotation = 0;
                                break;
                            case 3:
                                rotation = 180;
                                break;
                            case 6:
                                rotation = 90;
                                break;
                            case 8:
                                rotation = 270;
                                break;

                        }
                    }
                    catch (ArgumentNullException)
                    {
                        rotation = 0;
                    }
                    catch (NullReferenceException)
                    {
                        rotation = 0;
                    }

                    if (rotation != 0)
                    {
                        image.Rotate(rotation);
                    }
                }

                int newWidth = (180 / image.Height) * image.Width;
                int newHeight = 180;
                if (image.Width > image.Height)
                {
                    newWidth = 180;
                    newHeight = (180 / image.Width) * image.Height;
                }
                image.Resize(newWidth, newHeight);
                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                pictureLink = await imageStore.SaveImage(memStream, BlobContainers.Contacts);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]")]
        [HttpPost]
        public async Task<IActionResult> UploadNoteImage([FromForm] IFormFile file)
        {
            string pictureLink;
            await using (Stream stream = file.OpenReadStream())
            {
                pictureLink = await imageStore.SaveImage(stream, BlobContainers.Notes);
                pictureLink = imageStore.UriFor(pictureLink, BlobContainers.Notes);
            }

            if (pictureLink != "")
            {
                return Ok(pictureLink);
            }

            return NoContent();
        }

        [Route("[action]/{id}")]
        [HttpGet]
        public IActionResult GetProfilePicture(string id)
        {
            string result = "";
            if (!string.IsNullOrEmpty(id))
            {
                if (!id.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
                {
                    result = imageStore.UriFor(id, "profiles");
                }
            }
            
            if (string.IsNullOrEmpty(result))
            {
                result = Constants.ProfilePictureUrl;
            }

            return Ok(result);
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            
            List<Picture> allItems = await picturesService.GetPicturesList(id);
            allItems = allItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (Picture picture in allItems)
            {
                if (!string.IsNullOrEmpty(picture.Location))
                {
                    if (!autoSuggestList.Contains(picture.Location))
                    {
                        autoSuggestList.Add(picture.Location);
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (!string.IsNullOrEmpty(video.Location))
                {
                    if (!autoSuggestList.Contains(video.Location))
                    {
                        autoSuggestList.Add(video.Location);
                    }
                }
            }
            
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }

        [Route("[action]/{id:int}/{accessLevel:int}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }


            List<Picture> allItems = await picturesService.GetPicturesList(id);
            allItems = allItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = [];
            foreach (Picture picture in allItems)
            {
                if (string.IsNullOrEmpty(picture.Tags)) continue;

                List<string> tagsList = [.. picture.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }

            List<Video> allVideos = await videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (string.IsNullOrEmpty(video.Tags)) continue;

                List<string> tagsList = [.. video.Tags.Split(',')];
                foreach (string tagString in tagsList)
                {
                    if (!autoSuggestList.Contains(tagString.Trim()))
                    {
                        autoSuggestList.Add(tagString.Trim());
                    }
                }
            }
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }

        

        // Download pictures to StorageBlob from Url
        [HttpGet]
        [Route("[action]/{pictureId:int}")]
        public async Task<IActionResult> DownloadPicture(int pictureId)
        {
            Picture picture = await picturesService.GetPicture(pictureId);
            if (picture != null && picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                // Check if user should be allowed access.
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await dataService.GetProgenyUserAccessForUser(picture.ProgenyId, userEmail);

                if (userAccess == null || userAccess.AccessLevel > 0)
                {
                    return Unauthorized();
                }

                await using (Stream stream = await GetStreamFromUrl(picture.PictureLink))
                {
                    picture.PictureLink = await imageStore.SaveImage(stream);
                }

                await using (Stream stream = await GetStreamFromUrl(picture.PictureLink600))
                {
                    picture.PictureLink600 = await imageStore.SaveImage(stream);
                }

                await using (Stream stream = await GetStreamFromUrl(picture.PictureLink1200))
                {
                    picture.PictureLink1200 = await imageStore.SaveImage(stream);
                }

                picture = await picturesService.UpdatePicture(picture);
                return Ok(picture);
            }
            else
            {
                return NotFound();
            }
        }

        private static async Task<Stream> GetStreamFromUrl(string url)
        {
            using HttpClient client = new();
            using HttpResponseMessage response = await client.GetAsync(url);
            
            return await response.Content.ReadAsStreamAsync();
        }
    }
}
