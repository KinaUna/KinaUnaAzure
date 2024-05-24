using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Constants = KinaUna.Data.Constants;

namespace KinaUnaProgenyApi.Controllers
{
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
        // GET api/sleep/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Sleep> sleepList = await sleepService.GetSleepList(id);
                sleepList = sleepList.Where(s => s.AccessLevel >= accessLevel).ToList();
                if (sleepList.Any())
                {
                    return Ok(sleepList);
                }
            }

            return NotFound();
        }

        // GET api/sleep/5
        [HttpGet("{id}")]
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

        // PUT api/sleep/5
        [HttpPut("{id}")]
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

            if (timeLineItem != null)
            {
                timeLineItem.CopySleepPropertiesForUpdate(sleepItem);
                _ = await timelineService.UpdateTimeLineItem(timeLineItem);

                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

                string notificationTitle = "Sleep for " + progeny.NickName + " edited";
                string notificationMessage = userInfo.FullName() + " edited a sleep item for " + progeny.NickName;

                await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);
            }

            return Ok(sleepItem);
        }

        // DELETE api/sleep/5
        [HttpDelete("{id}")]
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

                if (timeLineItem != null)
                {
                    UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

                    string notificationTitle = "Sleep for " + progeny.NickName + " deleted";
                    string notificationMessage = userInfo.FullName() + " deleted a sleep item for " + progeny.NickName + ". Sleep start: " + sleepItem.SleepStart.ToString("dd-MMM-yyyy HH:mm");

                    sleepItem.AccessLevel = timeLineItem.AccessLevel = 0;

                    await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);
                }

                return NoContent();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("[action]/{id}")]
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
            allItems = allItems.OrderBy(s => s.SleepStart).ToList();

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

        [HttpGet("[action]/{progenyId}/{accessLevel}/{start}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                List<Sleep> result = await sleepService.GetSleepList(progenyId);
                result = result.Where(s => s.AccessLevel >= accessLevel).ToList();
                result = result.OrderByDescending(s => s.SleepStart).ToList();
                if (start != -1)
                {
                    result = result.Skip(start).Take(25).ToList();
                }

                return Ok(result);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepStatsMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
                string userTimeZone = userInfo.Timezone;
                List<Sleep> allSleepList = await sleepService.GetSleepList(progenyId);
                SleepStatsModel model = new();
                model.ProcessSleepStats(allSleepList, accessLevel, userTimeZone);

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
                string userTimeZone = userInfo.Timezone;

                List<Sleep> sList = await sleepService.GetSleepList(progenyId);

                SleepStatsModel sleepStatsModel = new();
                List<Sleep> chartList = sleepStatsModel.ProcessSleepChartData(sList, accessLevel, userTimeZone);

                List<Sleep> model = chartList.OrderBy(s => s.SleepStart).ToList();

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{sleepId}/{accessLevel}/{sortOrder}")]
        public async Task<IActionResult> GetSleepDetails(int sleepId, int accessLevel, int sortOrder)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            Sleep currentSleep = await sleepService.GetSleep(sleepId);

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(currentSleep.ProgenyId, userEmail);

            if (userAccess != null)
            {
                UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
                string userTimeZone = userInfo.Timezone;

                List<Sleep> allSleepList = await sleepService.GetSleepList(currentSleep.ProgenyId);
                SleepDetailsModel sleepDetailsModel = new();
                sleepDetailsModel.CreateSleepList(currentSleep, allSleepList, accessLevel, sortOrder, userTimeZone);

                return Ok(sleepDetailsModel.SleepList);
            }

            return Unauthorized();
        }
    }
}
