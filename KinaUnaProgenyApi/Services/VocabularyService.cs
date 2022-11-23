using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class VocabularyService: IVocabularyService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public VocabularyService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }

        public async Task<VocabularyItem> GetVocabularyItem(int id)
        {
            VocabularyItem vocabularyItem = await GetVocabularyItemFromCache(id);
            if (vocabularyItem == null || vocabularyItem.WordId == 0)
            {
                vocabularyItem = await SetVocabularyItemInCache(id);
            }

            return vocabularyItem;
        }

        public async Task<VocabularyItem> AddVocabularyItem(VocabularyItem vocabularyItem)
        {
            _ = _context.VocabularyDb.Add(vocabularyItem);
            _ = await _context.SaveChangesAsync();
            _ = await SetVocabularyItemInCache(vocabularyItem.WordId);

            return vocabularyItem;
        }

        private async Task<VocabularyItem> GetVocabularyItemFromCache(int id)
        {
            VocabularyItem vocabularyItem = new VocabularyItem();
            string cachedVocabularyItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);
            if (!string.IsNullOrEmpty(cachedVocabularyItem))
            {
                vocabularyItem = JsonConvert.DeserializeObject<VocabularyItem>(cachedVocabularyItem);
            }

            return vocabularyItem;
        }

        private async Task<VocabularyItem> SetVocabularyItemInCache(int id)
        {
            VocabularyItem vocabularyItem = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            if (vocabularyItem != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id, JsonConvert.SerializeObject(vocabularyItem), _cacheOptions);

                _ = await SetVocabularyListInCache(vocabularyItem.ProgenyId);
            }

            return vocabularyItem;
        }

        public async Task<VocabularyItem> UpdateVocabularyItem(VocabularyItem vocabularyItem)
        {
            VocabularyItem vocabularyItemToUpdate = await _context.VocabularyDb.SingleOrDefaultAsync(v => v.WordId == vocabularyItem.WordId);
            if (vocabularyItemToUpdate != null)
            {
                vocabularyItemToUpdate.AccessLevel = vocabularyItem.AccessLevel;
                vocabularyItemToUpdate.ProgenyId = vocabularyItem.ProgenyId;
                vocabularyItemToUpdate.Author = vocabularyItem.Author;
                vocabularyItemToUpdate.Description = vocabularyItem.Description;
                vocabularyItemToUpdate.Date = vocabularyItem.Date;
                vocabularyItemToUpdate.Language = vocabularyItem.Language;
                vocabularyItemToUpdate.DateAdded = vocabularyItem.DateAdded;
                vocabularyItemToUpdate.SoundsLike = vocabularyItem.SoundsLike;
                vocabularyItemToUpdate.Word = vocabularyItem.Word;
                vocabularyItemToUpdate.VocabularyItemNumber = vocabularyItem.VocabularyItemNumber;
                vocabularyItemToUpdate.Progeny = vocabularyItem.Progeny;

                _ = _context.VocabularyDb.Update(vocabularyItemToUpdate);
                _ = await _context.SaveChangesAsync();
                _ = await SetVocabularyItemInCache(vocabularyItemToUpdate.WordId);
            }
            

            return vocabularyItem;
        }

        public async Task<VocabularyItem> DeleteVocabularyItem(VocabularyItem vocabularyItem)
        {
            VocabularyItem vocabularyItemToDelete = await _context.VocabularyDb.SingleOrDefaultAsync(v => v.WordId == vocabularyItem.WordId);
            if (vocabularyItemToDelete != null)
            {
                _ = _context.VocabularyDb.Remove(vocabularyItemToDelete);
                _ = await _context.SaveChangesAsync();
                await RemoveVocabularyItemFromCache(vocabularyItem.WordId, vocabularyItem.ProgenyId);
            }
            
            return vocabularyItem;
        }

        public async Task RemoveVocabularyItemFromCache(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);

            _ = await SetVocabularyListInCache(progenyId);
        }

        public async Task<List<VocabularyItem>> GetVocabularyList(int progenyId)
        {
            List<VocabularyItem> vocabularyList = await GetVocabularyListFromCache(progenyId);
            if (!vocabularyList.Any())
            {
                vocabularyList = await SetVocabularyListInCache(progenyId);
            }

            return vocabularyList;
        }

        private async Task<List<VocabularyItem>> GetVocabularyListFromCache(int progenyId)
        {
            List<VocabularyItem> vocabularyList = new List<VocabularyItem>();
            string cachedVocabularyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(cachedVocabularyList);
            }

            return vocabularyList;
        }

        private async Task<List<VocabularyItem>> SetVocabularyListInCache(int progenyId)
        {
            List<VocabularyItem> vocabularyList = await _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId, JsonConvert.SerializeObject(vocabularyList), _cacheOptionsSliding);

            return vocabularyList;
        }
    }
}
