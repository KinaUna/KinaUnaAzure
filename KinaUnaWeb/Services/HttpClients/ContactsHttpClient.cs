using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services.HttpClients
{
    public class ContactsHttpClient : IContactsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public ContactsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
        }

        public async Task<Contact> GetContact(int contactId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Contact contactItem = new();
            string contactsApiPath = "/api/Contacts/" + contactId;
            HttpResponseMessage contactResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (!contactResponse.IsSuccessStatusCode) return contactItem;

            string contactAsString = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            contactItem = JsonConvert.DeserializeObject<Contact>(contactAsString);

            return contactItem;
        }

        public async Task<Contact> AddContact(Contact contact)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            const string contactsApiPath = "/api/Contacts/";
            HttpResponseMessage contactResponse = await _httpClient.PostAsync(contactsApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json"));
            if (!contactResponse.IsSuccessStatusCode) return new Contact();

            string contactAsString = await contactResponse.Content.ReadAsStringAsync();
            contact = JsonConvert.DeserializeObject<Contact>(contactAsString);
            return contact;

        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string updateContactApiPath = "/api/Contacts/" + contact.ContactId;
            HttpResponseMessage updateContactResponseString = await _httpClient.PutAsync(updateContactApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json"));
            string returnString = await updateContactResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Contact>(returnString);
        }

        public async Task<bool> DeleteContact(int contactId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string contactApiPath = "/api/Contacts/" + contactId;
            await _httpClient.DeleteAsync(contactApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Contact>> GetContactsList(int progenyId, int accessLevel = 5, string tagFilter = "")
        {
            List<Contact> progenyContactsList = [];
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string contactsApiPath = "/api/Contacts/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage contactsResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (!contactsResponse.IsSuccessStatusCode) return progenyContactsList;

            string contactsAsString = await contactsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            progenyContactsList = JsonConvert.DeserializeObject<List<Contact>>(contactsAsString);

            if (!string.IsNullOrEmpty(tagFilter))
            {
                progenyContactsList = progenyContactsList.Where(c => c.Tags != null && c.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }

            return progenyContactsList;
        }
    }
}
