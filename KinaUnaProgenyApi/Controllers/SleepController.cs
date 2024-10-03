using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Authorize(AuthenticationSchemes = "Bearer")]
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
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Sleep items.</returns>
        // GET api/sleep/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return NotFound();

            List<Sleep> sleepList = await sleepService.GetSleepList(id);
            sleepList = sleepList.Where(s => s.AccessLevel >= accessLevel).ToList();
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

            Sleep result = await sleepService.GetSleep(id);
            if (result.AccessLevel == (int)AccessLevel.Public || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
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
        /// Gets a list of Sleep items for a given Progeny and AccessLevel.
        /// For use in the mobile clients.
        /// </summary>
        /// <param name="id">The SleepId of the Sleep item to get.</param>
        /// <returns>The Sleep item with the given SleepId.UnauthorizedResult if the user doesn't have access to the Sleep item.</returns>
        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetSleepMobile(int id)
        {

            Sleep result = await sleepService.GetSleep(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        /// <summary>
        /// Gets a SleepListPage for displaying Sleep items in a paged list.
        /// </summary>
        /// <param name="pageSize">Number of Sleep items per page.</param>
        /// <param name="pageIndex">Current page number.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep data for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <param name="sortBy">Sort order. 0 = oldest first, 1= newest first.</param>
        /// <returns>SleepListPage object.</returns>
        [HttpGet("[action]")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> GetSleepListPage([FromQuery] int pageSize = 8, [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }
            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Sleep> allItems = await sleepService.GetSleepList(progenyId);
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
        /// Gets a list of Sleep items for a given Progeny and AccessLevel.
        /// For use in the mobile clients.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get sleep items for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <param name="start">Number of Sleep items to skip.</param>
        /// <returns>List of Sleep objects.</returns>
        [HttpGet("[action]/{progenyId:int}/{accessLevel:int}/{start:int}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess == null) return Unauthorized();

            List<Sleep> result = await sleepService.GetSleepList(progenyId);
            result = result.Where(s => s.AccessLevel >= accessLevel).ToList();
            result = [.. result.OrderByDescending(s => s.SleepStart)];
            if (start != -1)
            {
                result = result.Skip(start).Take(25).ToList();
            }

            return Ok(result);

        }

        /// <summary>
        /// Gets SleepStats for a given Progeny and AccessLevel.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get Sleep statistics for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>SleepStats object.</returns>
        [HttpGet("[action]/{progenyId:int}/{accessLevel:int}")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess == null) return Unauthorized();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string userTimeZone = userInfo.Timezone;
            List<Sleep> allSleepList = await sleepService.GetSleepList(progenyId);
            SleepStatsModel model = new();
            model.ProcessSleepStats(allSleepList, accessLevel, userTimeZone);

            return Ok(model);

        }

        /// <summary>
        /// Generates a list of Sleep items for displaying in Sleep statistics charts.
        /// For mobile clients.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to display statistics for.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny.</param>
        /// <returns>List of Sleep objects.</returns>
        [HttpGet("[action]/{progenyId:int}/{accessLevel:int}")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess == null) return Unauthorized();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string userTimeZone = userInfo.Timezone;

            List<Sleep> sList = await sleepService.GetSleepList(progenyId);

            SleepStatsModel sleepStatsModel = new();
            List<Sleep> chartList = sleepStatsModel.ProcessSleepChartData(sList, accessLevel, userTimeZone);

            List<Sleep> model = [.. chartList.OrderBy(s => s.SleepStart)];

            return Ok(model);

        }

        /// <summary>
        /// Generates a SleepDetailsModel for displaying Sleep with next and previous Sleep items.
        /// </summary>
        /// <param name="sleepId">The SleepId of the Sleep item to display.</param>
        /// <param name="accessLevel">The current user's access level for the Progeny to display Sleep data for.</param>
        /// <param name="sortOrder">Sort order. 0 = Oldest first, 1 = Newest first.</param>
        /// <returns></returns>
        [HttpGet("[action]/{sleepId:int}/{accessLevel:int}/{sortOrder:int}")]
        public async Task<IActionResult> GetSleepDetails(int sleepId, int accessLevel, int sortOrder)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            Sleep currentSleep = await sleepService.GetSleep(sleepId);

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(currentSleep.ProgenyId, userEmail);
            if (userAccess == null) return Unauthorized();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string userTimeZone = userInfo.Timezone;

            List<Sleep> allSleepList = await sleepService.GetSleepList(currentSleep.ProgenyId);
            SleepDetailsModel sleepDetailsModel = new();
            sleepDetailsModel.CreateSleepList(currentSleep, allSleepList, accessLevel, sortOrder, userTimeZone);

            return Ok(sleepDetailsModel.SleepList);

        }
    }
}
