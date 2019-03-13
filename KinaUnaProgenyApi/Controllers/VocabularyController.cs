using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            VocabularyItem result = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);

            return Ok(result);
        }
    }
}
