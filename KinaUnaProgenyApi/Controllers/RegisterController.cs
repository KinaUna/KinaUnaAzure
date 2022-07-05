using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using System.Threading.Tasks;
using KinaUna.Data.Extensions;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(AuthenticationSchemes = "Bearer")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly NotificationHubClient _hub;

        public RegisterController(AzureNotifications azureNotifications)
        {
            _hub = azureNotifications.Hub;
        }

        public class DeviceRegistration
        {
            public string Platform { get; set; }
            public string Handle { get; set; }
            public string[] Tags { get; set; }
        }

        // POST api/register
        // This creates a registration id
        [HttpPost]
        public async Task<string> Post([FromBody] string handle = null)
        {
            string newRegistrationId = null;

            // make sure there are no existing registrations for this push handle (used for iOS and Android)
            if (handle != null)
            {
                var registrations = await _hub.GetRegistrationsByChannelAsync(handle, 100);

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

            if (newRegistrationId == null)
                newRegistrationId = await _hub.CreateRegistrationIdAsync();

            return newRegistrationId;
        }

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
             
            var username = User.GetEmail();
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

        // DELETE api/register/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _hub.DeleteRegistrationAsync(id);
            return Ok();
        }

        private static void ReturnGoneIfHubResponseIsGone(MessagingException e)
        {
            var webex = e.InnerException as WebException;
            if (webex != null)
            {
                if (webex.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = (HttpWebResponse)webex.Response;
                    if (response != null && response.StatusCode == HttpStatusCode.Gone)
                        throw new HttpRequestException(HttpStatusCode.Gone.ToString());
                }
            }
        }

        [HttpGet("[action]/{handle}")]
        public async Task<string> GetRegistrationId(string handle)
        {
            var regList = await _hub.GetRegistrationsByChannelAsync(handle, 1);

            if (regList.Any())
            {
                foreach (var regItem in regList)
                {
                    if (regItem.PnsHandle == handle)
                    {
                        return regItem.RegistrationId;
                    }
                }
            }

            return "NO_DATA";
        }
    }
}