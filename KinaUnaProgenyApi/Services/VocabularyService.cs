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
            VocabularyItem vocabularyItem;
            string cachedVocabularyItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);
            if (!string.IsNullOrEmpty(cachedVocabularyItem))
            {
                vocabularyItem = JsonConvert.DeserializeObject<VocabularyItem>(cachedVocabularyItem);
            }
            else
            {
                vocabularyItem = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id, JsonConvert.SerializeObject(vocabularyItem), _cacheOptionsSliding);
            }

            return vocabularyItem;
        }

        public async Task<VocabularyItem> AddVocabularyItem(VocabularyItem vocabularyItem)
        {
            _ = _context.VocabularyDb.Add(vocabularyItem);
            _ = await _context.SaveChangesAsync();
            _ = await SetVocabularyItem(vocabularyItem.WordId);

            return vocabularyItem;
        }
        public async Task<VocabularyItem> SetVocabularyItem(int id)
        {
            VocabularyItem word = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            if (word != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id, JsonConvert.SerializeObject(word), _cacheOptions);

                List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == word.ProgenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + word.ProgenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);
            }

            return word;
        }

        public async Task<VocabularyItem> UpdateVocabularyItem(VocabularyItem vocabularyItem)
        {
            _ = _context.VocabularyDb.Update(vocabularyItem);
            _ = await _context.SaveChangesAsync();
            _ = await SetVocabularyItem(vocabularyItem.WordId);

            return vocabularyItem;
        }

        public async Task<VocabularyItem> DeleteVocabularyItem(VocabularyItem vocabularyItem)
        {
            await RemoveVocabularyItem(vocabularyItem.WordId, vocabularyItem.ProgenyId);
            _ = _context.VocabularyDb.Remove(vocabularyItem);
            _ = await _context.SaveChangesAsync();
            

            return vocabularyItem;
        }

        public async Task RemoveVocabularyItem(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);

            List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);
        }

        public async Task<List<VocabularyItem>> GetVocabularyList(int progenyId)
        {
            List<VocabularyItem> vocabularyList;
            string cachedVocabularyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(cachedVocabularyList);
            }
            else
            {
                vocabularyList = await _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId, JsonConvert.SerializeObject(vocabularyList), _cacheOptionsSliding);
            }

            return vocabularyList;
        }
    }
}
