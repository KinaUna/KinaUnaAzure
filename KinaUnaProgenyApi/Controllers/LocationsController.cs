using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly ProgenyDbContext _context;
        private readonly IDataService _dataService;
        private readonly AzureNotifications _azureNotifications;

        public LocationsController(ProgenyDbContext context, IDataService dataService, AzureNotifications azureNotifications)
        {
            _context = context;
            _dataService = dataService;
            _azureNotifications = azureNotifications;
        }

        // GET api/locations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail); 
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Location> locationsList = await _dataService.GetLocationsList(id);
                locationsList = locationsList.Where(l => l.AccessLevel >= accessLevel).ToList();
                if (locationsList.Any())
                {
                    return Ok(locationsList);
                }

                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/locations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetLocationItem(int id)
        {
            Location
                result = await _dataService.GetLocation(id);

            if (result == null)
            {
                return NotFound();
            }

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Location value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to add locations for this child.

                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Location location = new Location();
            location.AccessLevel = value.AccessLevel;
            location.Author = value.Author;
            location.City = value.City;
            location.Country = value.Country;
            location.County = value.County;
            location.Date = value.Date;
            location.DateAdded = value.DateAdded;
            location.District = value.District;
            location.HouseNumber = value.HouseNumber;
            location.Latitude = value.Latitude;
            location.Longitude = value.Longitude;
            location.Name = value.Name;
            location.Notes = value.Notes;
            location.PostalCode = value.PostalCode;
            location.ProgenyId = value.ProgenyId;
            location.State = value.State;
            location.StreetName = value.StreetName;
            location.Tags = value.Tags;

            _context.LocationsDb.Add(location);
            await _context.SaveChangesAsync();
            await _dataService.SetLocation(location.LocationId);

            TimeLineItem tItem = new TimeLineItem();
            tItem.ProgenyId = location.ProgenyId;
            tItem.AccessLevel = location.AccessLevel;
            tItem.ItemType = (int) KinaUnaTypes.TimeLineType.Location;
            tItem.ItemId = location.LocationId.ToString();
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            if (userinfo != null)
            {
                tItem.CreatedBy = userinfo.UserId;
            }

            tItem.CreatedTime = DateTime.UtcNow;
            if (location.Date.HasValue)
            {
                tItem.ProgenyTime = location.Date.Value;
            }
            else
            {
                tItem.ProgenyTime = DateTime.UtcNow;
            }

            await _context.TimeLineDb.AddAsync(tItem);
            await _context.SaveChangesAsync();
            await _dataService.SetTimeLineItem(tItem.TimeLineId);

            string title = "Location added for " + prog.NickName;
            if (userinfo != null)
            {
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName +
                                 " added a new location for " + prog.NickName;
                await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            }

            return Ok(location);
        }

        // PUT api/timeline/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Location value)
        {
            // Check if child exists.
            Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (prog != null)
            {
                // Check if user is allowed to edit locations for this child.
                if (!prog.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            Location location = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == id);
            if (location == null)
            {
                return NotFound();
            }

            location.AccessLevel = value.AccessLevel;
            location.Author = value.Author;
            location.City = value.City;
            location.Country = value.Country;
            location.County = value.County;
            location.Date = value.Date;
            location.DateAdded = value.DateAdded;
            location.District = value.District;
            location.HouseNumber = value.HouseNumber;
            location.Latitude = value.Latitude;
            location.Longitude = value.Longitude;
            location.Name = value.Name;
            location.Notes = value.Notes;
            location.PostalCode = value.PostalCode;
            location.ProgenyId = value.ProgenyId;
            location.State = value.State;
            location.StreetName = value.StreetName;
            location.Tags = value.Tags;

            _context.LocationsDb.Update(location);
            await _context.SaveChangesAsync();
            await _dataService.SetLocation(location.LocationId);

            // Update Timeline.
            TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                t.ItemId == location.LocationId.ToString() && t.ItemType == (int) KinaUnaTypes.TimeLineType.Location);
            if (tItem != null)
            {
                if (location.Date.HasValue)
                {
                    tItem.ProgenyTime = location.Date.Value;
                }

                tItem.AccessLevel = location.AccessLevel;
                _context.TimeLineDb.Update(tItem);
                await _context.SaveChangesAsync();
                await _dataService.SetTimeLineItem(tItem.TimeLineId);
            }

            UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
            string title = "Location edited for " + prog.NickName;
            string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " edited a location for " + prog.NickName;
            await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
            return Ok(location);
        }

        // DELETE api/timeline/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Location location = await _context.LocationsDb.SingleOrDefaultAsync(l => l.LocationId == id);
            if (location != null)
            {
                // Check if child exists.
                Progeny prog = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == location.ProgenyId);
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                if (prog != null)
                {
                    // Check if user is allowed to delete locations for this child.
                    if (!prog.IsInAdminList(userEmail))
                    {
                        return Unauthorized();
                    }
                }
                else
                {
                    return NotFound();
                }

                TimeLineItem tItem = await _context.TimeLineDb.SingleOrDefaultAsync(t =>
                    t.ItemId == location.LocationId.ToString() &&
                    t.ItemType == (int) KinaUnaTypes.TimeLineType.Location);
                if (tItem != null)
                {
                    if (location.Date.HasValue)
                    {
                        tItem.ProgenyTime = location.Date.Value;
                    }

                    tItem.AccessLevel = location.AccessLevel;
                    _context.TimeLineDb.Remove(tItem);
                    await _context.SaveChangesAsync();
                    await _dataService.RemoveTimeLineItem(tItem.TimeLineId, tItem.ItemType, tItem.ProgenyId);
                }

                _context.LocationsDb.Remove(location);
                await _context.SaveChangesAsync();
                await _dataService.RemoveLocation(location.LocationId, location.ProgenyId);

                UserInfo userinfo = await _dataService.GetUserInfoByEmail(userEmail);
                string title = "Location deleted for " + prog.NickName;
                string message = userinfo.FirstName + " " + userinfo.MiddleName + " " + userinfo.LastName + " deleted a location for " + prog.NickName + ". Location: " + location.Name;
                if (tItem != null)
                {
                    tItem.AccessLevel = 0;
                    await _azureNotifications.ProgenyUpdateNotification(title, message, tItem, userinfo.ProfilePicture);
                }

                return NoContent();
            }

            return NotFound();
        }


        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetLocationMobile(int id)
        {
            Location result = await _dataService.GetLocation(id); 
            if (result != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);

                if (userAccess != null || result.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(result);
                }

                return Unauthorized();
            }

            return NotFound();
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetLocationsListPage([FromQuery] int pageSize = 8,
            [FromQuery] int pageIndex = 1, [FromQuery] int progenyId = Constants.DefaultChildId,
            [FromQuery] int accessLevel = 5, [FromQuery] int sortBy = 1)
        {

            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(progenyId, userEmail);

            if (userAccess == null && progenyId != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            if (pageIndex < 1)
            {
                pageIndex = 1;
            }

            List<Location> allItems = await _dataService.GetLocationsList(progenyId);
            allItems = allItems.OrderBy(v => v.Date).ToList();

            if (sortBy == 1)
            {
                allItems.Reverse();
            }

            int locationCounter = 1;
            int locationsCount = allItems.Count;
            foreach (Location location in allItems)
            {
                if (sortBy == 1)
                {
                    location.LocationNumber = locationsCount - locationCounter + 1;
                }
                else
                {
                    location.LocationNumber = locationCounter;
                }

                locationCounter++;
            }

            var itemsOnPage = allItems
                .Skip(pageSize * (pageIndex - 1))
                .Take(pageSize)
                .ToList();

            LocationsListPage model = new LocationsListPage();
            model.LocationsList = itemsOnPage;
            model.TotalPages = (int) Math.Ceiling(allItems.Count / (double) pageSize);
            model.PageNumber = pageIndex;
            model.SortBy = sortBy;

            return Ok(model);
        }
    }
}
