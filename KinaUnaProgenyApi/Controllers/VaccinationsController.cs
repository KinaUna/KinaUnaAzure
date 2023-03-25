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
    public class VaccinationsController : ControllerBase
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IUserAccessService _userAccessService;
        private readonly ITimelineService _timelineService;
        private readonly IVaccinationService _vaccinationService;
        private readonly IProgenyService _progenyService;
        private readonly IAzureNotifications _azureNotifications;
        private readonly IWebNotificationsService _webNotificationsService;

        public VaccinationsController(IAzureNotifications azureNotifications, IUserInfoService userInfoService, IUserAccessService userAccessService,
            ITimelineService timelineService, IVaccinationService vaccinationService, IProgenyService progenyService, IWebNotificationsService webNotificationsService)
        {
            _azureNotifications = azureNotifications;
            _userInfoService = userInfoService;
            _userAccessService = userAccessService;
            _timelineService = timelineService;
            _vaccinationService = vaccinationService;
            _progenyService = progenyService;
            _webNotificationsService = webNotificationsService;
        }

        // GET api/vaccinations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                List<Vaccination> vaccinationsList = await _vaccinationService.GetVaccinationsList(id);
                vaccinationsList = vaccinationsList.Where(v => v.AccessLevel >= accessLevel).ToList();
                if (vaccinationsList.Any())
                {
                    return Ok(vaccinationsList);
                }

                return NotFound();
            }

            return Unauthorized();
        }

        // GET api/vaccinations/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVaccinationItem(int id)
        {
            Vaccination result = await _vaccinationService.GetVaccination(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
            if (userAccess != null || id == Constants.DefaultChildId)
            {
                return Ok(result);
            }

            return Unauthorized();
        }

        // POST api/vaccinations
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Vaccination value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            if (progeny != null)
            {
                // Check if user is allowed to add vaccinations for this child.

                if (!progeny.IsInAdminList(userEmail))
                {
                    return Unauthorized();
                }
            }
            else
            {
                return NotFound();
            }

            value.Author = User.GetUserId();

            Vaccination vaccinationItem = await _vaccinationService.AddVaccination(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVaccinationPropertiesForAdd(vaccinationItem);

            _ = await _timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Vaccination added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new vaccination for " + progeny.NickName;

            await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await _webNotificationsService.SendVaccinationNotification(vaccinationItem, userInfo, notificationTitle);

            return Ok(vaccinationItem);
        }

        // PUT api/vaccinations/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vaccination value)
        {
            Progeny progeny = await _progenyService.GetProgeny(value.ProgenyId);
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

            Vaccination vaccinationItem = await _vaccinationService.GetVaccination(id);
            if (vaccinationItem == null)
            {
                return NotFound();
            }

            vaccinationItem = await _vaccinationService.UpdateVaccination(value);

            TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(vaccinationItem.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination);
            if (timeLineItem != null)
            {
                timeLineItem.CopyVaccinationPropertiesForUpdate(vaccinationItem);
                _ = await _timelineService.UpdateTimeLineItem(timeLineItem);

                UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);
                string notificationTitle = "Vaccination edited for " + progeny.NickName;
                string notificationMessage = userInfo.FullName() + " edited a vaccination for " + progeny.NickName;

                await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                await _webNotificationsService.SendVaccinationNotification(vaccinationItem, userInfo, notificationTitle);
            }



            return Ok(vaccinationItem);
        }

        // DELETE api/vaccinations/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            Vaccination vaccinationItem = await _vaccinationService.GetVaccination(id);
            if (vaccinationItem != null)
            {
                Progeny progeny = await _progenyService.GetProgeny(vaccinationItem.ProgenyId);
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

                TimeLineItem timeLineItem = await _timelineService.GetTimeLineItemByItemId(vaccinationItem.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination);
                if (timeLineItem != null)
                {
                    _ = await _timelineService.DeleteTimeLineItem(timeLineItem);
                }

                _ = await _vaccinationService.DeleteVaccination(vaccinationItem);


                if (timeLineItem != null)
                {
                    UserInfo userInfo = await _userInfoService.GetUserInfoByEmail(userEmail);

                    string notificationTitle = "Vaccination deleted for " + progeny.NickName;
                    string notificationMessage = userInfo.FullName() + " deleted a vaccination for " + progeny.NickName + ". Vaccination: " + vaccinationItem.VaccinationName;

                    vaccinationItem.AccessLevel = timeLineItem.AccessLevel = 0;

                    await _azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
                    await _webNotificationsService.SendVaccinationNotification(vaccinationItem, userInfo, notificationTitle);
                }

                return NoContent();
            }

            return NotFound();
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> GetVaccinationMobile(int id)
        {
            Vaccination vaccination = await _vaccinationService.GetVaccination(id);

            if (vaccination != null)
            {
                string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
                UserAccess userAccess = await _userAccessService.GetProgenyUserAccessForUser(vaccination.ProgenyId, userEmail);

                if (userAccess != null || vaccination.ProgenyId == Constants.DefaultChildId)
                {
                    return Ok(vaccination);
                }

                return Unauthorized();
            }

            return NotFound();
        }
    }
}
