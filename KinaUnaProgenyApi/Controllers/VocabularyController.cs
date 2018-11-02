using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Data;
using KinaUnaProgenyApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VocabularyController : ControllerBase
    {
        private readonly ProgenyDbContext _context;

        public VocabularyController(ProgenyDbContext context)
        {
            _context = context;

        }
        // GET api/vocabulary
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            List<VocabularyItem> resultList = await _context.VocabularyDb.AsNoTracking().ToListAsync();

            return Ok(resultList);
        }

        // GET api/vocabulary/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == id && w.AccessLevel >= accessLevel).ToListAsync();
            if (wordList.Any())
            {
                return Ok(wordList);
            }
            else
            {
                return NotFound();
            }

        }

        // GET api/vocabulary/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVocabularyItem(int id)
        {
            VocabularyItem result = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);

            return Ok(result);
        }

        // POST api/vocabulary
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VocabularyItem value)
        {
            VocabularyItem vocabularyItem = new VocabularyItem();
            vocabularyItem.AccessLevel = value.AccessLevel;
            vocabularyItem.Author = value.Author;
            vocabularyItem.Date = value.Date;
            vocabularyItem.DateAdded = DateTime.UtcNow;
            vocabularyItem.ProgenyId = value.ProgenyId;
            vocabularyItem.Description = value.Description;
            vocabularyItem.Language = value.Language;
            vocabularyItem.SoundsLike = value.SoundsLike;
            vocabularyItem.Word = value.Word;
            
            _context.VocabularyDb.Add(vocabularyItem);
            await _context.SaveChangesAsync();

            return Ok(vocabularyItem);
        }

        // PUT api/calendar/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] VocabularyItem value)
        {
            VocabularyItem vocabularyItem = await _context.VocabularyDb.SingleOrDefaultAsync(w => w.WordId == id);
            if (vocabularyItem == null)
            {
                return NotFound();
            }

            vocabularyItem.AccessLevel = value.AccessLevel;
            vocabularyItem.Author = value.Author;
            vocabularyItem.Date = value.Date;
            vocabularyItem.DateAdded = value.DateAdded;
            vocabularyItem.ProgenyId = value.ProgenyId;
            vocabularyItem.Description = value.Description;
            vocabularyItem.Language = value.Language;
            vocabularyItem.SoundsLike = value.SoundsLike;
            vocabularyItem.Word = value.Word;

            _context.VocabularyDb.Update(vocabularyItem);
            await _context.SaveChangesAsync();

            return Ok(vocabularyItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            VocabularyItem vocabularyItem = await _context.VocabularyDb.SingleOrDefaultAsync(w => w.WordId == id);
            if (vocabularyItem != null)
            {
                _context.VocabularyDb.Remove(vocabularyItem);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> SyncAll()
        {
            
            HttpClient vocabularyHttpClient = new HttpClient();
            
            vocabularyHttpClient.BaseAddress = new Uri("https://kinauna.com");
            vocabularyHttpClient.DefaultRequestHeaders.Accept.Clear();
            vocabularyHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // GET api/pictures/[id]
            string vocabularyApiPath = "/api/azureexport/vocabularyexport";
            var vocabularyUri = "https://kinauna.com" + vocabularyApiPath;

            var vocabularyResponseString = await vocabularyHttpClient.GetStringAsync(vocabularyUri);

            List<VocabularyItem> vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(vocabularyResponseString);
            List<VocabularyItem> vocabularyItems = new List<VocabularyItem>();
            foreach (VocabularyItem value in vocabularyList)
            {
                VocabularyItem vocabularyItem = new VocabularyItem();
                vocabularyItem.AccessLevel = value.AccessLevel;
                vocabularyItem.Author = value.Author;
                vocabularyItem.Date = value.Date;
                vocabularyItem.DateAdded = value.DateAdded;
                vocabularyItem.ProgenyId = value.ProgenyId;
                vocabularyItem.Description = value.Description;
                vocabularyItem.Language = value.Language;
                vocabularyItem.SoundsLike = value.SoundsLike;
                vocabularyItem.Word = value.Word;
                await _context.VocabularyDb.AddAsync(vocabularyItem);
                    vocabularyItems.Add(vocabularyItem);
                
            }
            await _context.SaveChangesAsync();

            return Ok(vocabularyItems);
        }
    }
}
