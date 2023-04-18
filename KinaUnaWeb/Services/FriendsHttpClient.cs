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

namespace KinaUnaWeb.Services
{
    public class FriendsHttpClient: IFriendsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public FriendsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);

        }
        

        public async Task<Friend> GetFriend(int friendId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            Friend friendItem = new();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
                        
            string friendsApiPath = "/api/Friends/" + friendId;
            HttpResponseMessage friendResponse = await _httpClient.DeleteAsync(friendsApiPath).ConfigureAwait(false);
            if (friendResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<List<Friend>> GetFriendsList(int progenyId, int accessLevel, string tagFilter = "")
        {
            List<Friend> progenyFriendsList = new();
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string friendsApiPath = "/api/Friends/Progeny/" + progenyId + "?accessLevel=" + accessLevel;
            HttpResponseMessage friendsResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (friendsResponse.IsSuccessStatusCode)
            {
                string friendsAsString = await friendsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                progenyFriendsList = JsonConvert.DeserializeObject<List<Friend>>(friendsAsString);

                if (!string.IsNullOrEmpty(tagFilter))
                {
                    progenyFriendsList = progenyFriendsList.Where(c => c.Tags != null && c.Tags.ToUpper().Contains(tagFilter.ToUpper())).ToList();
                }
            }

            return progenyFriendsList;
        }
    }
}
