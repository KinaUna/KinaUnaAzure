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
    public class VocabularyController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ITimelineService timelineService,
        IVocabularyService vocabularyService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/vocabulary/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<VocabularyItem> wordList = await vocabularyService.GetVocabularyList(id);
            wordList = wordList.Where(w => w.AccessLevel >= accessLevel).ToList();
            if (wordList.Count != 0)
            {
                return Ok(wordList);
            }
            return NotFound();

        }

        // GET api/vocabulary/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVocabularyItem(int id)
        {
            VocabularyItem result = await vocabularyService.GetVocabularyItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            VocabularyItem vocabularyItem = await vocabularyService.AddVocabularyItem(value);


            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVocabularyItemPropertiesForAdd(vocabularyItem);
            _ = await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Word added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new word for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendVocabularyNotification(vocabularyItem, userInfo, notificationTitle);

            return Ok(vocabularyItem);
        }

        // PUT api/calendar/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] VocabularyItem value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            VocabularyItem vocabularyItem = await vocabularyService.GetVocabularyItem(id);
            if (vocabularyItem == null)
            {
                return NotFound();
            }

            vocabularyItem = await vocabularyService.UpdateVocabularyItem(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(vocabularyItem.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);
            if (timeLineItem == null) return Ok(vocabularyItem);

            timeLineItem.CopyVocabularyItemPropertiesForUpdate(vocabularyItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

            //UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            //string notificationTitle = "Word edited for " + progeny.NickName;
            //string notificationMessage = userInfo.FullName() + " edited a word for " + progeny.NickName;

            //await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            //await webNotificationsService.SendVocabularyNotification(vocabularyItem, userInfo, notificationTitle);
            
            return Ok(vocabularyItem);
        }

        // DELETE api/calendar/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            VocabularyItem vocabularyItem = await vocabularyService.GetVocabularyItem(id);
            if (vocabularyItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(vocabularyItem.ProgenyId);
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

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(vocabularyItem.WordId.ToString(), (int)KinaUnaTypes.TimeLineType.Vocabulary);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            _ = await vocabularyService.DeleteVocabularyItem(vocabularyItem);

            if (timeLineItem == null) return NoContent();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Word deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a word for " + progeny.NickName + ". Word: " + vocabularyItem.Word;

            vocabularyItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendVocabularyNotification(vocabularyItem, userInfo, notificationTitle);

            return NoContent();

        }

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            VocabularyItem result = await vocabularyService.GetVocabularyItem(id);

            if (result == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

            if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();

        }

        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetVocabularyListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<VocabularyItem> allItems = await vocabularyService.GetVocabularyList(progenyId);

            VocabularyListPage model = new();
            model.ProcessVocabularyList(allItems, sortBy, pageIndex, pageSize);

            return Ok(model);
        }
    }
}
