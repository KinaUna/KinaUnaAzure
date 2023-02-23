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
    public class SleepController : ControllerBase
    {
        private readonly IUserAccessService _userAccessService;
        private readonly ITimelineService _timelineService;
        private readonly ISleepService _sleepService;
        private readonly IProgenyService _progenyService;
        private readonly IUserInfoService _userInfoService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public SleepController(IAzureNotifications azureNotifications, IUserAccessService userAccessService, ITimelineService timelineService, ISleepService sleepService, IProgenyService progenyService, IUserInfoService userInfoService, IWebNotificationsService webNotificationsService)
        {
            _azureNotifications = azureNotifications;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _sleepService = sleepService;
            _progenyService = progenyService;
            _userInfoService = userInfoService;
            _webNotificationsService = webNotificationsService;
        }

        // GET api/sleep/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Sleep> sleepList = await _sleepService.GetSleepList(id); 
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

            Sleep result = await _sleepService.GetSleep(id);
            if (result.AccessLevel == (int) AccessLevel.Public || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
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
            
            Sleep sleepItem = await _sleepService.AddSleep(value);
            
            TimeLineItem timeLineItem = new TimeLineItem();
            timeLineItem.CopySleepPropertiesForAdd(sleepItem);
            
            _ = await _timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            
            string notificationTitle = "Sleep added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new sleep item for " + progeny.NickName;
            
            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);

            return Ok(sleepItem);
        }

        // PUT api/sleep/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Sleep value)
        {
            Sleep sleepItem = await _sleepService.GetSleep(id);
            if (sleepItem == null)
            {
                return NotFound();
            }

            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
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

            sleepItem = await _sleepService.UpdateSleep(value);
            
            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(sleepItem.SleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep);
            
            if (timeLineItem != null)
            {
                timeLineItem.CopySleepPropertiesForUpdate(sleepItem);
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);

                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                string notificationTitle = "Sleep for " + progeny.NickName + " edited";
                string notificationMessage = userInfo.FullName() + " edited a sleep item for " + progeny.NickName;
                
                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);
            }

            return Ok(sleepItem);
        }

        // DELETE api/sleep/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Sleep sleepItem = await _sleepService.GetSleep(id);
            if (sleepItem != null)
            {
                string userEmail = User.GetEmail();
                Progeny progeny = await _progenyService.GetProgeny(sleepItem.ProgenyId);
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

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(sleepItem.SleepId.ToString(), (int)KinaUnaTypes.TimeLineType.Sleep);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await _sleepService.DeleteSleep(sleepItem);

                if (timeLineItem != null)
                {
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                    string notificationTitle = "Sleep for " + progeny.NickName + " deleted";
                    string notificationMessage = userInfo.FullName() + " deleted a sleep item for " + progeny.NickName + ". Sleep start: " + sleepItem.SleepStart.ToString("dd-MMM-yyyy HH:mm");
                    
                    sleepItem.AccessLevel = timeLineItem.AccessLevel = 0;

                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendSleepNotification(sleepItem, userInfo, notificationTitle);
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

            Sleep result = await _sleepService.GetSleep(id);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail); 
            if (userAccess != null)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetSleepListPage([FromQuery]int pageSize = 8, [FromQuery]int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId, [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

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

            List<Sleep> allItems = await _sleepService.GetSleepList(progenyId);
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

            SleepListPage model = new SleepListPage();
            model.SleepList = itemsOnPage;
            model.TotalPages = (int)Math.Ceiling(allItems.Count / (double)pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;
            
            return Ok(model);
        }
        
        [HttpGet("[action]/{progenyId}/{accessLevel}/{start}")]
        public async Task<IActionResult> GetSleepListMobile(int progenyId, int accessLevel, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
            if (userAccess != null)
            {
                List<Sleep> result = await _sleepService.GetSleepList(progenyId); 
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
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail); 
            if (userAccess != null)
            {
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string userTimeZone = userInfo.Timezone;
                List<Sleep> allSleepList = await _sleepService.GetSleepList(progenyId);
                SleepStatsModel model = new SleepStatsModel();
                model.ProcessSleepStats(allSleepList, accessLevel, userTimeZone);

                return Ok(model);
            }

            return Unauthorized();
        }

        [HttpGet("[action]/{progenyId}/{accessLevel}")]
        public async Task<IActionResult> GetSleepChartDataMobile(int progenyId, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail); 
            if (userAccess != null)
            {
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string userTimeZone = userInfo.Timezone;

                List<Sleep> sList = await _sleepService.GetSleepList(progenyId);
                
                SleepStatsModel sleepStatsModel = new SleepStatsModel();
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
            
            Sleep currentSleep = await _sleepService.GetSleep(sleepId);
            
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(currentSleep.ProgenyId, userEmail);
            
            if (userAccess != null)
            {
                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string userTimeZone = userInfo.Timezone;

                List<Sleep> allSleepList = await _sleepService.GetSleepList(currentSleep.ProgenyId);
                SleepDetailsModel sleepDetailsModel = new SleepDetailsModel();
                sleepDetailsModel.CreateSleepList(currentSleep, allSleepList, accessLevel, sortOrder, userTimeZone);

                return Ok(sleepDetailsModel.SleepList);
            }

            return Unauthorized();
        }
    }
}
