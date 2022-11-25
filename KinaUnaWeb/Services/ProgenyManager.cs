using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Models;
using Microsoft.Extensions.Hosting;

namespace KinaUnaWeb.Services
{
    public class ProgenyManager : IProgenyManager
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdentityParser<ApplicationUser> _userManager;
        private readonly ImageStore _imageStore;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHostEnvironment _env;
        private readonly IAuthHttpClient _authHttpClient;
        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, HttpClient httpClient, ApiTokenInMemoryClient apiTokenClient,
            IHostEnvironment env, IAuthHttpClient authHttpClient)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
            _apiTokenClient = apiTokenClient;
            _env = env;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                clientUri = _configuration.GetValue<string>("ProgenyApiServer" + Constants.DebugKinaUnaServer);
            }
            httpClient.BaseAddress = new Uri(clientUri!);
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

            string accessToken = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return accessToken;
        }


        public async Task<UserInfo> GetInfo(string userEmail)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string userInfoApiPath = "/api/UserInfo/ByEmail/" + userEmail;
            UserInfo userInfo = new UserInfo();
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
                ApplicationUser applicationUser = _userManager.Parse(_httpContextAccessor.HttpContext?.User);
                
                UserInfo newUserinfo = new UserInfo();
                newUserinfo.UserEmail = applicationUser.Email;
                newUserinfo.ViewChild = 0;
                newUserinfo.UserId = applicationUser.Id;
                
                newUserinfo.FirstName = applicationUser.FirstName ?? "";
                newUserinfo.MiddleName = applicationUser.MiddleName ?? "";
                newUserinfo.LastName = applicationUser.LastName ?? "";
                // Todo: ProfilePicture
                newUserinfo.Timezone = applicationUser.TimeZone;
                newUserinfo.UserName = applicationUser.UserName;
                newUserinfo.IsKinaUnaUser = true;
                if (String.IsNullOrEmpty(newUserinfo.UserName))
                {
                    newUserinfo.UserName = newUserinfo.UserEmail;
                }

                string newUserinfoApiPath = "/api/UserInfo/";
                HttpResponseMessage newUserResponse = await _httpClient.PostAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(newUserinfo), System.Text.Encoding.UTF8, "application/json"));
                if (newUserResponse.IsSuccessStatusCode)
                {
                    string newUserResponseString = await newUserResponse.Content.ReadAsStringAsync();
                    userInfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
                }
                
            }

            if (userInfo != null && userInfo.ViewChild == 0)
            {
                if (userInfo.ProgenyList.Any())
                {
                    await SetViewChild(userInfo.UserEmail, userInfo.ProgenyList[0].Id, userInfo.UserId);
                }
                else
                {
                    userInfo.ViewChild = Constants.DefaultChildId;
                }
            }
            return userInfo;

        }

        public string GetImageUrl(string pictureLink, string pictureContainer)
        {
            string returnString = pictureLink;
            if (!pictureLink.ToLower().StartsWith("http"))
            {
                returnString = _imageStore.UriFor(pictureLink, pictureContainer);
            }
            return returnString;
        }

        public async Task<UserInfo> UpdateUserInfo(UserInfo userinfo)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            // Todo: ProfilePicture
            string newUserinfoApiPath = "/api/UserInfo/" + userinfo.UserId;
            HttpResponseMessage userInfoResponse = await _httpClient.PutAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json"));
            if (userInfoResponse.IsSuccessStatusCode)
            {
                string userInfoAsString = await userInfoResponse.Content.ReadAsStringAsync();
                UserInfo updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(userInfoAsString);
                return updatedUserinfo;
            }

            return new UserInfo();
        }

        public async Task<Progeny> CurrentChildAsync(int progenyId, string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string progenyApiPath = "/api/Progeny/" + progenyId;
            string progenyResponseString = await _httpClient.GetStringAsync(progenyApiPath);
            Progeny child = JsonConvert.DeserializeObject<Progeny>(progenyResponseString);
            bool hasAccess = false;
            string accessApiPath = "/api/Access/Progeny/" + progenyId;
            string accessResponseString = await _httpClient.GetStringAsync(accessApiPath);
            List<UserAccess> accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessResponseString);

            if (accessList != null && accessList.Any())
            {
                foreach (UserAccess accessItem in accessList)
                {
                    if (accessItem.UserId.ToUpper() == userId.ToUpper())
                    {
                        hasAccess = true;
                    }
                }
            }
            
            if (child == null || !hasAccess)
            {
                if (child == null)
                {
                    child = new Progeny();
                }
                child.Name = "Test Child";
                child.NickName = "Tester";
                child.Id = -1;
                child.Admins = "per.mogensen@live.com";
                child.BirthDay = DateTime.Now;
                child.TimeZone = TimeZoneInfo.Utc.Id;
                child.PictureLink = "/images/images_placeholder.png";

            }
            
            return child;
        }

        public async Task<bool> CanUserAddItems(string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string accessApiPath = "/api/Access/AccessListByUser/" + userId;
            HttpResponseMessage accessResponse = await _httpClient.GetAsync(accessApiPath);
            if (accessResponse.IsSuccessStatusCode)
            {
                string accessAsString = await accessResponse.Content.ReadAsStringAsync();
                List<UserAccess> accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessAsString);

                if (accessList != null)
                {
                    foreach (UserAccess ua in accessList)
                    {
                        if (ua.AccessLevel == 0)
                        {
                            return true;
                        }
                    }

                    if (accessList.Any())
                    {
                    }
                }

                if (userId == "Yes")
                {
                    return true;
                }
            }
            
            return false;
        }

        public async Task SetViewChild(string userEmail, int childId, string userId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            UserInfo userinfo = new UserInfo();
            userinfo.UserEmail = userEmail;
            userinfo.ViewChild = childId;
            userinfo.UserId = userId;

            string setChildApiPath = "/api/UserInfo/" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }

        public async Task<bool> IsUserLoginValid(string userId)
        {
            if (userId != Constants.DefaultUserId && userId != "401")
            {
                HttpContext currentContext = _httpContextAccessor.HttpContext;

                if (currentContext != null)
                {
                    string contextAccessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken);

                    if (!string.IsNullOrWhiteSpace(contextAccessToken))
                    {
                        _httpClient.SetBearerToken(contextAccessToken);
                        const string userinfoApiPath = "/api/UserInfo/CheckCurrentUser/";
                        HttpResponseMessage userInfoResponse = await _httpClient.PostAsync(userinfoApiPath, new StringContent(JsonConvert.SerializeObject(userId), System.Text.Encoding.UTF8, "application/json"));
                        if (userInfoResponse.IsSuccessStatusCode)
                        {
                            string userInfoResponseAsString = await userInfoResponse.Content.ReadAsStringAsync();
                            UserInfo userinfo = JsonConvert.DeserializeObject<UserInfo>(userInfoResponseAsString);
                            if (userinfo?.UserId.ToUpper() == userId.ToUpper())
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public async Task<bool> IsApplicationUserValid(string userId)
        {
            return await _authHttpClient.IsApplicationUserValid(userId);
        }
    }
}
