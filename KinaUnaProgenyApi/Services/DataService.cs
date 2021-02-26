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
            string cachedProgenyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email);
            if (!string.IsNullOrEmpty(cachedProgenyList))
            {
                progenyList = JsonConvert.DeserializeObject<List<Progeny>>(cachedProgenyList);
            }
            else
            {
                progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            }

            return progenyList;
        }

        public async Task<List<Progeny>> SetProgenyUserIsAdmin(string email)
        {
            List<Progeny> progenyList = await _context.ProgenyDb.AsNoTracking().Where(p => p.Admins.Contains(email)).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenywhereadmin" + email, JsonConvert.SerializeObject(progenyList), _cacheOptionsSliding);
            
            return progenyList;
        }

        public async Task<Progeny> GetProgeny(int id)
        {
            Progeny progeny;
            string cachedProgeny = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            if (!string.IsNullOrEmpty(cachedProgeny))
            {
                progeny = JsonConvert.DeserializeObject<Progeny>(cachedProgeny);
            }
            else
            {
                progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }

            return progeny;
        }

        public async Task<Progeny> SetProgeny(int id)
        {
            Progeny progeny = await _context.ProgenyDb.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
            if (progeny != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id, JsonConvert.SerializeObject(progeny), _cacheOptionsSliding);
            }
            else
            {
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
            }

            return progeny;
        }

        public async Task RemoveProgeny(int id)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progeny" + id);
        }

        public async Task<List<UserAccess>> GetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList;
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId);
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> SetProgenyUserAccessList(int progenyId)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "accessList" + progenyId, JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public async Task<List<UserAccess>> GetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList;
            string cachedAccessList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper());
            if (!string.IsNullOrEmpty(cachedAccessList))
            {
                accessList = JsonConvert.DeserializeObject<List<UserAccess>>(cachedAccessList);
            }
            else
            {
                accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper(), JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            }

            return accessList;
        }

        public async Task<List<UserAccess>> SetUsersUserAccessList(string email)
        {
            List<UserAccess> accessList = await _context.UserAccessDb.AsNoTracking().Where(u => u.UserId.ToUpper() == email.ToUpper()).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "usersaccesslist" + email.ToUpper(), JsonConvert.SerializeObject(accessList), _cacheOptionsSliding);
            
            return accessList;
        }

        public async Task<UserAccess> GetUserAccess(int id)
        {
            UserAccess userAccess;
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }
        
        public async Task<UserAccess> SetUserAccess(int id)
        {
            UserAccess userAccess = await _context.UserAccessDb.AsNoTracking().SingleOrDefaultAsync(u => u.AccessId == id);
            if (userAccess != null)
            {
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + userAccess.ProgenyId + userAccess.UserId, JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }
            else
            {
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
            }

            return userAccess;
        }

        public async Task<UserAccess> AddUserAccess(UserAccess userAccess)
        {
            await _context.UserAccessDb.AddAsync(userAccess);
            await _context.SaveChangesAsync();

            await SetUserAccess(userAccess.AccessId);
            await SetUsersUserAccessList(userAccess.UserId);
            await SetProgenyUserAccessList(userAccess.ProgenyId);
            await SetProgenyUserIsAdmin(userAccess.UserId);
            return userAccess;
        }

        public async Task<UserAccess> UpdateUserAccess(UserAccess userAccess)
        {
            _context.UserAccessDb.Update(userAccess);
            await _context.SaveChangesAsync();

            await SetUserAccess(userAccess.AccessId);
            await SetUsersUserAccessList(userAccess.UserId);
            await SetProgenyUserAccessList(userAccess.ProgenyId);
            await SetProgenyUserIsAdmin(userAccess.UserId);
            return userAccess;
        }

        public async Task RemoveUserAccess(int id, int progenyId, string userId)
        {
            UserAccess deleteUserAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.AccessId == id && u.ProgenyId == progenyId);
            if (deleteUserAccess != null)
            {
                _context.UserAccessDb.Remove(deleteUserAccess);
                await _context.SaveChangesAsync();
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "useraccess" + id);
                await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userId);
                await SetUsersUserAccessList(userId);
                await SetProgenyUserAccessList(progenyId);
                await SetProgenyUserIsAdmin(userId);
            }
            
        }

        public async Task<UserAccess> GetProgenyUserAccessForUser(int progenyId, string userEmail)
        {
            UserAccess userAccess;
            string cachedUserAccess = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper());
            if (!string.IsNullOrEmpty(cachedUserAccess))
            {
                userAccess = JsonConvert.DeserializeObject<UserAccess>(cachedUserAccess);
            }
            else
            {
                userAccess = await _context.UserAccessDb.SingleOrDefaultAsync(u => u.ProgenyId == progenyId && u.UserId.ToUpper() == userEmail.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progenyuseraccess" + progenyId + userEmail.ToUpper(), JsonConvert.SerializeObject(userAccess), _cacheOptionsSliding);
            }

            return userAccess;
        }

        public async Task<List<UserInfo>> GetAllUserInfos()
        {
            List<UserInfo> userinfo;
            string cachedUserInfos = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "alluserinfos");
            if (!string.IsNullOrEmpty(cachedUserInfos))
            {
                userinfo = JsonConvert.DeserializeObject<List<UserInfo>>(cachedUserInfos);
            }
            else
            {
                userinfo = await _context.UserInfoDb.ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "alluserinfos", JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task RefreshAllUserInfos()
        {
            List<UserInfo> userinfo = await _context.UserInfoDb.ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "alluserinfos", JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            
        }

        public async Task<UserInfo> GetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper());
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper(), JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<UserInfo> SetUserInfoByEmail(string userEmail)
        {
            UserInfo userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserEmail.ToUpper() == userEmail.ToUpper());
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper(), JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userinfo.UserId, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + userinfo.Id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            await RefreshAllUserInfos();
            return userinfo;
        }

        public async Task RemoveUserInfoByEmail(string userEmail, string userId, int userinfoId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobymail" + userEmail.ToUpper());
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + userId);
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + userinfoId);
            await RefreshAllUserInfos();
        }

        public async Task<UserInfo> GetUserInfoById(int id)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.Id == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<UserInfo> GetUserInfoByUserId(string id)
        {
            UserInfo userinfo;
            string cachedUserInfo = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + id);
            if (!string.IsNullOrEmpty(cachedUserInfo))
            {
                userinfo = JsonConvert.DeserializeObject<UserInfo>(cachedUserInfo);
            }
            else
            {
                userinfo = await _context.UserInfoDb.SingleOrDefaultAsync(u => u.UserId.ToUpper() == id.ToUpper());
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "userinfobyuserid" + id, JsonConvert.SerializeObject(userinfo), _cacheOptionsSliding);
            }

            return userinfo;
        }

        public async Task<Address> GetAddressItem(int id)
        {
            Address address;
            string cachedAddress = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
            if (!string.IsNullOrEmpty(cachedAddress))
            {
                address = JsonConvert.DeserializeObject<Address>(cachedAddress);
            }
            else
            {
                address = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonConvert.SerializeObject(address), _cacheOptionsSliding);

            }

            return address;
        }

        public async Task<Address> SetAddressItem(int id)
        {
            Address addressItem = await _context.AddressDb.AsNoTracking().SingleOrDefaultAsync(a => a.AddressId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "address" + id, JsonConvert.SerializeObject(addressItem), _cacheOptionsSliding);
            
            return addressItem;
        }

        public async Task<Address> AddAddressItem(Address addressItem)
        {
            await _context.AddressDb.AddAsync(addressItem);
            await _context.SaveChangesAsync();

            await SetAddressItem(addressItem.AddressId);

            return addressItem;
        }

        public async Task<Address> UpdateAddressItem(Address addressItem)
        {
            _context.AddressDb.Update(addressItem);
            await _context.SaveChangesAsync();

            await SetAddressItem(addressItem.AddressId);

            return addressItem;
        }

        public async Task RemoveAddressItem(int id)
        {
            Address addressItem = await _context.AddressDb.SingleOrDefaultAsync(a => a.AddressId == id);
            _context.AddressDb.Remove(addressItem);
            await _context.SaveChangesAsync();

            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "address" + id);
        }

        public async Task<CalendarItem> GetCalendarItem(int id)
        {
            CalendarItem calendarItem;
            string cachedCalendarItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);
            if (!string.IsNullOrEmpty(cachedCalendarItem))
            {
                calendarItem = JsonConvert.DeserializeObject<CalendarItem>(cachedCalendarItem);
            }
            else
            {
                calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);
            }

            return calendarItem;
        }

        public async Task<CalendarItem> SetCalendarItem(int id)
        {
            CalendarItem calendarItem = await _context.CalendarDb.AsNoTracking().SingleOrDefaultAsync(l => l.EventId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id, JsonConvert.SerializeObject(calendarItem), _cacheOptionsSliding);

            List<CalendarItem> calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == calendarItem.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + calendarItem.ProgenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            return calendarItem;
        }

        public async Task RemoveCalendarItem(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "calendaritem" + id);

            List<CalendarItem> calendarList = _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToList();
            _cache.SetString(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
        }

        public async Task<List<CalendarItem>> GetCalendarList(int progenyId)
        {
            List<CalendarItem> calendarList;
            string cachedCalendar = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId);
            if (!string.IsNullOrEmpty(cachedCalendar))
            {
                calendarList = JsonConvert.DeserializeObject<List<CalendarItem>>(cachedCalendar);
            }
            else
            {
                calendarList = await _context.CalendarDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "calendarlist" + progenyId, JsonConvert.SerializeObject(calendarList), _cacheOptionsSliding);
            }

            return calendarList;
        }

        public async Task<Contact> GetContact(int id)
        {
            Contact contact;
            string cachedContact = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);
            if (!string.IsNullOrEmpty(cachedContact))
            {
                contact = JsonConvert.DeserializeObject<Contact>(cachedContact);
            }
            else
            {
                contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);
            }

            return contact;
        }

        public async Task<Contact> SetContact(int id)
        {
            Contact contact = await _context.ContactsDb.AsNoTracking().SingleOrDefaultAsync(c => c.ContactId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contact" + id, JsonConvert.SerializeObject(contact), _cacheOptionsSliding);

            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == contact.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + contact.ProgenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);

            return contact;
        }

        public async Task RemoveContact(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "contact" + id);

            List<Contact> contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
        }

        public async Task<List<Contact>> GetContactsList(int progenyId)
        {
            List<Contact> contactsList;
            string cachedContactsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedContactsList))
            {
                contactsList = JsonConvert.DeserializeObject<List<Contact>>(cachedContactsList);
            }
            else
            {
                contactsList = await _context.ContactsDb.AsNoTracking().Where(c => c.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "contactslist" + progenyId, JsonConvert.SerializeObject(contactsList), _cacheOptionsSliding);
            }

            return contactsList;
        }

        public async Task<Friend> GetFriend(int id)
        {
            Friend friend;
            string cachedFriend = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id);
            if (!string.IsNullOrEmpty(cachedFriend))
            {
                friend = JsonConvert.DeserializeObject<Friend>(cachedFriend);
            }
            else
            {
                friend = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);
            }

            return friend;
        }

        public async Task<Friend> SetFriend(int id)
        {
            Friend friend = await _context.FriendsDb.AsNoTracking().SingleOrDefaultAsync(f => f.FriendId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friend" + id, JsonConvert.SerializeObject(friend), _cacheOptionsSliding);

            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == friend.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + friend.ProgenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);

            return friend;
        }

        public async Task RemoveFriend(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "friend" + id);

            List<Friend> friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId)
        {
            List<Friend> friendsList;
            string cachedFriendsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedFriendsList))
            {
                friendsList = JsonConvert.DeserializeObject<List<Friend>>(cachedFriendsList);
            }
            else
            {
                friendsList = await _context.FriendsDb.AsNoTracking().Where(f => f.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "friendslist" + progenyId, JsonConvert.SerializeObject(friendsList), _cacheOptionsSliding);
            }

            return friendsList;
        }

        public async Task<Location> GetLocation(int id)
        {
            Location location;
            string cachedLocation = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id);
            if (!string.IsNullOrEmpty(cachedLocation))
            {
                location = JsonConvert.DeserializeObject<Location>(cachedLocation);
            }
            else
            {
                location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
                _cache.SetString(Constants.AppName + Constants.ApiVersion + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);
            }

            return location;
        }

        public async Task<Location> SetLocation(int id)
        {
            Location location = await _context.LocationsDb.AsNoTracking().SingleOrDefaultAsync(l => l.LocationId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "location" + id, JsonConvert.SerializeObject(location), _cacheOptionsSliding);

            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == location.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + location.ProgenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);

            return location;
        }
        
        public async Task RemoveLocation(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "location" + id);

            List<Location> locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
        }

        public async Task<List<Location>> GetLocationsList(int progenyId)
        {
            List<Location> locationsList;
            string cachedLocationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedLocationsList))
            {
                locationsList = JsonConvert.DeserializeObject<List<Location>>(cachedLocationsList);
            }
            else
            {
                locationsList = await _context.LocationsDb.AsNoTracking().Where(l => l.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "locationslist" + progenyId, JsonConvert.SerializeObject(locationsList), _cacheOptionsSliding);
            }

            return locationsList;
        }

        public async Task<TimeLineItem> GetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + id);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(t => t.TimeLineId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            }

            return timeLineItem;
        }

        public async Task<TimeLineItem> SetTimeLineItem(int id)
        {
            TimeLineItem timeLineItem = await _context.TimeLineDb.AsNoTracking().SingleOrDefaultAsync(t => t.TimeLineId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + id, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + timeLineItem.ItemId + "type" + timeLineItem.ItemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == timeLineItem.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + timeLineItem.ProgenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
            
            return timeLineItem;
        }

        public async Task RemoveTimeLineItem(int timeLineItemId, int timeLineType, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "timelineitem" + timeLineItemId);
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + timeLineItemId + "type" + timeLineType);
            List<TimeLineItem> timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
        }

        public async Task<TimeLineItem> GetTimeLineItemByItemId(string itemId, int itemType)
        {
            TimeLineItem timeLineItem;
            string cachedTimeLineItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + itemId + itemType);
            if (!string.IsNullOrEmpty(cachedTimeLineItem))
            {
                timeLineItem = JsonConvert.DeserializeObject<TimeLineItem>(cachedTimeLineItem);
            }
            else
            {
                timeLineItem = await _context.TimeLineDb.SingleOrDefaultAsync(t => t.ItemId == itemId && t.ItemType == itemType);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelineitembyid" + itemId + "type" + itemType, JsonConvert.SerializeObject(timeLineItem), _cacheOptionsSliding);
            }

            return timeLineItem;
        }

        public async Task<List<TimeLineItem>> GetTimeLineList(int progenyId)
        {
            List<TimeLineItem> timeLineList;
            string cachedTimeLineList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId);
            if (!string.IsNullOrEmpty(cachedTimeLineList))
            {
                timeLineList = JsonConvert.DeserializeObject<List<TimeLineItem>>(cachedTimeLineList);
            }
            else
            {
                timeLineList = await _context.TimeLineDb.AsNoTracking().Where(t => t.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "timelinelist" + progenyId, JsonConvert.SerializeObject(timeLineList), _cacheOptionsSliding);
            }

            return timeLineList;
        }

        public async Task<Measurement> GetMeasurement(int id)
        {
            Measurement measurement;
            string cachedMeasurement = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);
            if (!string.IsNullOrEmpty(cachedMeasurement))
            {
                measurement = JsonConvert.DeserializeObject<Measurement>(cachedMeasurement);
            }
            else
            {
                measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);
            }

            return measurement;
        }

        public async Task<Measurement> SetMeasurement(int id)
        {
            Measurement measurement = await _context.MeasurementsDb.AsNoTracking().SingleOrDefaultAsync(m => m.MeasurementId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id, JsonConvert.SerializeObject(measurement), _cacheOptionsSliding);

            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == measurement.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + measurement.ProgenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);

            return measurement;
        }

        public async Task RemoveMeasurement(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "measurement" + id);

            List<Measurement> measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
        }

        public async Task<List<Measurement>> GetMeasurementsList(int progenyId)
        {
            List<Measurement> measurementsList;
            string cachedMeasurementsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedMeasurementsList))
            {
                measurementsList = JsonConvert.DeserializeObject<List<Measurement>>(cachedMeasurementsList);
            }
            else
            {
                measurementsList = await _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "measurementslist" + progenyId, JsonConvert.SerializeObject(measurementsList), _cacheOptionsSliding);
            }

            return measurementsList;
        }

        public async Task<Note> GetNote(int id)
        {
            Note note;
            string cachedNote = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id);
            if (!string.IsNullOrEmpty(cachedNote))
            {
                note = JsonConvert.DeserializeObject<Note>(cachedNote);
            }
            else
            {
                note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);
            }

            return note;
        }

        public async Task<Note> SetNote(int id)
        {
            Note note = await _context.NotesDb.AsNoTracking().SingleOrDefaultAsync(n => n.NoteId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "note" + id, JsonConvert.SerializeObject(note), _cacheOptionsSliding);

            List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(c => c.ProgenyId == note.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + note.ProgenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);

            return note;
        }

        public async Task RemoveNote(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "note" + id);

            List<Note> notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
        }

        public async Task<List<Note>> GetNotesList(int progenyId)
        {
            List<Note> notesList;
            string cachedNotesList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedNotesList))
            {
                notesList = JsonConvert.DeserializeObject<List<Note>>(cachedNotesList);
            }
            else
            {
                notesList = await _context.NotesDb.AsNoTracking().Where(n => n.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "noteslist" + progenyId, JsonConvert.SerializeObject(notesList), _cacheOptionsSliding);
            }

            return notesList;
        }

        public async Task<Skill> GetSkill(int id)
        {
            Skill skill;
            string cachedSkill = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);
            if (!string.IsNullOrEmpty(cachedSkill))
            {
                skill = JsonConvert.DeserializeObject<Skill>(cachedSkill);
            }
            else
            {
                skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);
            }

            return skill;
        }

        public async Task<Skill> SetSkill(int id)
        {
            Skill skill = await _context.SkillsDb.AsNoTracking().SingleOrDefaultAsync(s => s.SkillId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skill" + id, JsonConvert.SerializeObject(skill), _cacheOptionsSliding);

            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == skill.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + skill.ProgenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);

            return skill;
        }

        public async Task RemoveSkill(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "skill" + id);

            List<Skill> skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
        }

        public async Task<List<Skill>> GetSkillsList(int progenyId)
        {
            List<Skill> skillsList;
            string cachedSkillsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSkillsList))
            {
                skillsList = JsonConvert.DeserializeObject<List<Skill>>(cachedSkillsList);
            }
            else
            {
                skillsList = await _context.SkillsDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "skillslist" + progenyId, JsonConvert.SerializeObject(skillsList), _cacheOptionsSliding);
            }

            return skillsList;
        }

        public async Task<Sleep> GetSleep(int id)
        {
            Sleep sleep;
            string cachedSleep = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);
            if (!string.IsNullOrEmpty(cachedSleep))
            {
                sleep = JsonConvert.DeserializeObject<Sleep>(cachedSleep);
            }
            else
            {
                sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);
            }

            return sleep;
        }

        public async Task<Sleep> SetSleep(int id)
        {
            Sleep sleep = await _context.SleepDb.AsNoTracking().SingleOrDefaultAsync(s => s.SleepId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id, JsonConvert.SerializeObject(sleep), _cacheOptionsSliding);

            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == sleep.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + sleep.ProgenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);

            return sleep;
        }

        public async Task RemoveSleep(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "sleep" + id);

            List<Sleep> sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
        }

        public async Task<List<Sleep>> GetSleepList(int progenyId)
        {
            List<Sleep> sleepList;
            string cachedSleepList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId);
            if (!string.IsNullOrEmpty(cachedSleepList))
            {
                sleepList = JsonConvert.DeserializeObject<List<Sleep>>(cachedSleepList);
            }
            else
            {
                sleepList = await _context.SleepDb.AsNoTracking().Where(s => s.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "sleeplist" + progenyId, JsonConvert.SerializeObject(sleepList), _cacheOptionsSliding);
            }

            return sleepList;
        }

        public async Task<Vaccination> GetVaccination(int id)
        {
            Vaccination vaccination;
            string cachedVaccination = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);
            if (!string.IsNullOrEmpty(cachedVaccination))
            {
                vaccination = JsonConvert.DeserializeObject<Vaccination>(cachedVaccination);
            }
            else
            {
                vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);
            }

            return vaccination;
        }

        public async Task<Vaccination> SetVaccination(int id)
        {
            Vaccination vaccination = await _context.VaccinationsDb.AsNoTracking().SingleOrDefaultAsync(v => v.VaccinationId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id, JsonConvert.SerializeObject(vaccination), _cacheOptionsSliding);

            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == vaccination.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + vaccination.ProgenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);

            return vaccination;
        }

        public async Task RemoveVaccination(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vaccination" + id);

            List<Vaccination> vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
        }

        public async Task<List<Vaccination>> GetVaccinationsList(int progenyId)
        {
            List<Vaccination> vaccinationsList;
            string cachedVaccinationsList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVaccinationsList))
            {
                vaccinationsList = JsonConvert.DeserializeObject<List<Vaccination>>(cachedVaccinationsList);
            }
            else
            {
                vaccinationsList = await _context.VaccinationsDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vaccinationslist" + progenyId, JsonConvert.SerializeObject(vaccinationsList), _cacheOptionsSliding);
            }

            return vaccinationsList;
        }

        public async Task<VocabularyItem> GetVocabularyItem(int id)
        {
            VocabularyItem vocabularyItem;
            string cachedVocabularyItem = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);
            if (!string.IsNullOrEmpty(cachedVocabularyItem))
            {
                vocabularyItem = JsonConvert.DeserializeObject<VocabularyItem>(cachedVocabularyItem);
            }
            else
            {
                vocabularyItem = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(v => v.WordId == id);
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id, JsonConvert.SerializeObject(vocabularyItem), _cacheOptionsSliding);
            }

            return vocabularyItem;
        }

        public async Task<VocabularyItem> SetVocabularyItem(int id)
        {
            VocabularyItem word = await _context.VocabularyDb.AsNoTracking().SingleOrDefaultAsync(w => w.WordId == id);
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id, JsonConvert.SerializeObject(word), _cacheOptions);

            List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == word.ProgenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + word.ProgenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);

            return word;
        }

        public async Task RemoveVocabularyItem(int id, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "vocabularyitem" + id);

            List<VocabularyItem> wordList = await _context.VocabularyDb.AsNoTracking().Where(w => w.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId, JsonConvert.SerializeObject(wordList), _cacheOptionsSliding);
        }

        public async Task<List<VocabularyItem>> GetVocabularyList(int progenyId)
        {
            List<VocabularyItem> vocabularyList;
            string cachedVocabularyList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId);
            if (!string.IsNullOrEmpty(cachedVocabularyList))
            {
                vocabularyList = JsonConvert.DeserializeObject<List<VocabularyItem>>(cachedVocabularyList);
            }
            else
            {
                vocabularyList = await _context.VocabularyDb.AsNoTracking().Where(v => v.ProgenyId == progenyId).ToListAsync();
                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "vocabularylist" + progenyId, JsonConvert.SerializeObject(vocabularyList), _cacheOptionsSliding);
            }

            return vocabularyList;
        }

        public async Task UpdateProgenyAdmins(Progeny progeny)
        {
            Progeny oldProgeny = await _context.ProgenyDb.SingleOrDefaultAsync(p => p.Id == progeny.Id);

            if (oldProgeny != null)
            {
                oldProgeny.Admins = progeny.Admins;
                _context.ProgenyDb.Update(oldProgeny);
                await _context.SaveChangesAsync();

                await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "progeny" + progeny.Id, JsonConvert.SerializeObject(oldProgeny), _cacheOptionsSliding);
            }
        }
    }
}
