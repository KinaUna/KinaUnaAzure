using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaProgenyApi.Services;
using KinaUnaProgenyApi.Services.CalendarServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class CalendarRemindersController(ICalendarRemindersService calendarRemindersService, IUserInfoService userInfoService) : ControllerBase
    {
        [HttpGet("[action]/")]
        public async Task<IActionResult> GetAllCalendarReminders()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            if (!currentUserInfo.IsKinaUnaAdmin) return Unauthorized();
            return Ok(await calendarRemindersService.GetAllCalendarReminders());
        }

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetCalendarReminder(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            CustomResult<CalendarReminder> getCalendarReminderResult = await calendarRemindersService.GetCalendarReminder(id, currentUserInfo);
            
            return getCalendarReminderResult.ToActionResult();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> AddCalendarReminder([FromBody] CalendarReminder calendarReminder)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            CustomResult<CalendarReminder> addCalendarReminderResult = await calendarRemindersService.AddCalendarReminder(calendarReminder, currentUserInfo);

            return addCalendarReminderResult.ToActionResult();
        }

        [HttpPut("[action]")]
        public async Task<IActionResult> UpdateCalendarReminder([FromBody] CalendarReminder calendarReminder)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            CustomResult<CalendarReminder> updateCalendarReminderResult = await calendarRemindersService.UpdateCalendarReminder(calendarReminder, currentUserInfo);

            return updateCalendarReminderResult.ToActionResult();
        }

        [HttpDelete("[action]/{id:int}")]
        public async Task<IActionResult> DeleteCalendarReminder(int id)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            CustomResult<CalendarReminder> calendarReminder = await calendarRemindersService.GetCalendarReminder(id, currentUserInfo);
            if (calendarReminder.IsFailure) return calendarReminder.ToActionResult();

            CustomResult<CalendarReminder> deleteCalendarReminderResult = await calendarRemindersService.DeleteCalendarReminder(calendarReminder.Value, currentUserInfo);

            return deleteCalendarReminderResult.ToActionResult();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GetCalendarRemindersForUser([FromBody] CalendarRemindersForUserRequest request)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            CustomResult<List<CalendarReminder>> getCalendarRemindersForUserResult = await calendarRemindersService.GetCalendarRemindersForUser(request, currentUserInfo);

            return getCalendarRemindersForUserResult.ToActionResult();
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> GetUsersCalendarRemindersForEvent([FromBody] CalendarRemindersForUserRequest request)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            CustomResult<List<CalendarReminder>> getCalendarRemindersForEventResult = await calendarRemindersService.GetUsersCalendarRemindersForEvent(request.EventId, request.UserId, currentUserInfo);

            return getCalendarRemindersForEventResult.ToActionResult();
        }

    }
}
