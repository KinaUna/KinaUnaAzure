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
        private readonly IDataService _dataService;
        public AutoSuggestsController(IDataService dataService)
        {
            _dataService = dataService;
        }

        [Route("[action]/{id}/{accessLevel}")]
        [HttpGet]
        public async Task<IActionResult> GetCategoryAutoSuggestList(int id, int accessLevel)
        {
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<string> autoSuggestList = new List<string>();
            List<Note> allNotes = await _dataService.GetNotesList(id);
            allNotes = allNotes.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Note noteItem in allNotes)
            {
                if (!string.IsNullOrEmpty(noteItem.Category))
                {
                    if (!autoSuggestList.Contains(noteItem.Category))
                    {
                        autoSuggestList.Add(noteItem.Category);
                    }
                }
            }

            List<Skill> allSkills = await _dataService.GetSkillsList(id);
            allSkills = allSkills.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Skill skillItem in allSkills)
            {
                if (!string.IsNullOrEmpty(skillItem.Category))
                {
                    if (!autoSuggestList.Contains(skillItem.Category))
                    {
                        autoSuggestList.Add(skillItem.Category);
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
            // Check if user should be allowed access.
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _dataService.GetProgenyUserAccessForUser(id, userEmail);

            if (userAccess == null && id != Constants.DefaultChildId)
            {
                return Unauthorized();
            }

            List<string> autoSuggestList = new List<string>();

            List<Friend> allFriends = await _dataService.GetFriendsList(id);
            allFriends = allFriends.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Friend friendItem in allFriends)
            {
                if (!string.IsNullOrEmpty(friendItem.Context))
                {
                    if (!autoSuggestList.Contains(friendItem.Context))
                    {
                        autoSuggestList.Add(friendItem.Context);
                    }
                }
            }

            List<CalendarItem> allCalendarItems = await _dataService.GetCalendarList(id);
            allCalendarItems = allCalendarItems.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (CalendarItem calendarItem in allCalendarItems)
            {
                if (!string.IsNullOrEmpty(calendarItem.Context))
                {
                    if (!autoSuggestList.Contains(calendarItem.Context))
                    {
                        autoSuggestList.Add(calendarItem.Context);
                    }
                }
            }

            List<Contact> allContacts = await _dataService.GetContactsList(id);
            allContacts = allContacts.Where(p => p.AccessLevel >= accessLevel).ToList();
            foreach (Contact contactItem in allContacts)
            {
                if (!string.IsNullOrEmpty(contactItem.Context))
                {
                    if (!autoSuggestList.Contains(contactItem.Context))
                    {
                        autoSuggestList.Add(contactItem.Context);
                    }
                }
            }
            autoSuggestList.Sort();
            return Ok(autoSuggestList);
        }

    }
}
