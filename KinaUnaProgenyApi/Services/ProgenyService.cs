using System.IO;
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
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }


        public async Task<Progeny> GetProgeny(int id)
        {
            Progeny progeny = await GetProgenyFromCache(id);
            if (progeny == null || progeny.Id == 0)
            {
                progeny = await SetProgenyInCache(id);
            }

            return progeny;
        }
        public async Task<Progeny> AddProgeny(Progeny progeny)
        {
            await _context.ProgenyDb.AddAsync(progeny);
            await _context.SaveChangesAsync();

            await SetProgenyInCache(progeny.Id);

            return progeny;
        }

        public async Task<Progeny> UpdateProgeny(Progeny progeny)
        {
            Progeny progenyToUpdate = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);
            if (progenyToUpdate != null)
            {
                progenyToUpdate.Admins = progeny.Admins;
                progenyToUpdate.BirthDay = progeny.BirthDay;
                progenyToUpdate.Name = progeny.Name;
                progenyToUpdate.NickName = progeny.NickName;
                progenyToUpdate.PictureLink = progeny.PictureLink;
                progenyToUpdate.TimeZone = progeny.TimeZone;

                _context.ProgenyDb.Update(progenyToUpdate);
                await _context.SaveChangesAsync();

                await SetProgenyInCache(progeny.Id);
            }

            return progenyToUpdate;
        }

        public async Task<Progeny> DeleteProgeny(Progeny progeny)
        {
            await RemoveProgenyFromCache(progeny.Id);

            Progeny progenyToDelete = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);
            if (progenyToDelete == null) return null;

            _context.ProgenyDb.Remove(progenyToDelete);
            await _context.SaveChangesAsync();

            return progenyToDelete;
        }

        private async Task<Progeny> GetProgenyFromCache(int id)
        {
            Progeny progeny = new();
            string cachedProgeny = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            if (!string.IsNullOrEmpty(cachedProgeny))
            {
                progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            }

            return progeny;
        }

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

        private async Task RemoveProgenyFromCache(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
        }

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

            await _imageStore.DeleteImage(imageId, BlobContainers.Progeny);

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
