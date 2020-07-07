using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
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
        private readonly IDataService _dataService;
        private readonly AzureNotifications _azureNotifications;

        public VocabularyController(ProgenyDbContext context, IDataService dataService, AzureNotifications azureNotifications)
        {
            _context = context;
            _dataService = dataService;
            _azureNotifications = azureNotifications;
        }
        
        // GET api/vocabulary/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<VocabularyItem> wordList = await _dataService.GetVocabularyList(id);
                wordList = wordList.Where(w => w.AccessLevel >= accessLevel).ToList();
                if (wordList.Any())
                {
                    return Ok(wordList);
                }
                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/vocabulary/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVocabularyItem(int id)
        {
            VocabularyItem result = await _dataService.GetVocabularyItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/vocabulary
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] VocabularyItem value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add words for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

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
            await _dataService.SetVocabularyItem(vocabularyItem.WordId);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = vocabularyItem.ProgenyId;
            tItem.AccessLevel = vocabularyItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary;
            tItem.ItemId = vocabularyItem.WordId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            if (userinfo != null)
            {
                tItem.CreatedBy = userinfo.UserId;
            }
            tItem.CreatedTime = DateTime.UtcNow;
            if (vocabularyItem.Date != null)
            {
                tItem.ProgenyTime = vocabularyItem.Date.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

            string title = "Word added for " + prog.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName +
                                 " added a new word for " + prog.NickName;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(vocabularyItem);
        }

        // PUT api/calendar/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] VocabularyItem value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit words for this child.
                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

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
            await _dataService.SetVocabularyItem(vocabularyItem.WordId);

            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == vocabularyItem.WordId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Vocabulary);
            if (tItem != null)
            {
                if (vocabularyItem.Date != null)
                {
                    tItem.ProgenyTime = vocabularyItem.Date.Value;
                }
                tItem.AccessLevel = vocabularyItem.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
            string title = "Word edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a word for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(vocabularyItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            VocabularyItem vocabularyItem = await _context.VocabularyDb.SingleOrDefaultAsync(w => w.WordId == id);
            if (vocabularyItem != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == vocabularyItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete words for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == vocabularyItem.WordId.ToString() && t.ItemType == (int)KinaUnaTypes.TimeLineType.Vocabulary);
                if (tItem != null)
                {
                    _context.TimeLineDb.Remove(tItem);
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.VocabularyDb.Remove(vocabularyItem);
                await _context.SaveChangesAsync();
                await _dataService.RemoveVocabularyItem(vocabularyItem.WordId, vocabularyItem.ProgenyId);

                UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
                string title = "Word deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a word for " + prog.NickName + ". Word: " + vocabularyItem.Word;
                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            VocabularyItem result = await _dataService.GetVocabularyItem(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVocabularyListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<VocabularyItem> allItems = await _dataService.GetVocabularyList(progenyId);
            allItems = allItems.OrderBy(v => v.Date).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int vocabularyCounter = 1;
            int vocabularyCount = allItems.Count;
            foreach (VocabularyItem word in allItems)
            {
                if (sortBy == 1)
                {
                    word.VocabularyItemNumber = vocabularyCount - vocabularyCounter + 1;
                }
                else
                {
                    word.VocabularyItemNumber = vocabularyCounter;
                }

                vocabularyCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            VocabularyListPage model = new VocabularyListPage();
            model.VocabularyList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }
    }
}
