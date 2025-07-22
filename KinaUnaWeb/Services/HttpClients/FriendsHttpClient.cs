using IdentityModel.Client;
using KinaUna.Data.Models;
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

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods for interacting with the Friends API.
    /// </summary>
    public class FriendsHttpClient : IFriendsHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FriendsHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _apiTokenClient = apiTokenClient;
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
        /// Gets the Friend with the given FriendId.
        /// </summary>
        /// <param name="friendId">The FriendId of the Friend to get.</param>
        /// <returns>Friend object with the given FriendId. If not found, a new Friend object with FriendId = 0.</returns>
        public async Task<Friend> GetFriend(int friendId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            Friend friendItem = new();
            string friendsApiPath = "/api/Friends/" + friendId;
            HttpResponseMessage friendResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (!friendResponse.IsSuccessStatusCode) return friendItem;

            string friendAsString = await friendResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            friendItem = JsonConvert.DeserializeObject<Friend>(friendAsString);

            return friendItem;
        }

        /// <summary>
        /// Adds a new Friend item to the database.
        /// </summary>
        /// <param name="friend">The Friend object to add.</param>
        /// <returns>The Friend object that was added.</returns>
        public async Task<Friend> AddFriend(Friend friend)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string friendsApiPath = "/api/Friends/";
            HttpResponseMessage friendResponse = await _httpClient.PostAsync(friendsApiPath, new StringContent(JsonConvert.SerializeObject(friend), System.Text.Encoding.UTF8, "application/json"));
            if (!friendResponse.IsSuccessStatusCode) return new Friend();

            string friendAsString = await friendResponse.Content.ReadAsStringAsync();
            friend = JsonConvert.DeserializeObject<Friend>(friendAsString);
            return friend;

        }

        /// <summary>
        /// Updates a Friend item. The Friend item with the same FriendId will be updated.
        /// </summary>
        /// <param name="friend">The Friend object to update.</param>
        /// <returns>The updated Friend object.</returns>
        public async Task<Friend> UpdateFriend(Friend friend)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string updateFriendApiPath = "/api/Friends/" + friend.FriendId;
            HttpResponseMessage friendResponse = await _httpClient.PutAsync(updateFriendApiPath, new StringContent(JsonConvert.SerializeObject(friend), System.Text.Encoding.UTF8, "application/json"));
            if (!friendResponse.IsSuccessStatusCode) return new Friend();

            string friendAsString = await friendResponse.Content.ReadAsStringAsync();
            friend = JsonConvert.DeserializeObject<Friend>(friendAsString);
            return friend;

        }

        /// <summary>
        /// Removes a Friend item with a given FriendId.
        /// </summary>
        /// <param name="friendId">The FriendId of the Friend item to remove.</param>
        /// <returns>bool: True if the Friend was successfully removed.</returns>
        public async Task<bool> DeleteFriend(int friendId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string friendsApiPath = "/api/Friends/" + friendId;
            HttpResponseMessage friendResponse = await _httpClient.DeleteAsync(friendsApiPath).ConfigureAwait(false);
            return friendResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets the list of Friend objects for a given progeny that a user with a given access level has access to.
        /// </summary>
        /// <param name="progenyId">The Id of the progeny.</param>
        /// <param name="tagFilter">The tag to filter by. Only Friend items with the tagFilter string in the Tag property are included. Includes all friends if tagFilter is an empty string.</param>
        /// <returns>List of Friend objects.</returns>
        public async Task<List<Friend>> GetFriendsList(int progenyId, string tagFilter = "")
        {
            List<Friend> progenyFriendsList = [];
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string friendsApiPath = "/api/Friends/Progeny/" + progenyId;
            HttpResponseMessage friendsResponse = await _httpClient.GetAsync(friendsApiPath).ConfigureAwait(false);
            if (!friendsResponse.IsSuccessStatusCode) return progenyFriendsList;

            string friendsAsString = await friendsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            progenyFriendsList = JsonConvert.DeserializeObject<List<Friend>>(friendsAsString);

            if (!string.IsNullOrEmpty(tagFilter))
            {
                progenyFriendsList = progenyFriendsList.Where(c => c.Tags != null && c.Tags.Contains(tagFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }

            return progenyFriendsList;
        }
    }
}
