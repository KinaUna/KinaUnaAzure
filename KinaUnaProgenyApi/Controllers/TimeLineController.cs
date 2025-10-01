using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for TimeLineItems.
    /// </summary>
    /// <param name="progenyService"></param>
    /// <param name="timelineService"></param>
    /// <param name="userInfoService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TimeLineController(IProgenyService progenyService, ITimelineService timelineService, IUserInfoService userInfoService, ICalendarService calendarService) : ControllerBase
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            if (timelineRequest.SortOrder == 1)
            {
                DateTime updateTime = new(timelineRequest.TimelineStartDateTime.Year, timelineRequest.TimelineStartDateTime.Month, timelineRequest.TimelineStartDateTime.Day, 23, 59, 59);
                timelineRequest.TimelineStartDateTime = updateTime;
            }

            TimelineResponse timelineResponse = await timelineService.GetTimelineData(timelineRequest, currentUserInfo);

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id, 0, currentUserInfo);
            timeLineList = [.. timeLineList.Where(t => t.ProgenyTime < DateTime.UtcNow)];

            List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsLatestPosts(id, 0, currentUserInfo);
            foreach (CalendarItem calendarItem in calendarItems)
            {
                if (calendarItem.StartTime.HasValue)
                {
                    CalendarItem originalCalendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                    if (originalCalendarItem == null)
                    {
                        continue;
                    }

                    TimeLineItem timeLineItem = new();
                    timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                    timeLineList.Add(timeLineItem);
                }
            }

            return Ok(timeLineList.Count != 0 ? timeLineList : []);
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny with the given ProgenyId that a user with a given access level can access.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <returns>List of TimeLineItems.</returns>
        // GET api/timeline/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Family(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(0, id, currentUserInfo);
            timeLineList = [.. timeLineList.Where(t => t.ProgenyTime < DateTime.UtcNow)];

            List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsLatestPosts(0, id, currentUserInfo);
            foreach (CalendarItem calendarItem in calendarItems)
            {
                if (calendarItem.StartTime.HasValue)
                {
                    CalendarItem originalCalendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                    if (originalCalendarItem == null)
                    {
                        continue;
                    }

                    TimeLineItem timeLineItem = new();
                    timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                    timeLineList.Add(timeLineItem);
                }
            }

            return Ok(timeLineList.Count != 0 ? timeLineList : []);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Progenies([FromBody] List<int> progenies)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            List<TimeLineItem> timeLineList = [];
            foreach (int progenyId in progenies)
            {
                List<TimeLineItem> progenyTimeLineList = await timelineService.GetTimeLineList(progenyId, 0, currentUserInfo);
                progenyTimeLineList = [.. progenyTimeLineList.Where(t => t.ProgenyTime < DateTime.UtcNow)];
                timeLineList.AddRange(progenyTimeLineList);

                List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsLatestPosts(progenyId, 0, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }
            }

            return Ok(timeLineList.Count != 0 ? timeLineList : []);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> Families([FromBody] List<int> families)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<TimeLineItem> timeLineList = [];
            foreach (int familyId in families)
            {
                List<TimeLineItem> progenyTimeLineList = await timelineService.GetTimeLineList(0, familyId, currentUserInfo);
                progenyTimeLineList = [.. progenyTimeLineList.Where(t => t.ProgenyTime < DateTime.UtcNow)];
                timeLineList.AddRange(progenyTimeLineList);

                List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsLatestPosts(0, familyId, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }

                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id, 0, currentUserInfo);
            timeLineList = [.. timeLineList
                .Where(t => t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime)];

            List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsLatestPosts(id, 0, currentUserInfo);
            foreach (CalendarItem calendarItem in calendarItems)
            {
                if (calendarItem.StartTime.HasValue)
                {
                    CalendarItem originalCalendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                    if (originalCalendarItem == null)
                    {
                        continue;
                    }
                    TimeLineItem timeLineItem = new();
                    timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                    timeLineList.Add(timeLineItem);
                }
            }

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id, 0, currentUserInfo);

            timeLineList = [.. timeLineList
                .Where(t => t.ProgenyTime.Year < DateTime.UtcNow.Year && t.ProgenyTime.Month == DateTime.UtcNow.Month && t.ProgenyTime.Day == DateTime.UtcNow.Day)
                .OrderBy(t => t.ProgenyTime)];

            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();

            return Ok(timeLineList);

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ProgeniesYearAgo([FromBody] List<int> progenies)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<TimeLineItem> timeLineList = [];
            foreach (int progenyId in progenies)
            {
                // Todo: Rewrite, this is inefficient. We should not get the full timeline for each progeny, just to filter it down to a few items.
                List<TimeLineItem> progenyTimeLineList = await timelineService.GetTimeLineList(progenyId, 0, currentUserInfo);
                progenyTimeLineList =
                [
                    .. progenyTimeLineList
                        .Where(t => t.ProgenyTime.Year < DateTime.UtcNow.Year
                                     && t.ProgenyTime.Month == DateTime.UtcNow.Month
                                     && t.ProgenyTime.Day == DateTime.UtcNow.Day)
                ];
                timeLineList.AddRange(progenyTimeLineList);
                List<CalendarItem> calendarItems = await calendarService.GetRecurringCalendarItemsOnThisDay(progenyId, 0, currentUserInfo);
                foreach (CalendarItem calendarItem in calendarItems)
                {
                    if (calendarItem.StartTime.HasValue)
                    {
                        CalendarItem originalCalendarItem = await calendarService.GetCalendarItem(calendarItem.EventId, currentUserInfo);
                        if (originalCalendarItem == null)
                        {
                            continue;
                        }
                        TimeLineItem timeLineItem = new();
                        timeLineItem.CopyCalendarItemPropertiesForRecurringEvent(calendarItem);
                        timeLineList.Add(timeLineItem);
                    }
                }
            }

            timeLineList = [.. timeLineList.OrderByDescending(t => t.ProgenyTime)];
            
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(itemId, itemType, currentUserInfo);
            if (timeLineItem == null)
            {
                return NotFound();
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItem(id, currentUserInfo);
            if (timeLineItem == null)
            {
                return NotFound();
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            TimeLineItem timeLineItem = await timelineService.AddTimeLineItem(value, currentUserInfo);
            if (timeLineItem == null)
            {
                return Unauthorized();
            }

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItem(id, currentUserInfo);
            if (timeLineItem == null)
            {
                return NotFound();
            }
            
            TimeLineItem updatedTimelineItem = await timelineService.UpdateTimeLineItem(value, currentUserInfo);
            if (updatedTimelineItem == null)
            {
                return Unauthorized();
            }

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            TimeLineItem timeLineItem = await timelineService.GetTimeLineItem(id, currentUserInfo);
            if (timeLineItem == null) return NotFound();
            
            TimeLineItem deletedTimelineItem = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            if (deletedTimelineItem == null) return Unauthorized();

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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Progeny progeny = await progenyService.GetProgeny(onThisDayRequest.ProgenyId, currentUserInfo);
            if (progeny == null) return Ok(new OnThisDayResponse());

            
            if (onThisDayRequest.SortOrder == 1)
            {
                DateTime updateTime = new(onThisDayRequest.ThisDayDateTime.Year, onThisDayRequest.ThisDayDateTime.Month, onThisDayRequest.ThisDayDateTime.Day, 23, 59, 59);
                onThisDayRequest.ThisDayDateTime = updateTime;
            }
            
            OnThisDayResponse onThisDayResponse = await timelineService.GetOnThisDayData(onThisDayRequest, currentUserInfo);
            
            return Ok(onThisDayResponse);
        }
    }
}
