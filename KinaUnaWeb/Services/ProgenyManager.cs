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

        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, HttpClient httpClient, ApiTokenInMemoryClient apiTokenClient, IHostEnvironment env)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
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
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<string> GetNewToken()
        {
            var authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId");
            if (_env.IsDevelopment() && !string.IsNullOrEmpty(Constants.DebugKinaUnaServer))
            {
                authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId" + Constants.DebugKinaUnaServer);
            }

            var access_token = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return access_token;
        }


        public async Task<UserInfo> GetInfo(string userEmail)
        {
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            
            string userinfoApiPath = "/api/userinfo/byemail/" + userEmail;
            UserInfo userinfo = new UserInfo();
            try
            {
                var userinfoResponseString = await _httpClient.GetStringAsync(userinfoApiPath);
                userinfo = JsonConvert.DeserializeObject<UserInfo>(userinfoResponseString);
                if (userinfo != null && !userinfo.IsKinaUnaUser)
                {
                    if (userinfo.UserEmail != "Unknown")
                    {
                        userinfo.IsKinaUnaUser = true;
                        await UpdateUserInfo(userinfo);
                    }
                }
            }
            catch (Exception e)
            {
                userinfo.UserId = "401";
                userinfo.UserName = e.Message;
                userinfo.UserEmail = Constants.DefaultUserEmail;
                userinfo.CanUserAddItems = false;
                userinfo.ViewChild = Constants.DefaultChildId;
                return userinfo;
            }

            if (userinfo.UserEmail == "Unknown")
            {
                ApplicationUser userId = _userManager.Parse(currentContext.User);
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    _httpClient.SetBearerToken(accessToken);
                }
                else
                {
                    accessToken = await GetNewToken();
                    _httpClient.SetBearerToken(accessToken);
                }
            
                UserInfo newUserinfo = new UserInfo();
                newUserinfo.UserEmail = userId.Email;
                newUserinfo.ViewChild = 0;
                newUserinfo.UserId = userId.Id;
                
                newUserinfo.FirstName = userId.FirstName ?? "";
                newUserinfo.MiddleName = userId.MiddleName ?? "";
                newUserinfo.LastName = userId.LastName ?? "";
                // Todo: ProfilePicture
                newUserinfo.Timezone = userId.TimeZone;
                newUserinfo.UserName = userId.UserName;
                newUserinfo.IsKinaUnaUser = true;
                if (String.IsNullOrEmpty(newUserinfo.UserName))
                {
                    newUserinfo.UserName = newUserinfo.UserEmail;
                }

                string newUserinfoApiPath = "/api/userinfo/";
                var newUserResponseString = await _httpClient.PostAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(newUserinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
                userinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            }

            if (userinfo.ViewChild == 0)
            {
                if (userinfo.ProgenyList.Any())
                {
                    await SetViewChild(userinfo.UserEmail, userinfo.ProgenyList[0].Id, userinfo.UserId);
                }
                else
                {
                    userinfo.ViewChild = Constants.DefaultChildId;
                }
            }
            return userinfo;

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
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            
            // Todo: ProfilePicture
            string newUserinfoApiPath = "/api/userinfo/" + userinfo.UserId;
            var newUserResponseString = await _httpClient.PutAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            var updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo;
        }

        public async Task<Progeny> CurrentChildAsync(int progenyId, string userId)
        {
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            
            string progenyApiPath = "/api/progeny/" + progenyId;
            var progenyResponseString = await _httpClient.GetStringAsync(progenyApiPath);
            Progeny child = JsonConvert.DeserializeObject<Progeny>(progenyResponseString);
            bool hasAccess = false;
            string accessApiPath = "/api/access/progeny/" + progenyId;
            var accessResponseString = await _httpClient.GetStringAsync(accessApiPath);
            List<UserAccess> accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessResponseString);

            if (accessList.Any())
            {
                foreach (var accessItem in accessList)
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
            HttpContext currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            
            string accessApiPath = "/api/access/accesslistbyuser/" + userId;
            var accessResponseString = await _httpClient.GetStringAsync(accessApiPath);
            List<UserAccess> accessList = JsonConvert.DeserializeObject<List<UserAccess>>(accessResponseString);

            foreach(UserAccess ua in accessList)
            {
                if (ua.AccessLevel == 0)
                {
                    return true;
                }
            }
            if (accessList.Any())
            {
                
            }

            if (userId == "Yes")
            {
                return true;
            }
            return false;
        }

        public async Task SetViewChild(string userEmail, int childId, string userId)
        {
            HttpContext currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            
            UserInfo userinfo = new UserInfo();
            userinfo.UserEmail = userEmail;
            userinfo.ViewChild = childId;
            userinfo.UserId = userId;

            string setChildApiPath = "/api/userinfo/" + userId;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }
    }
}
