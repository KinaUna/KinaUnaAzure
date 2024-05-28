﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class TimeLineController(IProgenyService progenyService, IUserAccessService userAccessService, ITimelineService timelineService) : ControllerBase
    {
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
    }
}
