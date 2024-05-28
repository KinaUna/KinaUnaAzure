using System.Net.Http.Headers;
using System.Security.Claims;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class ProgenyManager : IProgenyManager
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdentityParser<ApplicationUser> _userManager;
        private readonly ImageStore _imageStore;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IAuthHttpClient _authHttpClient;
        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, HttpClient httpClient, ApiTokenInMemoryClient apiTokenClient, IAuthHttpClient authHttpClient)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer") ?? throw new InvalidOperationException("ProgenyApiServer value missing in configuration.");
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
            _authHttpClient = authHttpClient;
        }

        private async Task<string> GetNewToken(bool apiTokenOnly = false)
        {
            if (!apiTokenOnly)
            {
                HttpContext? currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string? contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        return contextAccessToken;
                    }
                }
            }

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration.");

            string accessToken = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration."));
            return accessToken;
        }


        public async Task<UserInfo?> GetInfo(string userEmail)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string userInfoApiPath = "/api/UserInfo/ByEmail/" + userEmail;
            UserInfo? userInfo = new();
            try
            {
                string userInfoResponseString = await _httpClient.GetStringAsync(userInfoApiPath);
                userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseString);
                if (userInfo != null && !userInfo.IsKinaUnaUser)
                {
                    if (userInfo.UserEmail != "Unknown")
                    {
                        userInfo.IsKinaUnaUser = true;
                        _ = await UpdateUserInfo(userInfo);
                    }
                }
            }
            catch (Exception e)
            {
                if (userInfo != null)
                {
                    userInfo.UserId = "401";
                    userInfo.UserName = e.Message;
                    userInfo.UserEmail = Constants.DefaultUserEmail;
                    userInfo.CanUserAddItems = false;
                    userInfo.ViewChild = Constants.DefaultChildId;
                    return userInfo;
                }
            }

            if (userInfo != null && userInfo.UserEmail == "Unknown")
            {
                ApplicationUser applicationUser = _userManager.Parse(_httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal());
                if (!string.IsNullOrEmpty(applicationUser.Email))
                {
                    UserInfo newUserinfo = new()
                    {
                        UserEmail = applicationUser.Email,
                        ViewChild = 0,
                        UserId = applicationUser.Id,
                        FirstName = applicationUser.FirstName ?? "",
                        MiddleName = applicationUser.MiddleName ?? "",
                        LastName = applicationUser.LastName ?? "",
                        // Todo: ProfilePicture
                        Timezone = applicationUser.TimeZone,
                        UserName = applicationUser.UserName,
                        IsKinaUnaUser = true
                    };

                    if (string.IsNullOrEmpty(newUserinfo.UserName))
                    {
                        newUserinfo.UserName = newUserinfo.UserEmail;
                    }

                    const string newUserinfoApiPath = "/api/UserInfo/";
                    HttpResponseMessage newUserResponse = await _httpClient.PostAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(newUserinfo), System.Text.Encoding.UTF8, "application/json"));
                    if (newUserResponse.IsSuccessStatusCode)
                    {
                        string newUserResponseString = await newUserResponse.Content.ReadAsStringAsync();
                        userInfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
                    }
                }
            }

            if (userInfo == null || userInfo.ViewChild != 0) return userInfo;

            if (userInfo.ProgenyList.Count != 0)
            {
                await SetViewChild(userInfo.UserEmail, userInfo.ProgenyList[0].Id, userInfo.UserId);
            }
            else
            {
                userInfo.ViewChild = Constants.DefaultChildId;
            }
            return userInfo;

        }

        public string GetImageUrl(string pictureLink, string pictureContainer)
        {
            string returnString = pictureLink;
            if (!pictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase))
            {
                returnString = _imageStore.UriFor(pictureLink, pictureContainer);
            }
            return returnString;
        }

        private async Task<UserInfo?> UpdateUserInfo(UserInfo? userinfo)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            // Todo: ProfilePicture
            string newUserinfoApiPath = "/api/UserInfo/" + userinfo?.UserId;
            HttpResponseMessage userInfoResponse = await _httpClient.PutAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return new UserInfo();

            string userInfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo? updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(userInfoAsString);
            return updatedUserinfo;

        }

        public async Task<Progeny?> CurrentChildAsync(int progenyId, string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string progenyApiPath = "/api/Progeny/" + progenyId;
            string progenyResponseString = await _httpClient.GetStringAsync(progenyApiPath);
            Progeny? child = JsonConvert.DeserializeObject<Progeny>(progenyResponseString);
            bool hasAccess = false;
            string accessApiPath = "/api/Access/Progeny/" + progenyId;
            string accessResponseString = await _httpClient.GetStringAsync(accessApiPath);
            List<UserAccess>? accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessResponseString);

            if (accessList != null && accessList.Count != 0)
            {
                foreach (UserAccess accessItem in accessList)
                {
                    if (accessItem.UserId.Equals(userId, StringComparison.CurrentCultureIgnoreCase))
                    {
                        hasAccess = true;
                    }
                }
            }

            if (hasAccess) return child;
            if (child == null) return child;

            child.Name = "Test Child";
            child.NickName = "Tester";
            child.Id = -1;
            child.Admins = "per.mogensen@live.com";
            child.BirthDay = DateTime.Now;
            child.TimeZone = TimeZoneInfo.Utc.Id;
            child.PictureLink = "/images/images_placeholder.png";

            return child;
        }

        public async Task<bool> CanUserAddItems(string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string accessApiPath = "/api/Access/AccessListByUser/" + userId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (!accessResponse.IsSuccessStatusCode) return false;

            string accessAsString = await accessResponse.Content.ReadAsStringAsync();
            List<UserAccess>? accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);

            if (accessList == null || accessList.Count == 0) return userId == "Yes";

            foreach (UserAccess ua in accessList)
            {
                if (ua.AccessLevel == 0)
                {
                    return true;
                }
            }

            if (accessList.Count != 0)
            {
            }

            return userId == "Yes";
        }

        private async Task SetViewChild(string userEmail, int childId, string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            UserInfo userinfo = new()
            {
                UserEmail = userEmail,
                ViewChild = childId,
                UserId = userId
            };

            string setChildApiPath = "/api/UserInfo/" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }

        public async Task<bool> IsUserLoginValid(string userId)
        {
            if (userId == Constants.DefaultUserId || userId == "401") return false;

            HttpContext? currentContext = _httpContextAccessor.HttpContext;

            if (currentContext == null) return false;

            string? contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

            if (string.IsNullOrWhiteSpace(contextAccessToken)) return false;

            _httpClient.SetBearerToken(contextAccessToken);
            const string userinfoApiPath = "/api/UserInfo/CheckCurrentUser/";
            HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userinfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
            if (!userInfoResponse.IsSuccessStatusCode) return false;

            string userInfoResponseAsString = await userInfoResponse.Content.ReadAsStringAsync();
            UserInfo? userinfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseAsString);
            return userinfo?.UserId.Equals(userId, StringComparison.CurrentCultureIgnoreCase) ?? false;
        }

        public async Task<bool> IsApplicationUserValid(string userId)
        {
            return await _authHttpClient.IsApplicationUserValid(userId);
        }
    }
}
