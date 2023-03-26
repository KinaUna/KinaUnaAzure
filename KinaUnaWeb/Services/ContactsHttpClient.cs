using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class ContactsHttpClient: IContactsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public ContactsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");
            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName, _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }

        public async Task<Contact> GetContact(int contactId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            Contact contactItem = new();
            string contactsApiPath = "/api/Contacts/" + contactId;
            HttpResponseMessage contactResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (contactResponse.IsSuccessStatusCode)
            {
                string contactAsString = await contactResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                contactItem = JsonConvert.DeserializeObject<Contact>(contactAsString);
            }

            return contactItem;
        }

        public async Task<Contact> AddContact(Contact contact)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string contactsApiPath = "/api/Contacts/";
            HttpResponseMessage contactResponse = await _httpClient.PostAsync(contactsApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json"));
            if (contactResponse.IsSuccessStatusCode)
            {
                string contactAsString = await contactResponse.Content.ReadAsStringAsync();
                contact = JsonConvert.DeserializeObject<Contact>(contactAsString);
                return contact;
            }

            return new Contact();
        }

        public async Task<Contact> UpdateContact(Contact contact)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateContactApiPath = "/api/Contacts/" + contact.ContactId;
            HttpResponseMessage updateContactResponseString = await _httpClient.PutAsync(updateContactApiPath, new StringContent(JsonConvert.SerializeObject(contact), System.Text.Encoding.UTF8, "application/json"));
            string returnString = await updateContactResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Contact>(returnString);
        }

        public async Task<bool> DeleteContact(int contactId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string contactApiPath = "/api/Contacts/" + contactId;
            await _httpClient.DeleteAsync(contactApiPath).ConfigureAwait(false);

            return true;
        }

        public async Task<List<Contact>> GetContactsList(int progenyId, int accessLevel = 5, string tagFilter = "")
        {
            List<Contact> progenyContactsList = new();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string contactsApiPath = "/api/Contacts/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage contactsResponse = await _httpClient.GetAsync(contactsApiPath).ConfigureAwait(false);
            if (contactsResponse.IsSuccessStatusCode)
            {
                string contactsAsString = await contactsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                progenyContactsList = JsonConvert.DeserializeObject<List<Contact>>(contactsAsString);

                if (!string.IsNullOrEmpty(tagFilter))
                {
                    progenyContactsList = progenyContactsList.Where(c => c.Tags != null && c.Tags.ToUpper().Contains(tagFilter.ToUpper())).ToList();
                }
            }

            return progenyContactsList;
        }
    }
}
