using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class PicturesService : IPicturesService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();
        private readonly IImageStore _imageStore;

        public PicturesService(MediaDbContext mediaContext, IDistributedCache cache, IImageStore imageStore)
        {
            _mediaContext = mediaContext;
            _imageStore = imageStore;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }

        public async Task<Picture> GetPicture(int id)
        {
            Picture picture = await GetPictureFromCache(id);
            if (picture == null || picture.PictureId == 0)
            {
                picture = await SetPictureInCache(id);
            }

            if (picture == null) return null;
            
            if (picture.PictureRotation != null)
            {
                return picture;
            }

            picture.PictureRotation = 0;
            await UpdatePicture(picture);

            return picture;
        }

        private async Task<Picture> GetPictureFromCache(int id)
        {
            Picture picture = new();
            string cachedPicture = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "picture" + id);
            if (!string.IsNullOrEmpty(cachedPicture))
            {
                picture = JsonConvert.DeserializeObject<Picture>(cachedPicture);
            }

            return picture;
        }

        public async Task<Picture> AddPicture(Picture picture)
        {
            picture.RemoveNullStrings();
            Picture pictureToAdd = new();
            pictureToAdd.CopyPropertiesForAdd(picture);
            await _mediaContext.PicturesDb.AddAsync(pictureToAdd);
            await _mediaContext.SaveChangesAsync();

            await SetPictureInCache(pictureToAdd.PictureId);

            return pictureToAdd;
        }

        public async Task<Picture> UpdatePictureLinkWithExtension(Picture picture)
        {
            string originalPictureLink = picture.PictureLink;
            string originalPictureLink600 = picture.PictureLink600;
            string originalPictureLink1200 = picture.PictureLink1200;

            MemoryStream memoryStream = await _imageStore.GetStream(picture.PictureLink);
            memoryStream.Position = 0;

            using (MagickImage image = new(memoryStream))
            {
                string pictureFormat = "";
                switch (image.Format)
                {
                    case MagickFormat.Jpg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Jpeg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Png:
                        pictureFormat += ".png";
                        break;
                    case MagickFormat.Gif:
                        pictureFormat += ".gif";
                        break;
                    case MagickFormat.Bmp:
                        pictureFormat += ".bmp";
                        break;
                    case MagickFormat.Tiff:
                        pictureFormat += ".tiff";
                        break;
                    case MagickFormat.Tga:
                        pictureFormat += ".tga";
                        break;

                    default:
                        pictureFormat = "";
                        break;
                }

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                picture.PictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Pictures, pictureFormat);
            }

            MemoryStream memoryStream600 = await _imageStore.GetStream(picture.PictureLink600);
            memoryStream600.Position = 0;

            using (MagickImage image600 = new(memoryStream600))
            {
                string pictureFormat = "";
                switch (image600.Format)
                {
                    case MagickFormat.Jpg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Jpeg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Png:
                        pictureFormat += ".png";
                        break;
                    case MagickFormat.Gif:
                        pictureFormat += ".gif";
                        break;
                    case MagickFormat.Bmp:
                        pictureFormat += ".bmp";
                        break;
                    case MagickFormat.Tiff:
                        pictureFormat += ".tiff";
                        break;
                    case MagickFormat.Tga:
                        pictureFormat += ".tga";
                        break;

                    default:
                        pictureFormat = "";
                        break;
                }

                using MemoryStream memStream600 = new();
                await image600.WriteAsync(memStream600);
                memStream600.Position = 0;
                picture.PictureLink600 = await _imageStore.SaveImage(memStream600, BlobContainers.Pictures, pictureFormat);
            }

            MemoryStream memoryStream1200 = await _imageStore.GetStream(picture.PictureLink1200);
            memoryStream1200.Position = 0;

            using (MagickImage image1200 = new(memoryStream1200))
            {
                string pictureFormat = "";
                switch (image1200.Format)
                {
                    case MagickFormat.Jpg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Jpeg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Png:
                        pictureFormat += ".png";
                        break;
                    case MagickFormat.Gif:
                        pictureFormat += ".gif";
                        break;
                    case MagickFormat.Bmp:
                        pictureFormat += ".bmp";
                        break;
                    case MagickFormat.Tiff:
                        pictureFormat += ".tiff";
                        break;
                    case MagickFormat.Tga:
                        pictureFormat += ".tga";
                        break;

                    default:
                        pictureFormat = "";
                        break;
                }

                using MemoryStream memStream1200 = new();
                await image1200.WriteAsync(memStream1200);
                memStream1200.Position = 0;
                picture.PictureLink1200 = await _imageStore.SaveImage(memStream1200, BlobContainers.Pictures, pictureFormat);
            }

            picture = await UpdatePicture(picture);
            await _imageStore.DeleteImage(originalPictureLink, BlobContainers.Pictures);
            await _imageStore.DeleteImage(originalPictureLink600, BlobContainers.Pictures);
            await _imageStore.DeleteImage(originalPictureLink1200, BlobContainers.Pictures);

            return picture;
        }

        public async Task<Picture> ProcessPicture(Picture picture)
        {
            MemoryStream memoryStream = await _imageStore.GetStream(picture.PictureLink);
            memoryStream.Position = 0;

            using (MagickImage image = new(memoryStream))
            {
                string pictureFormat = "";
                switch (image.Format)
                {
                    case MagickFormat.Jpg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Jpeg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Png:
                        pictureFormat += ".png";
                        break;
                    case MagickFormat.Gif:
                        pictureFormat += ".gif";
                        break;
                    case MagickFormat.Bmp:
                        pictureFormat += ".bmp";
                        break;
                    case MagickFormat.Tiff:
                        pictureFormat += ".tiff";
                        break;
                    case MagickFormat.Tga:
                        pictureFormat += ".tga";
                        break;

                    default:
                        pictureFormat = "";
                        break;
                }

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
                            if (gpsLongtitude.GetValue() is Rational[] longValues && (longValues[0].Denominator != 0 && longValues[1].Denominator != 0 &&
                                                       longValues[2].Denominator != 0))
                            {
                                double long0 = longValues[0].Numerator / (double)longValues[0].Denominator;
                                double long1 = longValues[1].Numerator / (double)longValues[1].Denominator;
                                double long2 = longValues[2].Numerator / (double)longValues[2].Denominator;
                                picture.Longtitude = (long0 + long1 / 60.0 + long2 / 3600).ToString(CultureInfo.CurrentCulture);
                            }
                            else
                            {
                                picture.Longtitude = "";
                            }

                            if (gpsLatitude.GetValue() is Rational[] latValues && (latValues[0].Denominator != 0 && latValues[1].Denominator != 0 &&
                                                      latValues[2].Denominator != 0))
                            {
                                double lat0 = latValues[0].Numerator / (double)latValues[0].Denominator;
                                double lat1 = latValues[1].Numerator / (double)latValues[1].Denominator;
                                double lat2 = latValues[2].Numerator / (double)latValues[2].Denominator;
                                picture.Latitude = (lat0 + lat1 / 60.0 + lat2 / 3600).ToString(CultureInfo.CurrentCulture);
                            }
                            else
                            {
                                picture.Latitude = "";
                            }
                        }
                        else
                        {
                            picture.Longtitude = "";
                            picture.Latitude = "";
                        }

                        if (gpsAltitude != null)
                        {
                            Rational altValues = (Rational)gpsAltitude.GetValue();
                            if (altValues.Denominator != 0)
                            {
                                double alt0 = altValues.Numerator / (double)altValues.Denominator;
                                picture.Altitude = alt0.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                picture.Altitude = "";
                            }
                        }
                        else
                        {
                            picture.Altitude = "";
                        }


                    }
                    catch (ArgumentNullException)
                    {
                        picture.Longtitude = "";
                        picture.Latitude = "";
                        picture.Altitude = "";
                    }
                    catch (NullReferenceException)
                    {
                        picture.Longtitude = "";
                        picture.Latitude = "";
                        picture.Altitude = "";
                    }
                    catch (Exception)
                    {
                        picture.Longtitude = "";
                        picture.Latitude = "";
                        picture.Altitude = "";
                    }

                    try
                    {
                        int rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                        switch (rotation)
                        {
                            case 1:
                                picture.PictureRotation = 0;
                                break;
                            case 3:
                                picture.PictureRotation = 180;
                                break;
                            case 6:
                                picture.PictureRotation = 90;
                                break;
                            case 8:
                                picture.PictureRotation = 270;
                                break;

                        }
                    }
                    catch (ArgumentNullException)
                    {
                        picture.PictureRotation = 0;
                    }
                    catch (NullReferenceException)
                    {
                        picture.PictureRotation = 0;
                    }
                    
                    try
                    {
                        string date = profile.GetValue(ExifTag.DateTimeOriginal)?.Value;
                        if (!string.IsNullOrEmpty(date))
                        {
                            picture.PictureTime = new DateTime(
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
                        picture.PictureTime = null;
                    }
                    catch (OverflowException)
                    {
                        picture.PictureTime = null;
                    }
                    catch (ArgumentNullException)
                    {
                        picture.PictureTime = null;
                    }
                    catch (NullReferenceException)
                    {
                        picture.PictureTime = null;
                    }

                    try
                    {
                        Number w = profile.GetValue(ExifTag.PixelXDimension).Value;
                        Number h = profile.GetValue(ExifTag.PixelYDimension).Value;

                        picture.PictureWidth = Convert.ToInt32((uint)w);
                        picture.PictureHeight = Convert.ToInt32((uint)h);
                    }
                    catch (FormatException)
                    {
                        picture.PictureWidth = image.Width;
                        picture.PictureHeight = image.Height;
                    }
                    catch (OverflowException)
                    {
                        picture.PictureWidth = image.Width;
                        picture.PictureHeight = image.Height;
                    }
                    catch (ArgumentNullException)
                    {
                        picture.PictureWidth = image.Width;
                        picture.PictureHeight = image.Height;
                    }
                    catch (NullReferenceException)
                    {
                        picture.PictureWidth = image.Width;
                        picture.PictureHeight = image.Height;
                    }
                }
                else
                {
                    picture.PictureWidth = image.Width;
                    picture.PictureHeight = image.Height;

                }
                if (picture.PictureRotation != null)
                {
                    if (picture.PictureRotation != 0)
                    {
                        image.Rotate((int)picture.PictureRotation);
                    }

                }

                if (picture.PictureWidth > 600)
                {
                    const int newWidth = 600;
                    int newHeight = (600 / picture.PictureWidth) * picture.PictureHeight;

                    image.Resize(newWidth, newHeight);
                }

                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                
                picture.PictureLink600 = await _imageStore.SaveImage(memStream, BlobContainers.Pictures, pictureFormat);
            }

            using (MagickImage image = new(memoryStream))
            {
                string pictureFormat = "";
                switch (image.Format)
                {
                    case MagickFormat.Jpg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Jpeg:
                        pictureFormat += ".jpg";
                        break;
                    case MagickFormat.Png:
                        pictureFormat += ".png";
                        break;
                    case MagickFormat.Gif:
                        pictureFormat += ".gif";
                        break;
                    case MagickFormat.Bmp:
                        pictureFormat += ".bmp";
                        break;
                    case MagickFormat.Tiff:
                        pictureFormat += ".tiff";
                        break;
                    case MagickFormat.Tga:
                        pictureFormat += ".tga";
                        break;

                    default:
                        pictureFormat = "";
                        break;
                }

                if (picture.PictureRotation != null)
                {
                    if (picture.PictureRotation != 0)
                    {
                        image.Rotate((int)picture.PictureRotation);
                    }

                }

                if (picture.PictureWidth > 1200)
                {
                    const int newWidth = 1200;
                    int newHeight = (1200 / picture.PictureWidth) * picture.PictureHeight;

                    image.Resize(newWidth, newHeight);
                }

                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                picture.PictureLink1200 = await _imageStore.SaveImage(memStream,BlobContainers.Pictures, pictureFormat);
            }

            if (picture.PictureTime != null)
            {
                picture.PictureTime = TimeZoneInfo.ConvertTimeToUtc(picture.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(picture.TimeZone));
            }

            if (picture.Longtitude == "" || picture.Latitude == "") return picture;

            if (string.IsNullOrEmpty(picture.Location))
            {
                picture.Location = picture.Latitude + ", " + picture.Longtitude;
            }

            return picture;
        }

        public async Task<string> ProcessProgenyPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            
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

            string pictureFormat = "";
            switch (image.Format)
            {
                case MagickFormat.Jpg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Jpeg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Png:
                    pictureFormat += ".png";
                    break;
                case MagickFormat.Gif:
                    pictureFormat += ".gif";
                    break;
                case MagickFormat.Bmp:
                    pictureFormat += ".bmp";
                    break;
                case MagickFormat.Tiff:
                    pictureFormat += ".tiff";
                    break;
                case MagickFormat.Tga:
                    pictureFormat += ".tga";
                    break;

                default:
                    pictureFormat = "";
                    break;
            }

            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Progeny, pictureFormat);

            return pictureLink;
        }

        public async Task<string> ProcessProfilePicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
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

            string pictureFormat = "";
            switch (image.Format)
            {
                case MagickFormat.Jpg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Jpeg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Png:
                    pictureFormat += ".png";
                    break;
                case MagickFormat.Gif:
                    pictureFormat += ".gif";
                    break;
                case MagickFormat.Bmp:
                    pictureFormat += ".bmp";
                    break;
                case MagickFormat.Tiff:
                    pictureFormat += ".tiff";
                    break;
                case MagickFormat.Tga:
                    pictureFormat += ".tga";
                    break;

                default:
                    pictureFormat = "";
                    break;
            }

            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Profiles, pictureFormat);

            return pictureLink;
        }

        public async Task<string> ProcessFriendPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
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

            string pictureFormat = "";
            switch (image.Format)
            {
                case MagickFormat.Jpg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Jpeg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Png:
                    pictureFormat += ".png";
                    break;
                case MagickFormat.Gif:
                    pictureFormat += ".gif";
                    break;
                case MagickFormat.Bmp:
                    pictureFormat += ".bmp";
                    break;
                case MagickFormat.Tiff:
                    pictureFormat += ".tiff";
                    break;
                case MagickFormat.Tga:
                    pictureFormat += ".tga";
                    break;

                default:
                    pictureFormat = "";
                    break;
            }

            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Friends, pictureFormat);

            return pictureLink;
        }

        public async Task<string> ProcessContactPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
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

            string pictureFormat = "";
            switch (image.Format)
            {
                case MagickFormat.Jpg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Jpeg:
                    pictureFormat += ".jpg";
                    break;
                case MagickFormat.Png:
                    pictureFormat += ".png";
                    break;
                case MagickFormat.Gif:
                    pictureFormat += ".gif";
                    break;
                case MagickFormat.Bmp:
                    pictureFormat += ".bmp";
                    break;
                case MagickFormat.Tiff:
                    pictureFormat += ".tiff";
                    break;
                case MagickFormat.Tga:
                    pictureFormat += ".tga";
                    break;

                default:
                    pictureFormat = "";
                    break;
            }

            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Contacts, pictureFormat);
            
            return pictureLink;
        }
        public async Task<Picture> GetPictureByLink(string link)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureLink == link);
            return picture;
        }

        public async Task<Picture> SetPictureInCache(int id)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            if (picture == null) return null;

            if (picture.Tags != null && picture.Tags.Trim().EndsWith(','))
            {
                picture.Tags = picture.Tags.Trim().TrimEnd(',');
                _ = await UpdatePicture(picture);
            }

            if (!picture.PictureLink.ToLower().StartsWith("http") && !picture.PictureLink.Contains('.')) // Some pictures do not have file extensions. If they don't, update the links.
            {
                try
                {
                    picture = await UpdatePictureLinkWithExtension(picture);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);

            _ = await SetPicturesListInCache(picture.ProgenyId);

            return picture;
        }

        public async Task<Picture> UpdatePicture(Picture picture)
        {
            picture.RemoveNullStrings();
            Picture pictureToUpdate = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == picture.PictureId);
            if (pictureToUpdate == null) return null;

            pictureToUpdate.CopyPropertiesForUpdate(picture);

            _ = _mediaContext.PicturesDb.Update(pictureToUpdate);
            _ = await _mediaContext.SaveChangesAsync();
            _ = await SetPictureInCache(pictureToUpdate.PictureId);

            return pictureToUpdate;

        }

        public async Task<Picture> DeletePicture(Picture picture)
        {
            Picture pictureToDelete = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == picture.PictureId);
            if (pictureToDelete != null)
            {
                _mediaContext.PicturesDb.Remove(pictureToDelete);
                _ = await _mediaContext.SaveChangesAsync();
            }


            await RemovePictureFromCache(picture.PictureId, picture.ProgenyId);
            return pictureToDelete;
        }
        public async Task RemovePictureFromCache(int pictureId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "picture" + pictureId);

            _ = await SetPicturesListInCache(progenyId);
        }

        public async Task<List<Picture>> GetPicturesList(int progenyId)
        {
            List<Picture> picturesList = await GetPicturesListFromCache(progenyId);
            if (picturesList.Count == 0)
            {
                picturesList = await SetPicturesListInCache(progenyId);
            }

            return picturesList;
        }

        private async Task<List<Picture>> GetPicturesListFromCache(int progenyId)
        {
            List<Picture> picturesList = [];
            string cachedPicturesList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "pictureslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedPicturesList))
            {
                picturesList = JsonConvert.DeserializeObject<List<Picture>>(cachedPicturesList);
            }

            return picturesList;
        }

        public async Task<List<Picture>> SetPicturesListInCache(int progenyId)
        {
            List<Picture> picturesList = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "pictureslist" + progenyId, JsonConvert.SerializeObject(picturesList), _cacheOptionsSliding);

            return picturesList;
        }

        public async Task UpdateAllPictures()
        {
            List<Picture> allPicturesList = await _mediaContext.PicturesDb.ToListAsync();
            foreach (Picture picture in allPicturesList)
            {
                picture.RemoveNullStrings();
                _ = await UpdatePicture(picture);
            }
        }
    }
}
