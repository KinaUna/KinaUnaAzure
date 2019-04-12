using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace KinaUnaProgenyApi.Services
{
    public class DataService: IDataService
    {
        private readonly ProgenyDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new DistributedCacheEntryOptions();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new DistributedCacheEntryOptions();

        public DataService(ProgenyDbContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _cacheOptions.SetAbsoluteExpiration(new System.TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(7, 0, 0, 0)); // Expire after a week.
        }
        
        public async Task<List<Progeny>> GetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList;
            string cachedProgenyList = await _cache.GetStringAsync(Constants.AppName + "progenywhereadmin" + email);
            if (!string.IsNullOrEmpty(cachedProgenyList))
            {
                progenyList = JsonConvert.DeserializeObject<List<Progeny>>(cachedProgenyList);
            }
            else
            {
                progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            }

            return progenyList;
        }

        public async Task<List<Progeny>> SetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            
            return progenyList;
        }

        public async Task<Progeny> GetProgeny(int id)
        {
            Progeny progeny;
            string cachedProgeny = await _cache.GetStringAsync(Constants.AppName + "progeny" + id);
            if (!string.IsNullOrEmpty(cachedProgeny))
            {
                progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            }
            else
            {
                progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
                await _cache.SetStringAsync(Constants.AppName + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }

            return progeny;
        }

        public async Task<Progeny> SetProgeny(int id)
        {
            Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            if (progeny != null)
            {
                await _cache.SetStringAsync(Constants.AppName + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }
            else
            {
                await _cache.RemoveAsync(Constants.AppName + "progeny" + id);
            }

            return progeny;
        }

        public async Task RemoveProgeny(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + "progeny" + id);
        }

        public async Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList;
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + "accessList" + progenyId);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> SetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public async Task<List<UserAccess>> GetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList;
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + "usersaccesslist" + email);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "usersaccesslist" + email, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> SetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "usersaccesslist" + email, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public async Task<UserAccess> GetUserAccess(int id)
        {
            UserAccess userAccess;
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + "useraccess" + id);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
                await _cache.SetStringAsync(Constants.AppName + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public async Task<UserAccess> SetUserAccess(int id)
        {
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
            await _cache.SetStringAsync(Constants.AppName + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + "progenyuseraccess" + userAccess.ProgenyId + userAccess.UserId, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);

            return userAccess;
        }

        public async Task RemoveUserAccess(int id, int progenyId, string userId)
        {
            await _cache.RemoveAsync(Constants.AppName + "useraccess" + id);
            await _cache.RemoveAsync(Constants.AppName + "progenyuseraccess" + progenyId + userId);
            await SetUsersUserAccessList(userId);
            await SetProgenyUserAccessList(progenyId);
            await SetProgenyUserIsAdmin(userId);
        }

        public async Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess;
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + "progenyuseraccess" + progenyId + userEmail);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + "progenyuseraccess" + progenyId + userEmail, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public async Task<UserInfo> GetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + "userinfobymail" + userEmail);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + "userinfobymail" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<UserInfo> SetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            await _cache.SetStringAsync(Constants.AppName + "userinfobymail" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + "userinfobyuserid" + userinfo.UserId, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + "userinfobyid" + userinfo.Id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);

            return userinfo;
        }

        public async Task RemoveUserInfoByEmail(string userEmail, string userId, int userinfoId)
        {
            await _cache.RemoveAsync(Constants.AppName + "userinfobymail" + userEmail);
            await _cache.RemoveAsync(Constants.AppName + "userinfobyuserid" + userId);
            await _cache.RemoveAsync(Constants.AppName + "userinfobyid" + userinfoId);
        }

        public async Task<UserInfo> GetUserInfoById(int id)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + "userinfobyid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.Id == id);
                await _cache.SetStringAsync(Constants.AppName + "userinfobyid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<UserInfo> GetUserInfoByUserId(string id)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + "userinfobyuserid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserId.ToUpper() == id.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + "userinfobyuserid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<Address> GetAddressItem(int id)
        {
            Address address;
            string cachedAddress = await _cache.GetStringAsync(Constants.AppName + "address" + id);
            if (!string.IsNullOrEmpty(cachedAddress))
            {
                address = JsonConvert.DeserializeObject<Address>(cachedAddress);
            }
            else
            {
                address = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
                await _cache.SetStringAsync(Constants.AppName + "address" + id, JsonConvert.SerializeObject(address), _cacheOptionsSliding);

            }

            return address;
        }

        public async Task<Address> SetAddressItem(int id)
        {
            Address addressItem = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
            await _cache.SetStringAsync(Constants.AppName + "address" + id, JsonConvert.SerializeObject(addressItem), _cacheOptionsSliding);
            
            return addressItem;
        }

        public async Task RemoveAddressItem(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + "address" + id);
        }

        public async Task<CalendarItem> GetCalendarItem(int id)
        {
            CalendarItem calendarItem;
            string cachedCalendarItem = await _cache.GetStringAsync(Constants.AppName + "calendaritem" + id);
            if (!string.IsNullOrEmpty(cachedCalendarItem))
            {
                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(cachedCalendarItem);
            }
            else
            {
                calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
                await _cache.SetStringAsync(Constants.AppName + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> SetCalendarItem(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
            await _cache.SetStringAsync(Constants.AppName + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);

            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == calendarItem.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "calendarlist" + calendarItem.ProgenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            return calendarItem;
        }

        public async Task RemoveCalendarItem(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "calendaritem" + id);

            List<CalendarItem> calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList;
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }
            else
            {
                calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            }

            return calendarList;
        }

        public async Task<Contact> GetContact(int id)
        {
            Contact contact;
            string cachedContact = await _cache.GetStringAsync(Constants.AppName + "contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }
            else
            {
                contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
                await _cache.SetStringAsync(Constants.AppName + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);
            }

            return contact;
        }

        public async Task<Contact> SetContact(int id)
        {
            Contact contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            await _cache.SetStringAsync(Constants.AppName + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);

            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == contact.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "contactslist" + contact.ProgenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);

            return contact;
        }

        public async Task RemoveContact(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "contact" + id);

            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
        }

        public async Task<List<Contact>> GetContactsList(int progenyId)
        {
            List<Contact> contactsList;
            string cachedContactsList = await _cache.GetStringAsync(Constants.AppName + "contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }
            else
            {
                contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
            }

            return contactsList;
        }

        public async Task<Friend> GetFriend(int id)
        {
            Friend friend;
            string cachedFriend = await _cache.GetStringAsync(Constants.AppName + "friend" + id);
            if (!string.IsNullOrEmpty(cachedFriend))
            {
                friend = JsonConvert.DeserializeObject<Friend>(cachedFriend);
            }
            else
            {
                friend = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
                await _cache.SetStringAsync(Constants.AppName + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);
            }

            return friend;
        }

        public async Task<Friend> SetFriend(int id)
        {
            Friend friend = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            await _cache.SetStringAsync(Constants.AppName + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);

            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == friend.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "friendslist" + friend.ProgenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);

            return friend;
        }

        public async Task RemoveFriend(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "friend" + id);

            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId)
        {
            List<Friend> friendsList;
            string cachedFriendsList = await _cache.GetStringAsync(Constants.AppName + "friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }
            else
            {
                friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
            }

            return friendsList;
        }

        public async Task<Location> GetLocation(int id)
        {
            Location location;
            string cachedLocation = await _cache.GetStringAsync(Constants.AppName + "location" + id);
            if (!string.IsNullOrEmpty(cachedLocation))
            {
                location = JsonConvert.DeserializeObject<Location>(cachedLocation);
            }
            else
            {
                location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
                _cache.SetString(Constants.AppName + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);
            }

            return location;
        }

        public async Task<Location> SetLocation(int id)
        {
            Location location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            await _cache.SetStringAsync(Constants.AppName + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);

            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == location.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "locationslist" + location.ProgenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);

            return location;
        }
        
        public async Task RemoveLocation(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "location" + id);

            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
        }

        public async Task<List<Location>> GetLocationsList(int progenyId)
        {
            List<Location> locationsList;
            string cachedLocationsList = await _cache.GetStringAsync(Constants.AppName + "locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }
            else
            {
                locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
            }

            return locationsList;
        }

        public async Task<TimeLineItem> GetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = await _cache.GetStringAsync(Constants.AppName + "timelineitem" + id);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(t => t.TimeLineId == id);
                await _cache.SetStringAsync(Constants.AppName + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            }

            return timeLineItem;
        }

        public async Task<TimeLineItem> SetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(t => t.TimeLineId == id);
            await _cache.SetStringAsync(Constants.AppName + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + "timelineitembyid" + timeLineItem.ItemId + "type" + timeLineItem.ItemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == timeLineItem.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "timelinelist" + timeLineItem.ProgenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
            
            return timeLineItem;
        }

        public async Task RemoveTimeLineItem(int timeLineItemId, int timeLineType, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "timelineitem" + timeLineItemId);
            await _cache.RemoveAsync(Constants.AppName + "timelineitembyid" + timeLineItemId + "type" + timeLineType);
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
        }

        public async Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = await _cache.GetStringAsync(Constants.AppName + "timelineitembyid" + itemId + itemType);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == itemId && t.ItemType == itemType);
                await _cache.SetStringAsync(Constants.AppName + "timelineitembyid" + itemId + "type" + itemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            }

            return timeLineItem;
        }

        public async Task<List<TimeLineItem>> GetTimeLineList(int progenyId)
        {
            List<TimeLineItem> timeLineList;
            string cachedTimeLineList = await _cache.GetStringAsync(Constants.AppName + "timelinelist" + progenyId);
            if (!string.IsNullOrEmpty(cachedTimeLineList))
            {
                timeLineList = JsonConvert.DeserializeObject<List<TimeLineItem>>(cachedTimeLineList);
            }
            else
            {
                timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
            }

            return timeLineList;
        }

        public async Task<Measurement> GetMeasurement(int id)
        {
            Measurement measurement;
            string cachedMeasurement = await _cache.GetStringAsync(Constants.AppName + "measurement" + id);
            if (!string.IsNullOrEmpty(cachedMeasurement))
            {
                measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            }
            else
            {
                measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
                await _cache.SetStringAsync(Constants.AppName + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);
            }

            return measurement;
        }

        public async Task<Measurement> SetMeasurement(int id)
        {
            Measurement measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            await _cache.SetStringAsync(Constants.AppName + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);

            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == measurement.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "measurementslist" + measurement.ProgenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);

            return measurement;
        }

        public async Task RemoveMeasurement(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "measurement" + id);

            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList;
            string cachedMeasurementsList = await _cache.GetStringAsync(Constants.AppName + "measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }
            else
            {
                measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            }

            return measurementsList;
        }

        public async Task<Note> GetNote(int id)
        {
            Note note;
            string cachedNote = await _cache.GetStringAsync(Constants.AppName + "note" + id);
            if (!string.IsNullOrEmpty(cachedNote))
            {
                note = JsonConvert.DeserializeObject<Note>(cachedNote);
            }
            else
            {
                note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
                await _cache.SetStringAsync(Constants.AppName + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);
            }

            return note;
        }

        public async Task<Note> SetNote(int id)
        {
            Note note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            await _cache.SetStringAsync(Constants.AppName + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);

            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == note.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "noteslist" + note.ProgenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);

            return note;
        }

        public async Task RemoveNote(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "note" + id);

            List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
        }

        public async Task<List<Note>> GetNotesList(int progenyId)
        {
            List<Note> notesList;
            string cachedNotesList = await _cache.GetStringAsync(Constants.AppName + "noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonConvert.DeserializeObject<List<Note>>(cachedNotesList);
            }
            else
            {
                notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
            }

            return notesList;
        }

        public async Task<Skill> GetSkill(int id)
        {
            Skill skill;
            string cachedSkill = await _cache.GetStringAsync(Constants.AppName + "skill" + id);
            if (!string.IsNullOrEmpty(cachedSkill))
            {
                skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            }
            else
            {
                skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
                await _cache.SetStringAsync(Constants.AppName + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);
            }

            return skill;
        }

        public async Task<Skill> SetSkill(int id)
        {
            Skill skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            await _cache.SetStringAsync(Constants.AppName + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);

            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == skill.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "skillslist" + skill.ProgenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);

            return skill;
        }

        public async Task RemoveSkill(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "skill" + id);

            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId)
        {
            List<Skill> skillsList;
            string cachedSkillsList = await _cache.GetStringAsync(Constants.AppName + "skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }
            else
            {
                skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
            }

            return skillsList;
        }

        public async Task<Sleep> GetSleep(int id)
        {
            Sleep sleep;
            string cachedSleep = await _cache.GetStringAsync(Constants.AppName + "sleep" + id);
            if (!string.IsNullOrEmpty(cachedSleep))
            {
                sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            }
            else
            {
                sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
                await _cache.SetStringAsync(Constants.AppName + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);
            }

            return sleep;
        }

        public async Task<Sleep> SetSleep(int id)
        {
            Sleep sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            await _cache.SetStringAsync(Constants.AppName + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);

            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == sleep.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "sleeplist" + sleep.ProgenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);

            return sleep;
        }

        public async Task RemoveSleep(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "sleep" + id);

            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId)
        {
            List<Sleep> sleepList;
            string cachedSleepList = await _cache.GetStringAsync(Constants.AppName + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }
            else
            {
                sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
            }

            return sleepList;
        }

        public async Task<Vaccination> GetVaccination(int id)
        {
            Vaccination vaccination;
            string cachedVaccination = await _cache.GetStringAsync(Constants.AppName + "vaccination" + id);
            if (!string.IsNullOrEmpty(cachedVaccination))
            {
                vaccination = JsonConvert.DeserializeObject<Vaccination>(cachedVaccination);
            }
            else
            {
                vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
                await _cache.SetStringAsync(Constants.AppName + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);
            }

            return vaccination;
        }

        public async Task<Vaccination> SetVaccination(int id)
        {
            Vaccination vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            await _cache.SetStringAsync(Constants.AppName + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);

            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == vaccination.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "vaccinationslist" + vaccination.ProgenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);

            return vaccination;
        }

        public async Task RemoveVaccination(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "vaccination" + id);

            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> vaccinationsList;
            string cachedVaccinationsList = await _cache.GetStringAsync(Constants.AppName + "vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(cachedVaccinationsList);
            }
            else
            {
                vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
            }

            return vaccinationsList;
        }

        public async Task<VocabularyItem> GetVocabularyItem(int id)
        {
            VocabularyItem vocabularyItem;
            string cachedVocabularyItem = await _cache.GetStringAsync(Constants.AppName + "vocabularyitem" + id);
            if (!string.IsNullOrEmpty(cachedVocabularyItem))
            {
                vocabularyItem = JsonConvert.DeserializeObject<VocabularyItem>(cachedVocabularyItem);
            }
            else
            {
                vocabularyItem = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == id);
                await _cache.SetStringAsync(Constants.AppName + "vocabularyitem" + id, JsonConvert.SerializeObject(vocabularyItem), _cacheOptionsSliding);
            }

            return vocabularyItem;
        }

        public async Task<VocabularyItem> SetVocabularyItem(int id)
        {
            VocabularyItem word = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            await _cache.SetStringAsync(Constants.AppName + "vocabularyitem" + id, JsonConvert.SerializeObject(word), _cacheOptions);

            List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == word.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "vocabularylist" + word.ProgenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);

            return word;
        }

        public async Task RemoveVocabularyItem(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + "vocabularyitem" + id);

            List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + "vocabularylist" + progenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);
        }

        public async Task<List<VocabularyItem>> GetVocabularyList(int progenyId)
        {
            List<VocabularyItem> vocabularyList;
            string cachedVocabularyList = await _cache.GetStringAsync(Constants.AppName + "vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(cachedVocabularyList);
            }
            else
            {
                vocabularyList = await _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + "vocabularylist" + progenyId, JsonConvert.SerializeObject(vocabularyList), _cacheOptionsSliding);
            }

            return vocabularyList;
        }
    }
}
