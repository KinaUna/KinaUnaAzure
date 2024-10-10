using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
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
    public class TimeLineController(IProgenyService progenyService, IUserAccessService userAccessService, ITimelineService timelineService, IUserInfoService userInfoService) : ControllerBase
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

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(timelineRequest.ProgenyId, userEmail);
            if (userAccess == null) return Ok(new OnThisDayResponse());
            UserInfo currentUser = await userInfoService.GetUserInfoByEmail(userEmail);
            timelineRequest.AccessLevel = userAccess.AccessLevel;
            if (timelineRequest.SortOrder == 1)
            {
                DateTime updateTime = new(timelineRequest.TimelineStartDateTime.Year, timelineRequest.TimelineStartDateTime.Month, timelineRequest.TimelineStartDateTime.Day, 23, 59, 59);
                timelineRequest.TimelineStartDateTime = updateTime;
            }

            TimelineResponse timelineResponse = await timelineService.GetTimelineData(timelineRequest, currentUser.Timezone);

            return Ok(timelineResponse);
        }

        /// <summary>
        /// Gets a list of all TimeLineItems for a Progeny with the given ProgenyId that a user with a given access level can access.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of TimeLineItems.</returns>
        // GET api/timeline/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);
            timeLineList = timeLineList.Where(t => userAccess != null && t.AccessLevel >= userAccess.AccessLevel && t.ProgenyTime < DateTime.UtcNow).ToList();
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
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="count">The number of TimeLineItems to include.</param>
        /// <param name="start">The number of TimeLineItems to skip.</param>
        /// <returns>List of TimeLineItems.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}/{count:int}/{start:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> ProgenyLatest(int id, int accessLevel = 5, int count = 5, int start = 0)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);
            timeLineList = [.. timeLineList
                .Where(t => userAccess != null && t.AccessLevel >= userAccess.AccessLevel && t.ProgenyTime < DateTime.UtcNow).OrderBy(t => t.ProgenyTime)];
            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();

            return Ok(timeLineList.Skip(start).Take(count));

        }

        /// <summary>
        /// Gets a list of TimeLineItems that happened on the same day as the current date.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of TimeLineItems.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public async Task<IActionResult> ProgenyYearAgo(int id, int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);

            timeLineList = [.. timeLineList
                .Where(t => userAccess != null && t.AccessLevel >= userAccess.AccessLevel && t.ProgenyTime.Year < DateTime.UtcNow.Year && t.ProgenyTime.Month == DateTime.UtcNow.Month && t.ProgenyTime.Day == DateTime.UtcNow.Day)
                .OrderBy(t => t.ProgenyTime)];

            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList.Reverse();

            return Ok(timeLineList);

        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> ProgeniesYearAgo([FromBody] List<int> progeniesList)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            List<TimeLineItem> timeLineList = [];
            foreach (int progenyId in progeniesList)
            {
                UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(progenyId, userEmail);
                if (userAccess != null)
                {
                    List<TimeLineItem> progenyTimeLineList = await timelineService.GetTimeLineList(progenyId);
                    progenyTimeLineList = [.. progenyTimeLineList
                        .Where(t => t.AccessLevel >= userAccess.AccessLevel && t.ProgenyTime.Year < DateTime.UtcNow.Year && t.ProgenyTime.Month == DateTime.UtcNow.Month && t.ProgenyTime.Day == DateTime.UtcNow.Day)
                        .OrderBy(t => t.ProgenyTime)];
                    if (progenyTimeLineList.Count != 0)
                    {
                        progenyTimeLineList.Reverse();
                        timeLineList.AddRange(progenyTimeLineList);
                    }
                }
            }

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
            TimeLineItem result = await timelineService.GetTimeLineItemByItemId(itemId, itemType);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if ((userAccess != null && userAccess.AccessLevel <= result.AccessLevel) || result.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
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
            TimeLineItem result = await timelineService.GetTimeLineItem(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if ((userAccess != null && userAccess.AccessLevel <= result.AccessLevel) || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
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
        /// Gets a list of the latest TimeLineItems for a Progeny with the given ProgenyId that a user with a given access level can access.
        /// If a start date is provided, only TimeLineItems before that date are included.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get TimeLineItems for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="count">The number of TimeLineItems to include.</param>
        /// <param name="start">The number of TimeLineItems to skip.</param>
        /// <param name="year">The start date year. 0 for all items.</param>
        /// <param name="month">The start date month. 1 = Jan, 2 = Feb, etc. 0 for all items.</param>
        /// <param name="day">The start date day. 0 for all items.</param>
        /// <returns>List of TimeLineItems ordered by newest first.</returns>
        [HttpGet]
        [Route("[action]/{id:int}/{accessLevel:int}/{count:int}/{start:int}/{year:int}/{month:int}/{day:int}")]
        // ReSharper disable once RedundantAssignment
        // The parameter is used by mobile app and cannot be removed until the mobile app is updated.
        public async Task<IActionResult> ProgenyLatestMobile(int id, int accessLevel = 5, int count = 5, int start = 0, int year = 0, int month = 0, int day = 0)
        {
            Progeny progeny = await progenyService.GetProgeny(id);
            if (progeny == null) return Ok(new List<TimeLineItem>());

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;

            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null) return Ok(new List<TimeLineItem>());

            accessLevel = userAccess.AccessLevel;

            DateTime startDate;
            if (year != 0 && month != 0 && day != 0)
            {
                startDate = new DateTime(year, month, day, 23, 59, 59, DateTimeKind.Utc);

            }
            else
            {
                startDate = DateTime.UtcNow;
            }

            List<TimeLineItem> timeLineList = await timelineService.GetTimeLineList(id);

            if (timeLineList.Count == 0) return Ok(new List<TimeLineItem>());

            timeLineList = [.. timeLineList.Where(t => t.AccessLevel >= accessLevel && t.ProgenyTime < startDate).OrderBy(t => t.ProgenyTime)];
            timeLineList.Reverse();

            return Ok(timeLineList.Skip(start).Take(count));

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
