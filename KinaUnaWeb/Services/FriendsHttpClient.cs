using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class FriendsHttpClient: IFriendsHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;

        public FriendsHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            _env = env;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                clientUri = _configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer);
            }

            httpClient.BaseAddress = new Uri(clientUri);
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
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer);
            }

            string accessToken = await _apiTokenClient.GetApiToken(authenticationServerClientId, Constants.ProgenyApiName + " " + Constants.MediaApiName, _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }

        public async Task<Friend> GetFriend(int friendId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            Friend friendItem = new Friend();
            string friendsApiPath = "/api/Friends/" + friendId;
            HttpResponseMessage friendResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (friendResponse.IsSuccessStatusCode)
            {
                string friendAsString = await friendResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                friendItem = JsonConvert.DeserializeObject<Friend>(friendAsString);
            }

            return friendItem;
        }

        public async Task<Friend> AddFriend(Friend friend)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string friendsApiPath = "/api/Friends/";
            HttpResponseMessage friendResponse = await _httpClient.PostAsync(friendsApiPath, new StringContent(JsonConvert.SerializeObject(friend), System.Text.Encoding.UTF8, "application/json"));
            if (friendResponse.IsSuccessStatusCode)
            {
                string friendAsString = await friendResponse.Content.ReadAsStringAsync();
                friend = JsonConvert.DeserializeObject<Friend>(friendAsString);
                return friend;
            }

            return new Friend();
        }

        public async Task<Friend> UpdateFriend(Friend friend)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateFriendApiPath = "/api/Friends/" + friend.FriendId;
            HttpResponseMessage friendResponse = await _httpClient.PutAsync(updateFriendApiPath, new StringContent(JsonConvert.SerializeObject(friend), System.Text.Encoding.UTF8, "application/json"));
            if (friendResponse.IsSuccessStatusCode)
            {
                string friendAsString = await friendResponse.Content.ReadAsStringAsync();
                friend = JsonConvert.DeserializeObject<Friend>(friendAsString);
                return friend;
            }

            return new Friend();
        }

        public async Task<bool> DeleteFriend(int friendId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
                        
            string friendsApiPath = "/api/Friends/" + friendId;
            HttpResponseMessage friendResponse = await _httpClient.DeleteAsync(friendsApiPath).ConfigureAwait(false);
            if (friendResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel)
        {
            List<Friend> progenyFriendsList = new List<Friend>();
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string friendsApiPath = "/api/Friends/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage friendsResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (friendsResponse.IsSuccessStatusCode)
            {
                string friendsAsString = await friendsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyFriendsList = JsonConvert.DeserializeObject<List<Friend>>(friendsAsString);
            }

            return progenyFriendsList;
        }
    }
}
