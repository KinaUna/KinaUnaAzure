using System.Collections.Generic;
using System.Linq;
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
            _cacheOptionsSliding.SetSlidingExpiration(new System.TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }
        
        public List<Progeny> GetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList;
            string cachedProgenyList = _cache.GetString(Constants.AppName + "progenywhereadmin" + email);
            if (!string.IsNullOrEmpty(cachedProgenyList))
            {
                progenyList = JsonConvert.DeserializeObject<List<Progeny>>(cachedProgenyList);
            }
            else
            {
                progenyList = _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToList();
                _cache.SetString(Constants.AppName + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            }

            return progenyList;
        }

        public List<Progeny> SetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList = _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToList();
            _cache.SetString(Constants.AppName + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            
            return progenyList;
        }

        public Progeny GetProgeny(int id)
        {
            Progeny progeny;
            string cachedProgeny = _cache.GetString(Constants.AppName + "progeny" + id);
            if (!string.IsNullOrEmpty(cachedProgeny))
            {
                progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            }
            else
            {
                progeny = _context.ProgenyDb.AsNoTracking().SingleOrDefault(p => p.Id == id);
                _cache.SetString(Constants.AppName + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }

            return progeny;
        }

        public Progeny SetProgeny(int id)
        {
            Progeny progeny = _context.ProgenyDb.AsNoTracking().SingleOrDefault(p => p.Id == id);
            if (progeny != null)
            {
                _cache.SetString(Constants.AppName + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }
            else
            {
                _cache.Remove(Constants.AppName + "progeny" + id);
            }

            return progeny;
        }

        public void RemoveProgeny(int id)
        {
            _cache.Remove(Constants.AppName + "progeny" + id);
        }

        public List<UserAccess> GetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList;
            string cachedAccessList = _cache.GetString(Constants.AppName + "accessList" + progenyId);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            }

            return accessList;
        }

        public List<UserAccess> SetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList = _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public List<UserAccess> GetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList;
            string cachedAccessList = _cache.GetString(Constants.AppName + "usersaccesslist" + email);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToList();
                _cache.SetString(Constants.AppName + "usersaccesslist" + email, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            }

            return accessList;
        }

        public List<UserAccess> SetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList = _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToList();
            _cache.SetString(Constants.AppName + "usersaccesslist" + email, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public UserAccess GetUserAccess(int id)
        {
            UserAccess userAccess;
            string cachedUserAccess = _cache.GetString(Constants.AppName + "useraccess" + id);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.AccessId == id);
                _cache.SetString(Constants.AppName + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public UserAccess SetUserAccess(int id)
        {
            UserAccess userAccess = _context.UserAccessDb.AsNoTracking().SingleOrDefault(u => u.AccessId == id);
            _cache.SetString(Constants.AppName + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            _cache.SetString(Constants.AppName + "progenyuseraccess" + userAccess.ProgenyId + userAccess.UserId, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);

            return userAccess;
        }

        public void RemoveUserAccess(int id, int progenyId, string userId)
        {
            _cache.Remove(Constants.AppName + "useraccess" + id);
            _cache.Remove(Constants.AppName + "progenyuseraccess" + progenyId + userId);
            SetUsersUserAccessList(userId);
            SetProgenyUserAccessList(progenyId);
            SetProgenyUserIsAdmin(userId);
        }

        public UserAccess GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess;
            string cachedUserAccess = _cache.GetString(Constants.AppName + "progenyuseraccess" + progenyId + userEmail);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = _context.UserAccessDb.SingleOrDefault(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
                _cache.SetString(Constants.AppName + "progenyuseraccess" + progenyId + userEmail, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public UserInfo GetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo;
            string cachedUserInfo = _cache.GetString(Constants.AppName + "userinfobymail" + userEmail);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                _cache.SetString(Constants.AppName + "userinfobymail" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public UserInfo SetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            _cache.SetString(Constants.AppName + "userinfobymail" + userEmail, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            _cache.SetString(Constants.AppName + "userinfobyuserid" + userinfo.UserId, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            _cache.SetString(Constants.AppName + "userinfobyid" + userinfo.Id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);

            return userinfo;
        }

        public void RemoveUserInfoByEmail(string userEmail, string userId, int userinfoId)
        {
            _cache.Remove(Constants.AppName + "userinfobymail" + userEmail);
            _cache.Remove(Constants.AppName + "userinfobyuserid" + userId);
            _cache.Remove(Constants.AppName + "userinfobyid" + userinfoId);
        }

        public UserInfo GetUserInfoById(int id)
        {
            UserInfo userinfo;
            string cachedUserInfo = _cache.GetString(Constants.AppName + "userinfobyid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.Id == id);
                _cache.SetString(Constants.AppName + "userinfobyid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public UserInfo GetUserInfoByUserId(string id)
        {
            UserInfo userinfo;
            string cachedUserInfo = _cache.GetString(Constants.AppName + "userinfobyuserid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = _context.UserInfoDb.SingleOrDefault(u => u.UserId.ToUpper() == id.ToUpper());
                _cache.SetString(Constants.AppName + "userinfobyuserid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public Address GetAddressItem(int id)
        {
            Address address;
            string cachedAddress = _cache.GetString(Constants.AppName + "address" + id);
            if (!string.IsNullOrEmpty(cachedAddress))
            {
                address = JsonConvert.DeserializeObject<Address>(cachedAddress);
            }
            else
            {
                address = _context.AddressDb.AsNoTracking().SingleOrDefault(a => a.AddressId == id);
                _cache.SetString(Constants.AppName + "address" + id, JsonConvert.SerializeObject(address), _cacheOptionsSliding);

            }

            return address;
        }

        public Address SetAddressItem(int id)
        {
            Address addressItem = _context.AddressDb.AsNoTracking().SingleOrDefault(a => a.AddressId == id);
            _cache.SetString(Constants.AppName + "address" + id, JsonConvert.SerializeObject(addressItem), _cacheOptionsSliding);
            
            return addressItem;
        }

        public void RemoveAddressItem(int id)
        {
            _cache.Remove(Constants.AppName + "address" + id);
        }

        public CalendarItem GetCalendarItem(int id)
        {
            CalendarItem calendarItem;
            string cachedCalendarItem = _cache.GetString(Constants.AppName + "calendaritem" + id);
            if (!string.IsNullOrEmpty(cachedCalendarItem))
            {
                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(cachedCalendarItem);
            }
            else
            {
                calendarItem = _context.CalendarDb.AsNoTracking().SingleOrDefault(l => l.EventId == id);
                _cache.SetString(Constants.AppName + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);
            }

            return calendarItem;
        }

        public CalendarItem SetCalendarItem(int id)
        {
            CalendarItem calendarItem = _context.CalendarDb.AsNoTracking().SingleOrDefault(l => l.EventId == id);
            _cache.SetString(Constants.AppName + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);

            List<CalendarItem> calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == calendarItem.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "calendarlist" + calendarItem.ProgenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            return calendarItem;
        }

        public void RemoveCalendarItem(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "calendaritem" + id);

            List<CalendarItem> calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
        }

        public List<CalendarItem> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList;
            string cachedCalendar = _cache.GetString(Constants.AppName + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }
            else
            {
                calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            }

            return calendarList;
        }

        public Contact GetContact(int id)
        {
            Contact contact;
            string cachedContact = _cache.GetString(Constants.AppName + "contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }
            else
            {
                contact = _context.ContactsDb.AsNoTracking().SingleOrDefault(c => c.ContactId == id);
                _cache.SetString(Constants.AppName + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);
            }

            return contact;
        }

        public Contact SetContact(int id)
        {
            Contact contact = _context.ContactsDb.AsNoTracking().SingleOrDefault(c => c.ContactId == id);
            _cache.SetString(Constants.AppName + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);

            List<Contact> contactsList = _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == contact.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "contactslist" + contact.ProgenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);

            return contact;
        }

        public void RemoveContact(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "contact" + id);

            List<Contact> contactsList = _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
        }

        public List<Contact> GetContactsList(int progenyId)
        {
            List<Contact> contactsList;
            string cachedContactsList = _cache.GetString(Constants.AppName + "contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }
            else
            {
                contactsList = _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
            }

            return contactsList;
        }

        public Friend GetFriend(int id)
        {
            Friend friend;
            string cachedFriend = _cache.GetString(Constants.AppName + "friend" + id);
            if (!string.IsNullOrEmpty(cachedFriend))
            {
                friend = JsonConvert.DeserializeObject<Friend>(cachedFriend);
            }
            else
            {
                friend = _context.FriendsDb.AsNoTracking().SingleOrDefault(f => f.FriendId == id);
                _cache.SetString(Constants.AppName + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);
            }

            return friend;
        }

        public Friend SetFriend(int id)
        {
            Friend friend = _context.FriendsDb.AsNoTracking().SingleOrDefault(f => f.FriendId == id);
            _cache.SetString(Constants.AppName + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);

            List<Friend> friendsList = _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == friend.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "friendslist" + friend.ProgenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);

            return friend;
        }

        public void RemoveFriend(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "friend" + id);

            List<Friend> friendsList = _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
        }

        public List<Friend> GetFriendsList(int progenyId)
        {
            List<Friend> friendsList;
            string cachedFriendsList = _cache.GetString(Constants.AppName + "friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }
            else
            {
                friendsList = _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
            }

            return friendsList;
        }

        public Location GetLocation(int id)
        {
            Location location;
            string cachedLocation = _cache.GetString(Constants.AppName + "location" + id);
            if (!string.IsNullOrEmpty(cachedLocation))
            {
                location = JsonConvert.DeserializeObject<Location>(cachedLocation);
            }
            else
            {
                location = _context.LocationsDb.AsNoTracking().SingleOrDefault(l => l.LocationId == id);
                _cache.SetString(Constants.AppName + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);
            }

            return location;
        }

        public Location SetLocation(int id)
        {
            Location location = _context.LocationsDb.AsNoTracking().SingleOrDefault(l => l.LocationId == id);
            _cache.SetString(Constants.AppName + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);

            List<Location> locationsList = _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == location.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "locationslist" + location.ProgenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);

            return location;
        }
        
        public void RemoveLocation(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "location" + id);

            List<Location> locationsList = _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
        }

        public List<Location> GetLocationsList(int progenyId)
        {
            List<Location> locationsList;
            string cachedLocationsList = _cache.GetString(Constants.AppName + "locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }
            else
            {
                locationsList = _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
            }

            return locationsList;
        }

        public TimeLineItem GetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = _cache.GetString(Constants.AppName + "timelineitem" + id);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = _context.TimeLineDb.AsNoTracking().SingleOrDefault(t => t.TimeLineId == id);
                _cache.SetString(Constants.AppName + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            }

            return timeLineItem;
        }

        public TimeLineItem SetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem = _context.TimeLineDb.AsNoTracking().SingleOrDefault(t => t.TimeLineId == id);
            _cache.SetString(Constants.AppName + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            _cache.SetString(Constants.AppName + "timelineitembyid" + timeLineItem.ItemId + "type" + timeLineItem.ItemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            List<TimeLineItem> timeLineList = _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == timeLineItem.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "timelinelist" + timeLineItem.ProgenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
            
            return timeLineItem;
        }

        public void RemoveTimeLineItem(int timeLineItemId, int timeLineType, int progenyId)
        {
            _cache.Remove(Constants.AppName + "timelineitem" + timeLineItemId);
            _cache.Remove(Constants.AppName + "timelineitembyid" + timeLineItemId + "type" + timeLineType);
            List<TimeLineItem> timeLineList = _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
        }

        public TimeLineItem GetTimeLineItemByItemId(string itemId, int itemType)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = _cache.GetString(Constants.AppName + "timelineitembyid" + itemId + itemType);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = _context.TimeLineDb.SingleOrDefault(t => t.ItemId == itemId && t.ItemType == itemType);
                _cache.SetString(Constants.AppName + "timelineitembyid" + itemId + "type" + itemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            }

            return timeLineItem;
        }

        public List<TimeLineItem> GetTimeLineList(int progenyId)
        {
            List<TimeLineItem> timeLineList;
            string cachedTimeLineList = _cache.GetString(Constants.AppName + "timelinelist" + progenyId);
            if (!string.IsNullOrEmpty(cachedTimeLineList))
            {
                timeLineList = JsonConvert.DeserializeObject<List<TimeLineItem>>(cachedTimeLineList);
            }
            else
            {
                timeLineList = _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
            }

            return timeLineList;
        }

        public Measurement GetMeasurement(int id)
        {
            Measurement measurement;
            string cachedMeasurement = _cache.GetString(Constants.AppName + "measurement" + id);
            if (!string.IsNullOrEmpty(cachedMeasurement))
            {
                measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            }
            else
            {
                measurement = _context.MeasurementsDb.AsNoTracking().SingleOrDefault(m => m.MeasurementId == id);
                _cache.SetString(Constants.AppName + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);
            }

            return measurement;
        }

        public Measurement SetMeasurement(int id)
        {
            Measurement measurement = _context.MeasurementsDb.AsNoTracking().SingleOrDefault(m => m.MeasurementId == id);
            _cache.SetString(Constants.AppName + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);

            List<Measurement> measurementsList = _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == measurement.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "measurementslist" + measurement.ProgenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);

            return measurement;
        }

        public void RemoveMeasurement(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "measurement" + id);

            List<Measurement> measurementsList = _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
        }

        public List<Measurement> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList;
            string cachedMeasurementsList = _cache.GetString(Constants.AppName + "measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }
            else
            {
                measurementsList = _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            }

            return measurementsList;
        }

        public Note GetNote(int id)
        {
            Note note;
            string cachedNote = _cache.GetString(Constants.AppName + "note" + id);
            if (!string.IsNullOrEmpty(cachedNote))
            {
                note = JsonConvert.DeserializeObject<Note>(cachedNote);
            }
            else
            {
                note = _context.NotesDb.AsNoTracking().SingleOrDefault(n => n.NoteId == id);
                _cache.SetString(Constants.AppName + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);
            }

            return note;
        }

        public Note SetNote(int id)
        {
            Note note = _context.NotesDb.AsNoTracking().SingleOrDefault(n => n.NoteId == id);
            _cache.SetString(Constants.AppName + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);

            List<Contact> contactsList = _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == note.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "contactslist" + note.ProgenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);

            return note;
        }

        public void RemoveNote(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "note" + id);

            List<Note> notesList = _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
        }

        public List<Note> GetNotesList(int progenyId)
        {
            List<Note> notesList;
            string cachedNotesList = _cache.GetString(Constants.AppName + "noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonConvert.DeserializeObject<List<Note>>(cachedNotesList);
            }
            else
            {
                notesList = _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
            }

            return notesList;
        }

        public Skill GetSkill(int id)
        {
            Skill skill;
            string cachedSkill = _cache.GetString(Constants.AppName + "skill" + id);
            if (!string.IsNullOrEmpty(cachedSkill))
            {
                skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            }
            else
            {
                skill = _context.SkillsDb.AsNoTracking().SingleOrDefault(s => s.SkillId == id);
                _cache.SetString(Constants.AppName + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);
            }

            return skill;
        }

        public Skill SetSkill(int id)
        {
            Skill skill = _context.SkillsDb.AsNoTracking().SingleOrDefault(s => s.SkillId == id);
            _cache.SetString(Constants.AppName + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);

            List<Skill> skillsList = _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == skill.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "skillslist" + skill.ProgenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);

            return skill;
        }

        public void RemoveSkill(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "skill" + id);

            List<Skill> skillsList = _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
        }

        public List<Skill> GetSkillsList(int progenyId)
        {
            List<Skill> skillsList;
            string cachedSkillsList = _cache.GetString(Constants.AppName + "skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }
            else
            {
                skillsList = _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
            }

            return skillsList;
        }

        public Sleep GetSleep(int id)
        {
            Sleep sleep;
            string cachedSleep = _cache.GetString(Constants.AppName + "sleep" + id);
            if (!string.IsNullOrEmpty(cachedSleep))
            {
                sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            }
            else
            {
                sleep = _context.SleepDb.AsNoTracking().SingleOrDefault(s => s.SleepId == id);
                _cache.SetString(Constants.AppName + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);
            }

            return sleep;
        }

        public Sleep SetSleep(int id)
        {
            Sleep sleep = _context.SleepDb.AsNoTracking().SingleOrDefault(s => s.SleepId == id);
            _cache.SetString(Constants.AppName + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);

            List<Sleep> sleepList = _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == sleep.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "sleeplist" + sleep.ProgenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);

            return sleep;
        }

        public void RemoveSleep(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "sleep" + id);

            List<Sleep> sleepList = _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
        }

        public List<Sleep> GetSleepList(int progenyId)
        {
            List<Sleep> sleepList;
            string cachedSleepList = _cache.GetString(Constants.AppName + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }
            else
            {
                sleepList = _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
            }

            return sleepList;
        }

        public Vaccination GetVaccination(int id)
        {
            Vaccination vaccination;
            string cachedVaccination = _cache.GetString(Constants.AppName + "vaccination" + id);
            if (!string.IsNullOrEmpty(cachedVaccination))
            {
                vaccination = JsonConvert.DeserializeObject<Vaccination>(cachedVaccination);
            }
            else
            {
                vaccination = _context.VaccinationsDb.AsNoTracking().SingleOrDefault(v => v.VaccinationId == id);
                _cache.SetString(Constants.AppName + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);
            }

            return vaccination;
        }

        public Vaccination SetVaccination(int id)
        {
            Vaccination vaccination = _context.VaccinationsDb.AsNoTracking().SingleOrDefault(v => v.VaccinationId == id);
            _cache.SetString(Constants.AppName + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);

            List<Vaccination> vaccinationsList = _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == vaccination.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "vaccinationslist" + vaccination.ProgenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);

            return vaccination;
        }

        public void RemoveVaccination(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "vaccination" + id);

            List<Vaccination> vaccinationsList = _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
        }

        public List<Vaccination> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> vaccinationsList;
            string cachedVaccinationsList = _cache.GetString(Constants.AppName + "vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(cachedVaccinationsList);
            }
            else
            {
                vaccinationsList = _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
            }

            return vaccinationsList;
        }

        public VocabularyItem GetVocabularyItem(int id)
        {
            VocabularyItem vocabularyItem;
            string cachedVocabularyItem = _cache.GetString(Constants.AppName + "vocabularyitem" + id);
            if (!string.IsNullOrEmpty(cachedVocabularyItem))
            {
                vocabularyItem = JsonConvert.DeserializeObject<VocabularyItem>(cachedVocabularyItem);
            }
            else
            {
                vocabularyItem = _context.VocabularyDb.AsNoTracking().SingleOrDefault(v => v.WordId == id);
                _cache.SetString(Constants.AppName + "vocabularyitem" + id, JsonConvert.SerializeObject(vocabularyItem), _cacheOptionsSliding);
            }

            return vocabularyItem;
        }

        public VocabularyItem SetVocabularyItem(int id)
        {
            VocabularyItem word = _context.VocabularyDb.AsNoTracking().SingleOrDefault(w => w.WordId == id);
            _cache.SetString(Constants.AppName + "vocabularyitem" + id, JsonConvert.SerializeObject(word), _cacheOptions);

            List<VocabularyItem> wordList = _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == word.ProgenyId).ToList();
            _cache.SetString(Constants.AppName + "vocabularylist" + word.ProgenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);

            return word;
        }

        public void RemoveVocabularyItem(int id, int progenyId)
        {
            _cache.Remove(Constants.AppName + "vocabularyitem" + id);

            List<VocabularyItem> wordList = _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + "vocabularylist" + progenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);
        }

        public List<VocabularyItem> GetVocabularyList(int progenyId)
        {
            List<VocabularyItem> vocabularyList;
            string cachedVocabularyList = _cache.GetString(Constants.AppName + "vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(cachedVocabularyList);
            }
            else
            {
                vocabularyList = _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToList();
                _cache.SetString(Constants.AppName + "vocabularylist" + progenyId, JsonConvert.SerializeObject(vocabularyList), _cacheOptionsSliding);
            }

            return vocabularyList;
        }
    }
}
