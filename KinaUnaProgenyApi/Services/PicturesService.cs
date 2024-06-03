using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Extensions.ThirdPartyElements;
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
                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                picture.PictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Pictures, image.FileExtensionString());
            }

            MemoryStream memoryStream600 = await _imageStore.GetStream(picture.PictureLink600);
            memoryStream600.Position = 0;

            using (MagickImage image600 = new(memoryStream600))
            {
                using MemoryStream memStream600 = new();
                await image600.WriteAsync(memStream600);
                memStream600.Position = 0;
                picture.PictureLink600 = await _imageStore.SaveImage(memStream600, BlobContainers.Pictures, image600.FileExtensionString());
            }

            MemoryStream memoryStream1200 = await _imageStore.GetStream(picture.PictureLink1200);
            memoryStream1200.Position = 0;

            using (MagickImage image1200 = new(memoryStream1200))
            {
                using MemoryStream memStream1200 = new();
                await image1200.WriteAsync(memStream1200);
                memStream1200.Position = 0;
                picture.PictureLink1200 = await _imageStore.SaveImage(memStream1200, BlobContainers.Pictures, image1200.FileExtensionString());
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
                IExifProfile profile = image.GetExifProfile();
                if (profile != null)
                {
                    picture.Longtitude = profile.GetLongitude();
                    picture.Latitude = profile.GetLatitude();
                    picture.Altitude = profile.GetAltitude();

                    picture.PictureRotation = profile.GetRotationInDegrees();

                    picture.PictureTime = profile.GetDateTime();

                    picture.PictureWidth = profile.GetPictureWidth(image);
                    picture.PictureHeight = profile.GetPictureHeight(image);
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
                        image.Rotate(-(int)picture.PictureRotation);
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
                
                picture.PictureLink600 = await _imageStore.SaveImage(memStream, BlobContainers.Pictures, image.FileExtensionString());
            }

            using (MagickImage image = new(memoryStream))
            {
                if (picture.PictureRotation != null)
                {
                    if (picture.PictureRotation != 0)
                    {
                        image.Rotate(-(int)picture.PictureRotation);
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
                picture.PictureLink1200 = await _imageStore.SaveImage(memStream,BlobContainers.Pictures, image.FileExtensionString());
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
                int rotation = profile.GetRotationInDegrees();
                
                if (rotation != 0)
                {
                    image.Rotate(-rotation);
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
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Progeny, image.FileExtensionString());

            return pictureLink;
        }

        public async Task<string> ProcessProfilePicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();
                
                if (rotation != 0)
                {
                    image.Rotate(-rotation);
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
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Profiles, image.FileExtensionString());

            return pictureLink;
        }

        public async Task<string> ProcessFriendPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();

                if (rotation != 0)
                {
                    image.Rotate(-rotation);
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
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Friends, image.FileExtensionString());

            return pictureLink;
        }

        public async Task<string> ProcessContactPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();

                if (rotation != 0)
                {
                    image.Rotate(-rotation);
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
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Contacts, image.FileExtensionString());
            
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

            if (!picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) && !picture.PictureLink.Contains('.')) // Some pictures do not have file extensions. If they don't, update the links.
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
