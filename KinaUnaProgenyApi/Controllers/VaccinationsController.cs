using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for Vaccinations.
    /// </summary>
    /// <param name="azureNotifications"></param>
    /// <param name="userInfoService"></param>
    /// <param name="timelineService"></param>
    /// <param name="vaccinationService"></param>
    /// <param name="progenyService"></param>
    /// <param name="webNotificationsService"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class VaccinationsController(
        IAzureNotifications azureNotifications,
        IUserInfoService userInfoService,
        ITimelineService timelineService,
        IVaccinationService vaccinationService,
        IProgenyService progenyService,
        IWebNotificationsService webNotificationsService)
        : ControllerBase
    {
        /// <summary>
        /// Get all vaccinations for a specific Progeny that a user with a given access level is allowed to see.
        /// </summary>
        /// <param name="id">The ProgenyId of the Progeny to get vaccinations for.</param>
        /// <returns>List of Vaccination objects.</returns>
        // GET api/vaccinations/progeny/[id]
        [HttpGet]
        [Route("[action]/{id:int}")]
        public async Task<IActionResult> Progeny(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            List<Vaccination> vaccinationsList = await vaccinationService.GetVaccinationsList(id, currentUserInfo);
            
            if (vaccinationsList.Count != 0)
            {
                return Ok(vaccinationsList);
            }

            return NotFound();

        }

        /// <summary>
        /// Get a specific vaccination item with a given VaccinationId.
        /// User must have appropriate access level.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination to get.</param>
        /// <returns>Vaccination object.</returns>
        // GET api/vaccinations/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVaccinationItem(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Vaccination vaccination = await vaccinationService.GetVaccination(id, currentUserInfo);
            if (vaccination == null)
            {
                return NotFound();
            }

            return Ok(vaccination);
        }

        /// <summary>
        /// Add a new vaccination entity to the database.
        /// Only users with appropriate access level for the Progeny can add vaccinations.
        /// Adds a corresponding TimeLineItem and also sends notifications to users with access to the Vaccination entity.
        /// </summary>
        /// <param name="value">The Vaccination to add.</param>
        /// <returns>The added Vaccination.</returns>
        // POST api/vaccinations
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Vaccination value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Progeny progeny = await progenyService.GetProgeny(value.ProgenyId, currentUserInfo);
            
            value.Author = User.GetUserId();
            value.CreatedBy = User.GetUserId();
            value.ModifiedBy = User.GetUserId();

            Vaccination vaccinationItem = await vaccinationService.AddVaccination(value, currentUserInfo);
            if (vaccinationItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = new();
            timeLineItem.CopyVaccinationPropertiesForAdd(vaccinationItem);

            _ = await timelineService.AddTimeLineItem(timeLineItem, currentUserInfo);

            string notificationTitle = "Vaccination added for " + progeny.NickName;
            string notificationMessage = currentUserInfo.FullName() + " added a new vaccination for " + progeny.NickName;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, currentUserInfo.ProfilePicture);
            await webNotificationsService.SendVaccinationNotification(vaccinationItem, currentUserInfo, notificationTitle);

            return Ok(vaccinationItem);
        }

        /// <summary>
        /// Update a vaccination entity in the database.
        /// Only users with appropriate access level for the Progeny can update vaccinations.
        /// Also updates the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination to update.</param>
        /// <param name="value">Vaccination object with the updated properties.</param>
        /// <returns>The updated Vaccination object.</returns>
        // PUT api/vaccinations/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Put(int id, [FromBody] Vaccination value)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            
            Vaccination vaccinationItem = await vaccinationService.GetVaccination(id, currentUserInfo);
            if (vaccinationItem == null)
            {
                return NotFound();
            }

            value.ModifiedBy = User.GetUserId();

            vaccinationItem = await vaccinationService.UpdateVaccination(value, currentUserInfo);
            if (vaccinationItem == null)
            {
                return Unauthorized();
            }

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(vaccinationItem.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, currentUserInfo);
            if (timeLineItem == null) return Ok(vaccinationItem);

            timeLineItem.CopyVaccinationPropertiesForUpdate(vaccinationItem);
            _ = await timelineService.UpdateTimeLineItem(timeLineItem, currentUserInfo);
            
            return Ok(vaccinationItem);
        }

        /// <summary>
        /// Delete a vaccination entity from the database.
        /// Also deletes the corresponding TimeLineItem.
        /// </summary>
        /// <param name="id">The VaccinationId of the Vaccination entity to delete.</param>
        /// <returns>NoContentResult.</returns>
        // DELETE api/vaccinations/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByUserId(User.GetUserId());
            Vaccination vaccinationItem = await vaccinationService.GetVaccination(id, currentUserInfo);
            if (vaccinationItem == null) return NotFound();

            Progeny progeny = await progenyService.GetProgeny(vaccinationItem.ProgenyId, currentUserInfo);
           
            vaccinationItem.ModifiedBy = User.GetUserId();

            _ = await vaccinationService.DeleteVaccination(vaccinationItem, currentUserInfo);

            TimeLineItem timeLineItem = await timelineService.GetTimeLineItemByItemId(vaccinationItem.VaccinationId.ToString(), (int)KinaUnaTypes.TimeLineType.Vaccination, currentUserInfo);
            if (timeLineItem != null)
            {
                _ = await timelineService.DeleteTimeLineItem(timeLineItem, currentUserInfo);
            }

            if (timeLineItem == null) return NoContent();

            string notificationTitle = "Vaccination deleted for " + progeny.NickName;
            string notificationMessage = currentUserInfo.FullName() + " deleted a vaccination for " + progeny.NickName + ". Vaccination: " + vaccinationItem.VaccinationName;

            vaccinationItem.AccessLevel = timeLineItem.AccessLevel = 0;

            await azureNotifications.ProgenyUpdateNotification(notificationTitle, notificationMessage, timeLineItem, currentUserInfo.ProfilePicture);
            await webNotificationsService.SendVaccinationNotification(vaccinationItem, currentUserInfo, notificationTitle);

            return NoContent();

        }
    }
}
