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
    public class AutoSuggestsController : ControllerBase
    {
        private readonly IUserAccessService _userAccessService;
        private readonly ICalendarService _calendarService;
        private readonly IContactService _contactService;
        private readonly IFriendService _friendService;
        private readonly INoteService _noteService;
        private readonly ISkillService _skillService;
        private readonly IPicturesService _picturesService;
        private readonly IVideosService _videosService;
        private readonly ILocationService _locationService;

        public AutoSuggestsController(IUserAccessService userAccessService, ICalendarService calendarService, IContactService contactService, IFriendService friendService, INoteService noteService,
            ISkillService skillService, IPicturesService picturesService, IVideosService videosService, ILocationService locationService)
        {
            _userAccessService = userAccessService;
            _calendarService = calendarService;
            _contactService = contactService;
            _friendService = friendService;
            _noteService = noteService;
            _skillService = skillService;
            _picturesService = picturesService;
            _videosService = videosService;
            _locationService = locationService;
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<string> autoSuggestList = new();
            List<Note> allNotes = await _noteService.GetNotesList(id);
            allNotes = allNotes.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Note noteItem in allNotes)
            {
                if (!string.IsNullOrEmpty(noteItem.Category))
                {
                    List<string> tagsList = noteItem.Category.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Skill> allSkills = await _skillService.GetSkillsList(id);
            allSkills = allSkills.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Skill skillItem in allSkills)
            {
                if (!string.IsNullOrEmpty(skillItem.Category))
                {
                    List<string> tagsList = skillItem.Category.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetContextAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<string> autoSuggestList = new();

            List<Friend> allFriends = await _friendService.GetFriendsList(id);
            allFriends = allFriends.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Friend friendItem in allFriends)
            {
                if (!string.IsNullOrEmpty(friendItem.Context))
                {
                    List<string> tagsList = friendItem.Context.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<CalendarItem> allCalendarItems = await _calendarService.GetCalendarList(id);
            allCalendarItems = allCalendarItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (CalendarItem calendarItem in allCalendarItems)
            {
                if (!string.IsNullOrEmpty(calendarItem.Context))
                {
                    List<string> tagsList = calendarItem.Context.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Contact> allContacts = await _contactService.GetContactsList(id);
            allContacts = allContacts.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Contact contactItem in allContacts)
            {
                if (!string.IsNullOrEmpty(contactItem.Context))
                {
                    List<string> tagsList = contactItem.Context.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetLocationAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> allPictures = await _picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = new();
            foreach (Picture picture in allPictures)
            {
                if (!string.IsNullOrEmpty(picture.Location))
                {
                    List<string> tagsList = picture.Location.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Video> allVideos = await _videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (!string.IsNullOrEmpty(video.Location))
                {
                    List<string> tagsList = video.Location.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<CalendarItem> allCalendarItems = await _calendarService.GetCalendarList(id);
            allCalendarItems = allCalendarItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (CalendarItem calendarItem in allCalendarItems)
            {
                if (!string.IsNullOrEmpty(calendarItem.Location))
                {
                    List<string> tagsList = calendarItem.Location.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Location> allLocations = await _locationService.GetLocationsList(id);
            allLocations = allLocations.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Location locationItem in allLocations)
            {
                if (!string.IsNullOrEmpty(locationItem.Name))
                {
                    List<string> tagsList = locationItem.Name.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!string.IsNullOrEmpty(tagString) && !autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetTagsAutoSuggestList(int id, int accessLevel)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<Picture> allPictures = await _picturesService.GetPicturesList(id);
            allPictures = allPictures.Where(p => p.AccessLevel >= accessLevel).ToList();
            List<string> autoSuggestList = new();
            foreach (Picture picture in allPictures)
            {
                if (!string.IsNullOrEmpty(picture.Tags))
                {
                    List<string> tagsList = picture.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Video> allVideos = await _videosService.GetVideosList(id);
            allVideos = allVideos.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Video video in allVideos)
            {
                if (!string.IsNullOrEmpty(video.Tags))
                {
                    List<string> tagsList = video.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Location> allLocations = await _locationService.GetLocationsList(id);
            allLocations = allLocations.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Location location in allLocations)
            {
                if (!string.IsNullOrEmpty(location.Tags))
                {
                    List<string> tagsList = location.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Friend> allFriends = await _friendService.GetFriendsList(id);
            allFriends = allFriends.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Friend friend in allFriends)
            {
                if (!string.IsNullOrEmpty(friend.Tags))
                {
                    List<string> tagsList = friend.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        if (!autoSuggestList.Contains(tagString.Trim()))
                        {
                            autoSuggestList.Add(tagString.Trim());
                        }
                    }
                }
            }

            List<Contact> allContacts = await _contactService.GetContactsList(id);
            allContacts = allContacts.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Contact contact in allContacts)
            {
                if (!string.IsNullOrEmpty(contact.Tags))
                {
                    List<string> tagsList = contact.Tags.Split(',').ToList();
                    foreach (string tagString in tagsList)
                    {
                        string trimmedTagString = tagString.Trim();
                        if (!string.IsNullOrEmpty(trimmedTagString) && !autoSuggestList.Contains(trimmedTagString))
                        {
                            autoSuggestList.Add(trimmedTagString);
                        }
                    }
                }
            }

            autoSuggestList = autoSuggestList.Distinct().ToList();
            autoSuggestList.Sort();

            return Ok(autoSuggestList);
        }
    }
}
