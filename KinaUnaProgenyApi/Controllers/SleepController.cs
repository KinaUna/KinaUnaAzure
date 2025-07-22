using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Constants = KinaUna.Data.Constants;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Sleep items.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userAccessService"></param>
    /// <param name="timelineService"></param>
    /// <param name="sleepService"></param>
    /// <param name="progenyService"></param>
    /// <param name="userInfoService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class SleepController(
        IAzureNotifications azureNotifications,
        IUserAccessService userAccessService,
        ITimelineService timelineService,
        ISleepService sleepService,
        IProgenyService progenyService,
        IUserInfoService userInfoService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Gets the list of Sleep items for a given Progeny and AccessLevel.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get Sleep data for.</param>
        /// <returns>List of Sleep items.</returns>
        // GET api/sleep/progeny/[id]
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

            List<Sleep> sleepList = await sleepService.GetSleepList(id, accessLevelResult.Value);
            
            if (sleepList.Count != 0)
            {
                return Ok(sleepList);
            }

            return NotFound();
        }

        /// <summary>
        /// Gets a single Sleep item by SleepId.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to get.</param>
        /// <returns>The Sleep object with the provided SleepId. UnauthorizedResult if the user doesn't have the access rights for this Sleep item.</returns>
        // GET api/sleep/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetSleepItem(int id)
        { 
            Sleep sleep = await sleepService.GetSleep(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(sleep.ProgenyId, userEmail, sleep.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            return Ok(sleep);
        }

        /// <summary>
        /// Adds a new Sleep item to the database.
        /// Then adds a TimeLineItem to the TimeLine collection and sends notifications to users with access to the Sleep item.
        /// </summary>
        /// <param name="value">The Sleep object to add.</param>
        /// <returns>The added Sleep object.</returns>
        // POST api/sleep
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Sleep value)
        {
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail();
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

            value.Author = User.GetUserId();

            Sleep sleepItem = await sleepService.AddSleep(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopySleepPropertiesForAdd(sleepItem);

            _ = await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Sleep added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new sleep item for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);

            return Ok(sleepItem);
        }

        /// <summary>
        /// Updates a Sleep item in the database.
        /// Also updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to update.</param>
        /// <param name="value">Sleep object with the updated properties.</param>
        /// <returns>The updated Sleep object.</returns>
        // PUT api/sleep/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Sleep value)
        {
            Sleep sleepItem = await sleepService.GetSleep(id);
            if (sleepItem == null)
            {
                return NotFound();
            }

            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail();
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

            sleepItem = await sleepService.UpdateSleep(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(sleepItem.SleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep);

            if (timeLineItem == null) return Ok(sleepItem);

            timeLineItem.CopySleepPropertiesForUpdate(sleepItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

            return Ok(sleepItem);
        }

        /// <summary>
        /// Deletes a Sleep item from the database.
        /// Also deletes the corresponding TimeLineItem and sends notifications to users with admin access to the Sleep item.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to delete.</param>
        /// <returns>NoContentResult. UnauthorizedResult if the user isn't an admin for the Progeny. NotFoundResult if the Sleep item doesn't exist.</returns>
        // DELETE api/sleep/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Sleep sleepItem = await sleepService.GetSleep(id);
            if (sleepItem != null)
            {
                string userEmail = User.GetEmail();
                Progeny progeny = await progenyService.GetProgeny(sleepItem.ProgenyId);
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

                TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(sleepItem.SleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep);
                if (timeLineItem != null)
                {
                    _ = await timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await sleepService.DeleteSleep(sleepItem);

                if (timeLineItem == null) return NoContent();

                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

                string notificationTitle = "Sleep for " + progeny.NickName + " deleted";
                string notificationMessage = userInfo.FullName() + " deleted a sleep item for " + progeny.NickName + ". Sleep start: " + sleepItem.SleepStart.ToString("dd-MMM-yyyy HH:mm");

                sleepItem.AccessLevel = timeLineItem.AccessLevel = 0;

                await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);

                return NoContent();
            }

            return NotFound();
        }

        /// <summary>
        /// Gets a SleepListPage for displaying Sleep items in a paged list.
        /// </summary>
        /// <param name="pageSize">Number of Sleep items per page.</param>
        /// <param name="pageIndex">Current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep data for.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first, 1= newest first.</param>
        /// <returns>SleepListPage object.</returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetSleepListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(progenyId, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Sleep> allItems = await sleepService.GetSleepList(progenyId, accessLevelResult.Value);
            allItems = [.. allItems.OrderBy(s => s.SleepStart)];

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int sleepCounter = 1;
            int slpCount = allItems.Count;
            foreach (Sleep slp in allItems)
            {
                if (sortBy == 1)
                {
                    slp.SleepNumber = slpCount - sleepCounter + 1;
                }
                else
                {
                    slp.SleepNumber = sleepCounter;
                }

                sleepCounter++;
            }

            List<Sleep> itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            SleepListPage model = new()
            {
                SleepList = itemsOnPage,
                TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize),
                PageNumber = pageIndex,
                SortBy = sortBy
            };

            return Ok(model);
        }
        
        /// <summary>
        /// Generates a SleepDetailsModel for displaying Sleep with next and previous Sleep items.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep item to display.</param>
        /// <param name="sortOrder">Sort order. 0 = Oldest first, 1 = Newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]/{sleepId:int}/{sortOrder:int}")]
        public async Task<IActionResult> GetSleepDetails(int sleepId, int sortOrder)
        {
            Sleep currentSleep = await sleepService.GetSleep(sleepId);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(currentSleep.ProgenyId, userEmail, currentSleep.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }
            
            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string userTimeZone = userInfo.Timezone;

            List<Sleep> allSleepList = await sleepService.GetSleepList(currentSleep.ProgenyId, accessLevelResult.Value);
            SleepDetailsModel sleepDetailsModel = new();
            sleepDetailsModel.CreateSleepList(currentSleep, allSleepList, accessLevelResult.Value, sortOrder, userTimeZone);

            return Ok(sleepDetailsModel.SleepList);

        }
    }
}
