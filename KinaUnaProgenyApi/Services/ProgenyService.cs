using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class ProgenyService : IProgenyService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();
        private readonly IImageStore _imageStore;
        
        public ProgenyService(ProgenyDbContext context, IDistributedCache cache, IImageStore imageStore)
        {
            _context = context;
            _imageStore = imageStore;
            _cache = cache;
            _ = _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _ = _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        /// <summary>
        /// Gets a Progeny by Id.
        /// First tries to get the Progeny from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The Id of the Progeny to get.</param>
        /// <returns>The Progeny with the given Id. Null if the Progeny doesn't exist.</returns>
        public async Task<Progeny> GetProgeny(int id)
        {
            Progeny progeny = await GetProgenyFromCache(id);
            if (progeny == null || progeny.Id == 0)
            {
                progeny = await SetProgenyInCache(id);
            }

            return progeny;
        }

        /// <summary>
        /// Adds a new Progeny to the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny to add.</param>
        /// <returns>The added Progeny.</returns>
        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            _ = _context.ProgenyDb.Add(progeny);
            _ = await _context.SaveChangesAsync();

            _ = await SetProgenyInCache(progeny.Id);

            return progeny;
        }

        /// <summary>
        /// Updates a Progeny in the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny with updated properties.</param>
        /// <returns>The updated Progeny.</returns>
        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            Progeny progenyToUpdate = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);
            if (progenyToUpdate == null) return null;

            string oldPictureLink = progenyToUpdate.PictureLink;
            
            progenyToUpdate.Admins = progeny.Admins;
            progenyToUpdate.BirthDay = progeny.BirthDay;
            progenyToUpdate.Name = progeny.Name;
            progenyToUpdate.NickName = progeny.NickName;
            progenyToUpdate.TimeZone = progeny.TimeZone;
            if (!string.IsNullOrEmpty(progeny.PictureLink))
            {
                progenyToUpdate.PictureLink = progeny.PictureLink;
            }

            if (!progenyToUpdate.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                progenyToUpdate.PictureLink = await ResizeImage(progenyToUpdate.PictureLink);
            }

            _ = _context.ProgenyDb.Update(progenyToUpdate);
            _ = await _context.SaveChangesAsync();

            if (oldPictureLink != progeny.PictureLink)
            {
                List<Progeny> progeniesWithThisPicture = await _context.ProgenyDb.AsNoTracking().Where(c => c.PictureLink == oldPictureLink).ToListAsync();
                if (progeniesWithThisPicture.Count == 0)
                {
                    _ = await _imageStore.DeleteImage(oldPictureLink, BlobContainers.Progeny);
                }
            }

            _ = await SetProgenyInCache(progeny.Id);

            return progenyToUpdate;
        }

        /// <summary>
        /// Deletes a Progeny from the database and the cache.
        /// </summary>
        /// <param name="progeny">The Progeny to delete.</param>
        /// <returns>The deleted Progeny.</returns>
        public async Task<Progeny> DeleteProgeny(Progeny progeny)
        {
            await RemoveProgenyFromCache(progeny.Id);

            Progeny progenyToDelete = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);
            if (progenyToDelete == null) return null;

            _ = _context.ProgenyDb.Remove(progenyToDelete);
            _ = await _context.SaveChangesAsync();

            _ = await _imageStore.DeleteImage(progeny.PictureLink, "progeny");
            return progenyToDelete;
        }

        /// <summary>
        /// Gets a Progeny by Id from the cache.
        /// </summary>
        /// <param name="id">The Id of the Progeny to get.</param>
        /// <returns>The Progeny with the given Id. Null if the Progeny isn't found in the cache.</returns>
        private async Task<Progeny> GetProgenyFromCache(int id)
        {
            string cachedProgeny = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            if (string.IsNullOrEmpty(cachedProgeny))
            {
                return null;
            }
            
            Progeny progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            return progeny;
        }

        /// <summary>
        /// Gets a Progeny by Id from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The Id of the Progeny to get and set.</param>
        /// <returns>The Progeny with the given Id. Null if the Progeny doesn't exist.</returns>
        private async Task<Progeny> SetProgenyInCache(int id)
        {
            Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            if (progeny != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }
            else
            {
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            }

            return progeny;
        }

        /// <summary>
        /// Removes a Progeny from the cache.
        /// </summary>
        /// <param name="id">The Id of the Progeny to remove.</param>
        /// <returns></returns>
        private async Task RemoveProgenyFromCache(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
        }

        /// <summary>
        /// Resizes a Progeny profile picture and saves it to the image store.
        /// </summary>
        /// <param name="imageId">The current file name.</param>
        /// <returns>The new file name of the resized image.</returns>
        public async Task<string> ResizeImage(string imageId)
        {
            MemoryStream memoryStream = await _imageStore.GetStream(imageId, BlobContainers.Progeny);
            memoryStream.Position = 0;
            const int maxWidthAndHeight = 250;
            using MagickImage image = new(memoryStream);
            if (image.Width <= maxWidthAndHeight && image.Height <= maxWidthAndHeight)
            {
                return imageId;
            }

            if (image.Width > maxWidthAndHeight)
            {
                int newHeight = (maxWidthAndHeight / image.Width) * image.Height;

                image.Resize(maxWidthAndHeight, newHeight);
            }

            if (image.Height > maxWidthAndHeight)
            {
                int newWidth = (maxWidthAndHeight / image.Width) * image.Height;

                image.Resize(newWidth, maxWidthAndHeight);
            }

            image.Strip();

            using MemoryStream memStream = new();
            await image.WriteAsync(memStream);
            memStream.Position = 0;

            _ = await _imageStore.DeleteImage(imageId, BlobContainers.Progeny);

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

            imageId = await _imageStore.SaveImage(memStream, BlobContainers.Progeny, pictureFormat);

            return imageId;
        }
    }
}
