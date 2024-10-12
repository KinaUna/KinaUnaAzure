using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for vocabulary items.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="timelineService"></param>
    /// <param name="vocabularyService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
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
        /// <summary>
        /// Get a list of all VocabularyItems for a specific Progeny.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Vocabulary items for.</param>
        /// <returns>List of Vocabulary items.</returns>
        // GET api/vocabulary/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<VocabularyItem> wordList = await vocabularyService.GetVocabularyList(id, accessLevelResult.Value);
            
            return Ok(wordList);
        }

        /// <summary>
        /// Get a specific VocabularyItem with a given WordId.
        /// Only users with the appropriate access level can get the item.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to get.</param>
        /// <returns>VocabularyItem</returns>
        // GET api/vocabulary/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVocabularyItem(int id)
        {
            VocabularyItem vocabularyItem = await vocabularyService.GetVocabularyItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(vocabularyItem.ProgenyId, userEmail, vocabularyItem.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            return Ok(vocabularyItem);
        }

        /// <summary>
        /// Add a new VocabularyItem.
        /// Also creates a TimeLineItem for the new VocabularyItem and sends notifications to users with access to the VocabularyItem.
        /// Only users with appropriate access level can add new items.
        /// </summary>
        /// <param name="value">The VocabularyItem to add.</param>
        /// <returns>The added VocabularyItems</returns>
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

        /// <summary>
        /// Update a VocabularyItem.
        /// Also updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to update.</param>
        /// <param name="value">VocabularyItem with the updated properties.</param>
        /// <returns>The updated VocabularyItem.</returns>
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
            
            return Ok(vocabularyItem);
        }

        /// <summary>
        /// Delete a VocabularyItem.
        /// Also deletes the corresponding TimeLineItem and sends notifications to users with admin access to the Progeny.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to delete.</param>
        /// <returns>NoContentResult. UnauthorizedResult if the user doesn't have access rights to delete this, NotFoundResult if the VocabularyItem doesn't exist.</returns>
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

        /// <summary>
        /// Gets the VocabularyItem with the given WordId.
        /// Only users with appropriate access for the Progeny can get the item.
        /// For mobile clients.
        /// </summary>
        /// <param name="id">The WordId of the VocabularyItem to get.</param>
        /// <returns>VocabularyItem</returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetItemMobile(int id)
        {
            VocabularyItem vocabularyItem = await vocabularyService.GetVocabularyItem(id);

            if (vocabularyItem == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(vocabularyItem.ProgenyId, userEmail, vocabularyItem.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            return Ok(vocabularyItem);

        }

        /// <summary>
        /// Generates a VocabularyListPage for a specific Progeny.
        /// </summary>
        /// <param name="pageSize">The number of VocabularyItems per page.</param>
        /// <param name="pageIndex">The current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to show VocabularyItems for.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first, 1 = newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]")]
        public async Task<IActionResult> GetVocabularyListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(progenyId, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<VocabularyItem> allItems = await vocabularyService.GetVocabularyList(progenyId, accessLevelResult.Value);

            VocabularyListPage model = new();
            model.ProcessVocabularyList(allItems, sortBy, pageIndex, pageSize);

            return Ok(model);
        }
    }
}
