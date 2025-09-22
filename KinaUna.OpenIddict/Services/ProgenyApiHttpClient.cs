using Duende.IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace KinaUna.OpenIddict.Services
{
    /// <summary>
    /// Service for handling languages, translations, and page texts.
    /// </summary>
    public class ProgenyApiHttpClient : IProgenyApiHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenService _tokenService;

        public ProgenyApiHttpClient(HttpClient httpClient, IConfiguration configuration, IHostEnvironment env, ITokenService tokenService)
        {
            string clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey) ?? throw new InvalidOperationException();
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Local") ?? throw new InvalidOperationException();
            }

            if (env.IsStaging())
            {
                clientUri = configuration.GetValue<string>(AuthConstants.ProgenyApiUrlKey + "Azure") ?? throw new InvalidOperationException();
            }

            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
            _tokenService = tokenService;
        }

        public async Task<UserInfo> GetUserInfoByUserId(string userId)
        {
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync();
            _httpClient.SetBearerToken(tokenInfo.AccessToken);

            const string userInfoApiPath = "api/UserInfo/ByUserIdPost/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userInfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userinfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo? userInfo = JsonConvert.DeserializeObject<UserInfo>(userinfoAsString);

            return userInfo ?? new UserInfo();
        }

        public async Task<bool> UpdateAccessListsWithNewUserEmail(string userId, string oldEmail, string newEmail)
        {
            UpdateUserEmailModel model = new()
            {
                UserId = userId,
                OldEmail = oldEmail,
                NewEmail = newEmail
            };

            const string updateAccesslistsWithNewEmailPath = "/api/Access/UpdateUsersEmail";
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync();
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage addResponse = await _httpClient.PostAsync(updateAccesslistsWithNewEmailPath, new StringContent(JsonConvert.SerializeObject(model), System.Text.Encoding.UTF8, "application/json"));

            if (addResponse.IsSuccessStatusCode)
            {
                return true;
            }

            string errorMessage = await addResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to update access lists with new email: {errorMessage}");
        }

        public async Task<List<UserInfo>> GetDeletedUserInfos()
        {
            const string deletedUserInfosPath = "/api/UserInfo/GetDeletedUserInfos";
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync();
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage addResponse = await _httpClient.GetAsync(deletedUserInfosPath);
            if (addResponse.IsSuccessStatusCode)
            {
                string responseContent = await addResponse.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<UserInfo>>(responseContent) ?? [];
            }

            return [];
        }

        public async Task<UserInfo?> AddUserInfoToDeletedUserInfos(UserInfo userInfo)
        {
            const string addPath = "/api/UserInfo/AddUserInfoToDeletedUserInfos";
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync();
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage addResponse = await _httpClient.PostAsync(addPath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (addResponse.IsSuccessStatusCode)
            {
                UserInfo? addedUserInfo = await addResponse.Content.ReadFromJsonAsync<UserInfo>();
                if (addedUserInfo != null) return addedUserInfo;
            }
            string errorMessage = await addResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to add user info to deleted user infos: {errorMessage}");
        }

        public async Task<UserInfo> UpdateDeletedUserInfo(UserInfo userInfo)
        {
            const string updatePath = "/api/UserInfo/UpdateDeletedUserInfo";
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync();
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage updateResponse = await _httpClient.PostAsync(updatePath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));
            if (updateResponse.IsSuccessStatusCode)
            {
                UserInfo? updatedUserInfo = await updateResponse.Content.ReadFromJsonAsync<UserInfo>();
                if (updatedUserInfo != null) return updatedUserInfo;
            }
            string errorMessage = await updateResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to update deleted user info: {errorMessage}");
        }

        public async Task<UserInfo?> RemoveUserInfoFromDeletedUserInfos(UserInfo userInfo)
        {
            const string removePath = "/api/UserInfo/RemoveUserInfoFromDeletedUserInfos";
            TokenInfo tokenInfo = await _tokenService.GetValidTokenAsync();
            _httpClient.SetBearerToken(tokenInfo.AccessToken);
            HttpResponseMessage removeResponse = await _httpClient.PostAsync(removePath, new StringContent(JsonConvert.SerializeObject(userInfo), System.Text.Encoding.UTF8, "application/json"));

            if (removeResponse.IsSuccessStatusCode)
            {
                UserInfo? removeUserInfoFromDeletedUserInfos = await removeResponse.Content.ReadFromJsonAsync<UserInfo>();
                if (removeUserInfoFromDeletedUserInfos != null) return removeUserInfoFromDeletedUserInfos;
            }

            string errorMessage = await removeResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to remove user info from deleted user infos: {errorMessage}");
        }
    }
}
