using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class PicturesService: IPicturesService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public PicturesService(MediaDbContext mediaContext, IDistributedCache cache)
        {
            _mediaContext = mediaContext;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }
        
        public async Task<Picture> GetPicture(int id)
        {
            Picture picture = await GetPictureFromCache(id);
            if (picture == null || picture.PictureId == 0)
            {
                picture = await SetPictureInCache(id);
            }
            
            if (picture != null && picture.PictureRotation == null)
            {
                picture.PictureRotation = 0;
                await UpdatePicture(picture);
            }
            
            return picture;
        }

        private async Task<Picture> GetPictureFromCache(int id)
        {
            Picture picture = new Picture();
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
            await _mediaContext.PicturesDb.AddAsync(picture);
            await _mediaContext.SaveChangesAsync();

            await SetPictureInCache(picture.PictureId);

            return picture;
        }

        public async Task<Picture> GetPictureByLink(string link)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureLink == link);
            return picture;
        }

        public async Task<Picture> SetPictureInCache(int id)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);
            if (picture != null)
            {
                _ = await SetPicturesListInCache(picture.ProgenyId);
            }

            return picture;
        }

        public async Task<Picture> UpdatePicture(Picture picture)
        {
            picture.RemoveNullStrings();
            Picture pictureToUpdate = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == picture.PictureId);
            if (pictureToUpdate != null)
            {
                pictureToUpdate.AccessLevel = picture.AccessLevel;
                pictureToUpdate.CommentThreadNumber = picture.CommentThreadNumber;
                pictureToUpdate.ProgenyId = picture.ProgenyId;
                pictureToUpdate.PictureLink = picture.PictureLink;
                pictureToUpdate.PictureLink600 = picture.PictureLink600;
                pictureToUpdate.PictureLink1200 = picture.PictureLink1200;
                pictureToUpdate.Latitude = picture.Latitude;
                pictureToUpdate.Longtitude = picture.Longtitude;
                pictureToUpdate.Altitude = picture.Altitude;
                pictureToUpdate.Author = picture.Author;
                pictureToUpdate.Location = picture.Location;
                pictureToUpdate.Tags = picture.Tags;
                pictureToUpdate.TimeZone = picture.TimeZone;
                pictureToUpdate.PictureTime = picture.PictureTime;
                pictureToUpdate.Owners = picture.Owners;
                pictureToUpdate.PictureHeight = picture.PictureHeight;
                pictureToUpdate.PictureWidth = picture.PictureWidth;
                pictureToUpdate.PictureRotation = picture.PictureRotation;
                pictureToUpdate.PictureNumber = picture.PictureNumber;
                pictureToUpdate.Progeny = picture.Progeny;
                pictureToUpdate.Comments = picture.Comments;

                _ = _mediaContext.PicturesDb.Update(pictureToUpdate);
                _ = await _mediaContext.SaveChangesAsync();
            }

            _ = await SetPictureInCache(picture.PictureId);
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
            if (!picturesList.Any())
            {
                picturesList = await SetPicturesListInCache(progenyId);
            }

            return picturesList;
        }

        private async Task<List<Picture>> GetPicturesListFromCache(int progenyId)
        {
            List<Picture> picturesList = new List<Picture>();
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
