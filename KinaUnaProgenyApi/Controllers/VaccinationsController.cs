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
    public class VaccinationsController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        IUserAccessService userAccessService,
        ITimelineService timelineService,
        IVaccinationService vaccinationService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        // GET api/vaccinations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id, [FromQuery] int accessLevel = 5)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(id, userEmail);
            if (userAccess == null && id != Constants.DefaultChildId) return Unauthorized();

            List<Vaccination> vaccinationsList = await vaccinationService.GetVaccinationsList(id);
            vaccinationsList = vaccinationsList.Where(v => v.AccessLevel >= accessLevel).ToList();
            if (vaccinationsList.Count != 0)
            {
                return Ok(vaccinationsList);
            }

            return NotFound();

        }

        // GET api/vaccinations/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVaccinationItem(int id)
        {
            Vaccination result = await vaccinationService.GetVaccination(id);

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(result.ProgenyId, userEmail);
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
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId);
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

            Vaccination vaccinationItem = await vaccinationService.AddVaccination(value);

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVaccinationPropertiesForAdd(vaccinationItem);

            _ = await timelineService.AddTimeLineItem(timeLineItem);

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            string notificationTitle = "Vaccination added for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " added a new vaccination for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendVaccinationNotification(vaccinationItem, userInfo, notificationTitle);

            return Ok(vaccinationItem);
        }

        // PUT api/vaccinations/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vaccination value)
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

            Vaccination vaccinationItem = await vaccinationService.GetVaccination(id);
            if (vaccinationItem == null)
            {
                return NotFound();
            }

            vaccinationItem = await vaccinationService.UpdateVaccination(value);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(vaccinationItem.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination);
            if (timeLineItem == null) return Ok(vaccinationItem);

            timeLineItem.CopyVaccinationPropertiesForUpdate(vaccinationItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem);

            //UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            //string notificationTitle = "Vaccination edited for " + progeny.NickName;
            //string notificationMessage = userInfo.FullName() + " edited a vaccination for " + progeny.NickName;

            //await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            //await webNotificationsService.SendVaccinationNotification(vaccinationItem, userInfo, notificationTitle);
            
            return Ok(vaccinationItem);
        }

        // DELETE api/vaccinations/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            Vaccination vaccinationItem = await vaccinationService.GetVaccination(id);
            if (vaccinationItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(vaccinationItem.ProgenyId);
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

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(vaccinationItem.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem);
            }

            _ = await vaccinationService.DeleteVaccination(vaccinationItem);


            if (timeLineItem == null) return NoContent();

            UserInfo userInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            string notificationTitle = "Vaccination deleted for " + progeny.NickName;
            string notificationMessage = userInfo.FullName() + " deleted a vaccination for " + progeny.NickName + ". Vaccination: " + vaccinationItem.VaccinationName;

            vaccinationItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, userInfo.ProfilePicture);
            await webNotificationsService.SendVaccinationNotification(vaccinationItem, userInfo, notificationTitle);

            return NoContent();

        }

        [HttpGet("[action]/{id:int}")]
        public async Task<IActionResult> GetVaccinationMobile(int id)
        {
            Vaccination vaccination = await vaccinationService.GetVaccination(id);

            if (vaccination == null) return NotFound();

            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserAccess userAccess = await userAccessService.GetProgenyUserAccessForUser(vaccination.ProgenyId, userEmail);

            if (userAccess != null || vaccination.ProgenyId == Constants.DefaultChildId)
            {
                return Ok(vaccination);
            }

            return Unauthorized();

        }
    }
}
