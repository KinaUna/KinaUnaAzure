using KinaUna.Data.Extensions;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.Controllers
{
    /// <summary>
    /// API endpoints for registering devices for push notifications.
    /// Uses Azure Notification Hubs.
    /// </summary>
    /// <param name="azureNotifications"></param>
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController(IAzureNotifications azureNotifications) : ControllerBase
    {
        private readonly NotificationHubClient _hub = azureNotifications.Hub;

        public class DeviceRegistration
        {
            public string Platform { get; set; }
            public string Handle { get; set; }
            public string[] Tags { get; set; }
        }

        /// <summary>
        /// Adds a push device.
        /// Deletes existing registrations for the device first if they exist.
        /// </summary>
        /// <param name="handle">The device id</param>
        /// <returns>String with the registration id.</returns>
        // POST api/register
        // This creates a registration id
        [HttpPost]
        public async Task<string> Post([FromBody] string handle = null)
        {
            string newRegistrationId = null;

            // make sure there are no existing registrations for this push handle (used for iOS and Android)
            if (handle != null)
            {
                CollectionQueryResult<RegistrationDescription> registrations = (CollectionQueryResult<RegistrationDescription>)await _hub.GetRegistrationsByChannelAsync(handle, 100);

                foreach (RegistrationDescription registration in registrations)
                {
                    if (newRegistrationId == null)
                    {
                        newRegistrationId = registration.RegistrationId;
                    }
                    else
                    {
                        await _hub.DeleteRegistrationAsync(registration);
                    }
                }
            }

            return newRegistrationId ?? await _hub.CreateRegistrationIdAsync();
        }

        /// <summary>
        /// Updates a push device.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="deviceUpdate"></param>
        /// <returns></returns>
        // PUT api/register/5
        // This creates or updates a registration (with provided channelURI) at the specified id
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(string id, [FromBody] DeviceRegistration deviceUpdate)
        {
            RegistrationDescription registration;
            switch (deviceUpdate.Platform)
            {
                case "mpns":
                    registration = new MpnsRegistrationDescription(deviceUpdate.Handle);
                    break;
                case "wns":
                    registration = new WindowsRegistrationDescription(deviceUpdate.Handle);
                    break;
                case "apns":
                    registration = new AppleRegistrationDescription(deviceUpdate.Handle);
                    break;
                case "fcm":
                    registration = new FcmRegistrationDescription(deviceUpdate.Handle);
                    break;
                default:
                    return BadRequest();
            }

            registration.RegistrationId = id;

            string username = User.GetEmail();
            //var userId = User.GetUserId();
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized();
            }

            registration.Tags = new HashSet<string>(deviceUpdate.Tags);
            // registration.Tags.Add("email:" + username.ToUpper());
            // registration.Tags.Add("userid:" + userId);

            try
            {
                await _hub.CreateOrUpdateRegistrationAsync(registration);
            }
            catch (MessagingException e)
            {
                ReturnGoneIfHubResponseIsGone(e);
            }

            return Ok();
        }

        /// <summary>
        /// Deletes a push device.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // DELETE api/register/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _hub.DeleteRegistrationAsync(id);
            return Ok();
        }

        /// <summary>
        /// Helper method to check if the hub response is gone.
        /// </summary>
        /// <param name="e"></param>
        /// <exception cref="HttpRequestException"></exception>
        private static void ReturnGoneIfHubResponseIsGone(MessagingException e)
        {
            if (e.InnerException is not WebException webex) return;

            if (webex.Status == WebExceptionStatus.ProtocolError)
            {
                HttpWebResponse response = (HttpWebResponse)webex.Response;
                if (response != null && response.StatusCode == HttpStatusCode.Gone)
                    throw new HttpRequestException(HttpStatusCode.Gone.ToString());
            }
        }

        /// <summary>
        /// Gets the registration id for a device.
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        [HttpGet("[action]/{handle}")]
        public async Task<string> GetRegistrationId(string handle)
        {
            CollectionQueryResult<RegistrationDescription> regList = (CollectionQueryResult<RegistrationDescription>)await _hub.GetRegistrationsByChannelAsync(handle, 1);

            if (!regList.Any()) return "NO_DATA";

            foreach (RegistrationDescription regItem in regList)
            {
                if (regItem.PnsHandle == handle)
                {
                    return regItem.RegistrationId;
                }
            }

            return "NO_DATA";
        }
    }
}