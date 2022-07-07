using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VocabularyController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ITimelineService _timelineService;
        private readonly IVocabularyService _vocabularyService;
        private readonly IProgenyService _progenyService;
        private readonly AzureNotifications _azureNotifications;

        public VocabularyController(AzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService, ITimelineService timelineService,
            IVocabularyService vocabularyService, IProgenyService progenyService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _vocabularyService = vocabularyService;
            _progenyService = progenyService;
        }

        // GET api/vocabulary/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<VocabularyItem> wordList = await _vocabularyService.GetVocabularyList(id);
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
            VocabularyItem result = await _vocabularyService.GetVocabularyItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
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

            vocabularyItem = await _vocabularyService.AddVocabularyItem(vocabularyItem);
            
            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = vocabularyItem.ProgenyId;
            tItem.AccessLevel = vocabularyItem.AccessLevel;
            tItem.ItemType = (int)KinaUnaTypes.TimeLineType.Vocabulary;
            tItem.ItemId = vocabularyItem.WordId.ToString();
            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
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

            _ = await _timelineService.AddTimeLineItem(tItem);

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
            Progeny prog = await _progenyService.GetProgeny(value.ProgenyId);
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

            VocabularyItem vocabularyItem = await _vocabularyService.GetVocabularyItem(id);
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

            vocabularyItem = await _vocabularyService.UpdateVocabularyItem(vocabularyItem);
            

            TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(vocabularyItem.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);
            if (tItem != null)
            {
                if (vocabularyItem.Date != null)
                {
                    tItem.ProgenyTime = vocabularyItem.Date.Value;
                }
                tItem.AccessLevel = vocabularyItem.AccessLevel;
                _ = await _timelineService.UpdateTimeLineItem(tItem);
            }

            UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string title = "Word edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a word for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);

            return Ok(vocabularyItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            VocabularyItem vocabularyItem = await _vocabularyService.GetVocabularyItem(id);
            if (vocabularyItem != null)
            {
                // Check if child exists.
                Progeny prog = await _progenyService.GetProgeny(vocabularyItem.ProgenyId);
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

                TimeLineItem tItem = await _timelineService.GetTimeLineItemByItemId(vocabularyItem.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);
                if (tItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(tItem);
                }

                _ = await _vocabularyService.DeleteVocabularyItem(vocabularyItem);
                
                UserInfo userinfo = await _userInfoService.GetUserInfoByEmail(userEmail);
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
            VocabularyItem result = await _vocabularyService.GetVocabularyItem(id);

            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

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
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<VocabularyItem> allItems = await _vocabularyService.GetVocabularyList(progenyId);
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
