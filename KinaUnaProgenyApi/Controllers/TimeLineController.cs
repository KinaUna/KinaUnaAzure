using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using KinaUnaProgenyApi.Services.UserAccessService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for TimeLineItems.
    /// </summary>
    /// <param name="progenyService"></param>
    /// <param name="userAccessService"></param>
    /// <param name="timelineService"></param>
    /// <param name="userInfoService"></param>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TimeLineController(IProgenyService progenyService, IUserAccessService userAccessService, ITimelineService timelineService, IUserInfoService userInfoService, ICalendarService calendarService) : ControllerBase
    {
        /// <summary>
        /// Gets a list of TimeLineItems for a Progeny,
        /// Filtering by TimeLineItem type, category, tags is optional.
        /// </summary>
        /// <param name="timelineRequest"></param>
        /// <returns>TimelineResponse object.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetTimeLineRequestData([FromBody] TimelineRequest timelineRequest)
        {
            Progeny progeny = await progenyService.GetProgeny(timelineRequest.ProgenyId);
            if (progeny == null) return Ok(new TimelineResponse());

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUser = await userInfoService.GetUserInfoByEmail(userEmail);

            List<UserAccess> userAccessList = [];
            foreach (int progenyId in timelineRequest.Progenies)
            {
                UserAccess userAccessItem = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                if (userAccessItem != null)
                {
                    userAccessList.Add(userAccessItem);
                }
            }

            if (timelineRequest.SortOrder == 1)
            {
                DateTime updateTime = new(timelineRequest.TimelineStartDateTime.Year, timelineRequest.TimelineStartDateTime.Month, timelineRequest.TimelineStartDateTime.Day, 23, 59, 59);
                timelineRequest.TimelineStartDateTime = updateTime;
            }

            TimelineResponse timelineResponse = await timelineService.GetTimelineData(timelineRequest, currentUser, userAccessList);

            return Ok(timelineResponse);
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny with the given ProgenyId that a user with a given access level can access.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <returns>List of TimeLineItems.</returns>
        // GET api/timeline/progeny/[id]
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

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);
            timeLineList = timeLineList.Where(t => t.AccessLevel >= accessLevelResult.Value && t.ProgenyTime < DateTime.UtcNow).ToList();
            
            return Ok(timeLineList.Count != 0 ? timeLineList : []);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Progenies([FromBody] List<int> progenies)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            List<TimeLineItem> timeLineList = [];
            foreach (int progenyId in progenies)
            {
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                if (userAccess != null)
                {
                    List<TimeLineItem> progenyTimeLineList = await timelineService.GetTimeLineList(progenyId);
                    progenyTimeLineList = progenyTimeLineList.Where(t => t.AccessLevel >= userAccess.AccessLevel && t.ProgenyTime < DateTime.UtcNow).ToList();
                    timeLineList.AddRange(progenyTimeLineList);
                }
            }

            return Ok(timeLineList.Count != 0 ? timeLineList : []);
        }

        /// <summary>
        /// Gets a list of the latest TimeLineItems for a Progeny with the given ProgenyId that a user with a given access level can access.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="count">The number of TimeLineItems to include.</param>
        /// <param name="start">The number of TimeLineItems to skip.</param>
        /// <returns>List of TimeLineItems.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{count:int}/{start:int}")]
        public async Task<IActionResult> ProgenyLatest(int id, int count = 5, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);
            timeLineList = [.. timeLineList
                .Where(t => t.AccessLevel >= accessLevelResult.Value && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime)];
            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();

            return Ok(timeLineList.Skip(start).Take(count));

        }

        /// <summary>
        /// Gets a list of TimeLineItems that happened on the same day as the current date.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <returns>List of TimeLineItems.</returns>
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> ProgenyYearAgo(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(id, userEmail, null);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);

            timeLineList = [.. timeLineList
                .Where(t => t.AccessLevel >= accessLevelResult.Value && t.ProgenyTime.Year < DateTime.UtcNow.Year && t.ProgenyTime.Month == DateTime.UtcNow.Month && t.ProgenyTime.Day == DateTime.UtcNow.Day)
                .OrderBy(t => t.ProgenyTime)];

            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();

            return Ok(timeLineList);

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ProgeniesYearAgo([FromBody] List<int> progenies)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            List<TimeLineItem> timeLineList = [];
            foreach (int progenyId in progenies)
            {
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                if (userAccess != null)
                {
                    List<TimeLineItem> progenyTimeLineList = await timelineService.GetTimeLineList(progenyId);
                    progenyTimeLineList = [.. progenyTimeLineList
                        .Where(t => t.AccessLevel >= userAccess.AccessLevel 
                                    && t.ProgenyTime.Year < DateTime.UtcNow.Year 
                                    && t.ProgenyTime.Month == DateTime.UtcNow.Month 
                                    && t.ProgenyTime.Day == DateTime.UtcNow.Day)];
                    timeLineList.AddRange(progenyTimeLineList);
                    List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsOnThisDay(progenyId);
                    foreach (CalendarItem calendarItem in calendarItems)
                    {
                        if (calendarItem.AccessLevel >= userAccess.AccessLevel && calendarItem.StartTime.HasValue)
                        {
                            TimeLineItem timeLineItem = new TimeLineItem();
                            timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                            timeLineList.Add(timeLineItem);
                        }
                    }
                }
            }

            timeLineList = timeLineList.OrderByDescending(t => t.ProgenyTime).ToList();
            
            return Ok(timeLineList.Count != 0 ? timeLineList : []);
        }

        /// <summary>
        /// Gets a TimeLineItem by the Type and ItemId.
        /// I.e. gets a TimeLineItem for a Picture with a given PictureId.
        /// </summary>
        /// <param name="itemId">The type specific id of the TimeLineItem.</param>
        /// <param name="itemType">The type of item to get TimeLineItem for.</param>
        /// <returns>TimeLineItem object. UnauthorizedResult if the user is not allowed to access the TimeLineItem.</returns>
        [HttpGet("[action]/{itemId}/{itemType:int}")]
        public async Task<IActionResult> GetTimeLineItemByItemId(string itemId, int itemType)
        {
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(itemId, itemType);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(timeLineItem.ProgenyId, userEmail, timeLineItem.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }
            
            return Ok(timeLineItem);
        }

        /// <summary>
        /// Gets a TimeLineItem by the TimeLineId.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem entity to get.</param>
        /// <returns>TimeLineItem. UnauthorizedResult if the user is not allowed to access the TimeLineItem.</returns>
        // GET api/timeline/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            CustomResult<int> accessLevelResult = await userAccessService.GetValidatedAccessLevel(timeLineItem.ProgenyId, userEmail, timeLineItem.AccessLevel);
            if (!accessLevelResult.IsSuccess)
            {
                return accessLevelResult.ToActionResult();
            }

            return Ok(timeLineItem);
        }


        /// <summary>
        /// Adds a new TimeLineItem entity to the database.
        /// </summary>
        /// <param name="value">The TimeLineItem to add.</param>
        /// <returns>The added TimeLineItem. Unauthorized if the user doesn't have access to add items for the Progeny.</returns>
        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TimeLineItem value)
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

            TimeLineItem timeLineItem = await timelineService.AddTimeLineItem(value);

            return Ok(timeLineItem);
        }

        /// <summary>
        /// Updates a TimeLineItem entity in the database.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to update.</param>
        /// <param name="value">TimeLineItem with the updated properties.</param>
        /// <returns>The updated TimeLineItem. UnauthorizedResult if the user is not allowed to edit items for the Progeny. NotFoundResult if the TimeLineItem doesn't exist.</returns>
        // PUT api/timeline/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] TimeLineItem value)
        {
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItem(id);
            if (timeLineItem == null)
            {
                return NotFound();
            }

            Progeny progeny = await progenyService.GetProgeny(timeLineItem.ProgenyId);
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

            _ = await timelineService.UpdateTimeLineItem(value);

            return Ok(timeLineItem);
        }

        /// <summary>
        /// Deletes a TimeLineItem entity from the database.
        /// </summary>
        /// <param name="id">The TimeLineId of the TimeLineItem to delete.</param>
        /// <returns>NoContent if successful. UnauthorizedResult if the user doesn't have the access level to delete items. NotFoundResult if the TimeLineItem doesn't exist.</returns>
        // DELETE api/timeline/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItem(id);
            if (timeLineItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(timeLineItem.ProgenyId);
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

            _ = await timelineService.DeleteTimeLineItem(timeLineItem);

            return NoContent();

        }

        /// <summary>
        /// Gets a list of TimeLineItems that happened on the same day for each year, month, or week, for a Progeny,
        /// Filtering by TimeLineItem type, category, tags is optional.
        /// </summary>
        /// <param name="onThisDayRequest"></param>
        /// <returns>OnThisDayResponse object.</returns>
        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetOnThisDayTimeLineItems([FromBody] OnThisDayRequest onThisDayRequest)
        {
            Progeny progeny = await progenyService.GetProgeny(onThisDayRequest.ProgenyId);
            if (progeny == null) return Ok(new OnThisDayResponse());

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(onThisDayRequest.ProgenyId, userEmail);
            if (userAccess == null) return Ok(new OnThisDayResponse());
            UserInfo currentUser = await userInfoService.GetUserInfoByEmail(userEmail);
            onThisDayRequest.AccessLevel = userAccess.AccessLevel;
            if (onThisDayRequest.SortOrder == 1)
            {
                DateTime updateTime = new(onThisDayRequest.ThisDayDateTime.Year, onThisDayRequest.ThisDayDateTime.Month, onThisDayRequest.ThisDayDateTime.Day, 23, 59, 59);
                onThisDayRequest.ThisDayDateTime = updateTime;
            }

            List<UserAccess> userAccessList = [];
            foreach (int progenyId in onThisDayRequest.Progenies)
            {
                UserAccess userAccessItem = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                if (userAccessItem != null)
                {
                    userAccessList.Add(userAccessItem);
                }
            }

            OnThisDayResponse onThisDayResponse = await timelineService.GetOnThisDayData(onThisDayRequest, currentUser, userAccessList);
            
            return Ok(onThisDayResponse);
        }
    }
}
