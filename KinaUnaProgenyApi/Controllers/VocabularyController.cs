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
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public VocabularyController(IAzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService, ITimelineService timelineService,
            IVocabularyService vocabularyService, IProgenyService progenyService, IWebNotificationsService webNotificationsService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _vocabularyService = vocabularyService;
            _progenyService = progenyService;
            _webNotificationsService = webNotificationsService;
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
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            VocabularyItem vocabularyItem = await _vocabularyService.AddVocabularyItem(value);

            
            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVocabularyItemPropertiesForAdd(vocabularyItem);
            _ = await _timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            string notificationTitle = "Word added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new word for " + progeny.NickName;

            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendVocabularyNotification(vocabularyItem, userInfo, notificationTitle);

            return Ok(vocabularyItem);
        }

        // PUT api/calendar/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] VocabularyItem value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                if (!progeny.IsInAdminList(userEmail))
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
            
            vocabularyItem = await _vocabularyService.UpdateVocabularyItem(value);
            
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(vocabularyItem.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);
            if (timeLineItem != null)
            {
                timeLineItem.CopyVocabularyItemPropertiesForUpdate(vocabularyItem);
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);
                
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail); 
                
                string notificationTitle = "Word edited for " + progeny.NickName; 
                string notificationMessage = userInfo.FullName() + " edited a word for " + progeny.NickName;
            
                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendVocabularyNotification(vocabularyItem, userInfo, notificationTitle);
            }

            
            return Ok(vocabularyItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            VocabularyItem vocabularyItem = await _vocabularyService.GetVocabularyItem(id);
            if (vocabularyItem != null)
            {
                Progeny progeny = await _progenyService.GetProgeny(vocabularyItem.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (progeny != null)
                {
                    if (!progeny.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(vocabularyItem.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await _vocabularyService.DeleteVocabularyItem(vocabularyItem);
                
                if (timeLineItem != null)
                {
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                    
                    string notificationTitle = "Word deleted for " + progeny.NickName;
                    string notificationMessage = userInfo.FullName() + " deleted a word for " + progeny.NickName + ". Word: " + vocabularyItem.Word;

                    vocabularyItem.AccessLevel = timeLineItem.AccessLevel = 0;
                    
                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendVocabularyNotification(vocabularyItem, userInfo, notificationTitle);
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

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            
            List<VocabularyItem> allItems = await _vocabularyService.GetVocabularyList(progenyId);
            
            VocabularyListPage model = new();
            model.ProcessVocabularyList(allItems, sortBy, pageIndex, pageSize);

            return Ok(model);
        }
    }
}
