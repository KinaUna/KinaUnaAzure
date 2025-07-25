using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Duende.IdentityModel.Client;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Contacts API.
    /// </summary>
    public class ContactsHttpClient : IContactsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ContactsHttpClient(HttpClient httpClient, IConfiguration configuration, ITokenService tokenService, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _tokenService = tokenService;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
            }
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        /// <summary>
        /// Gets the Contact with a given ContactId.
        /// </summary>
        /// <param name="contactId">The ContactId of the Contact to get.</param>
        /// <returns>Contact with the given ContactId. If not found, a new Contact with ContactId = 0.</returns>
        public async Task<Contact> GetContact(int contactId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            Contact contactItem = new();
            string contactsApiPath = "/api/Contacts/" + contactId;
            HttpResponseMessage contactResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (!contactResponse.IsSuccessStatusCode) return contactItem;

            string contactAsString = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            contactItem = JsonConvert.DeserializeObject<Contact>(contactAsString);

            return contactItem;
        }

        /// <summary>
        /// Adds a new Contact.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to add.</param>
        /// <returns>Contact: The Contact object that was added.</returns>
        public async Task<Contact> AddContact(Contact contact)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string contactsApiPath = "/api/Contacts/";
            HttpResponseMessage contactResponse = await _httpClient.PostAsync(contactsApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json"));
            if (!contactResponse.IsSuccessStatusCode) return new Contact();

            string contactAsString = await contactResponse.Content.ReadAsStringAsync();
            contact = JsonConvert.DeserializeObject<Contact>(contactAsString);
            return contact;

        }

        /// <summary>
        /// Updates a Contact. The Contact with the same ContactId will be updated.
        /// </summary>
        /// <param name="contact">Contact: The Contact object to update.</param>
        /// <returns>Contact: The updated Contact object.</returns>
        public async Task<Contact> UpdateContact(Contact contact)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string updateContactApiPath = "/api/Contacts/" + contact.ContactId;
            HttpResponseMessage updateContactResponseString = await _httpClient.PutAsync(updateContactApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json"));
            string returnString = await updateContactResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Contact>(returnString);
        }

        /// <summary>
        /// Removes the Contact with the given ContactId.
        /// </summary>
        /// <param name="contactId">int: The ContactId of the Contact object to remove.</param>
        /// <returns>bool: True if the Contact was successfully removed.</returns>
        public async Task<bool> DeleteContact(int contactId)
        {
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string contactApiPath = "/api/Contacts/" + contactId;
            await _httpClient.DeleteAsync(contactApiPath).ConfigureAwait(false);

            return true;
        }

        /// <summary>
        /// Gets the list of Contact objects for a progeny that a user has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="tagFilter">String to filter the result list by, only items with the tagFilter string in the Tags property are included. If empty string all items are included.</param>
        /// <returns>List of Contact objects.</returns>
        public async Task<List<Contact>> GetContactsList(int progenyId, string tagFilter = "")
        {
            List<Contact> progenyContactsList = [];
            string signedInUserId = _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? string.Empty;
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync(signedInUserId);
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            string contactsApiPath = "/api/Contacts/Progeny/" + progenyId;
            HttpResponseMessage contactsResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (!contactsResponse.IsSuccessStatusCode) return progenyContactsList;

            string contactsAsString = await contactsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            progenyContactsList = JsonConvert.DeserializeObject<List<Contact>>(contactsAsString);

            if (!string.IsNullOrEmpty(tagFilter))
            {
                progenyContactsList = [.. progenyContactsList.Where(c => c.Tags != null && c.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase))];
            }

            return progenyContactsList;
        }
    }
}
