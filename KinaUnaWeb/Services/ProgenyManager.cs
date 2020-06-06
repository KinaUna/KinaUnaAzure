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

        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore, HttpClient httpClient, ApiTokenInMemoryClient apiTokenClient)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
            _httpClient = httpClient;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<string> GetNewToken()
        {
            var access_token = await _apiTokenClient.GetApiToken(
                    _configuration.GetValue<string>("AuthenticationServerClientId"),
                    Constants.ProgenyApiName + " " + Constants.MediaApiName,
                    _configuration.GetValue<string>("AuthenticationServerClientSecret"));
            return access_token;
        }


        public async Task<UserInfo> GetInfo(string userEmail)
        {
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));
            
            string userinfoApiPath = "/api/userinfo/byemail/" + userEmail;
            //var userinfoUri = clientUri + userinfoApiPath;
            UserInfo userinfo = new UserInfo();
            try
            {
                var userinfoResponseString = await _httpClient.GetStringAsync(userinfoApiPath);
                userinfo = JsonConvert.DeserializeObject<UserInfo>(userinfoResponseString);
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
                //HttpClient _httpClient = new HttpClient();
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    // set as Bearer token
                    _httpClient.SetBearerToken(accessToken);
                }
                else
                {
                    accessToken = await GetNewToken();
                    _httpClient.SetBearerToken(accessToken);
                }
                //_httpClient.BaseAddress = new Uri(clientUri);
                //_httpClient.DefaultRequestHeaders.Accept.Clear();
                //_httpClient.DefaultRequestHeaders.Accept.Add(
                //    new MediaTypeWithQualityHeaderValue("application/json"));
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

                if (String.IsNullOrEmpty(newUserinfo.UserName))
                {
                    newUserinfo.UserName = newUserinfo.UserEmail;
                }

                string newUserinfoApiPath = "/api/userinfo/";
                //var newUserinfoUri = clientUri + newUserinfoApiPath;

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
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
             //   new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Todo: ProfilePicture
            string newUserinfoApiPath = "/api/userinfo/" + userinfo.Id;
            //var newUserinfoUri = clientUri + newUserinfoApiPath;

            var newUserResponseString = await _httpClient.PutAsync(newUserinfoApiPath, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            var updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo;
        }

        public async Task<Progeny> CurrentChildAsync(int progenyId, string userId)
        {
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;
            // get access token
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            //HttpClient _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string progenyApiPath = "/api/progeny/" + progenyId;
            //var progenyUri = clientUri + progenyApiPath;
            var progenyResponseString = await _httpClient.GetStringAsync(progenyApiPath);
            Progeny child = JsonConvert.DeserializeObject<Progeny>(progenyResponseString);

            bool hasAccess = false;
            string accessApiPath = "/api/access/progeny/" + progenyId;
            //var accessUri = clientUri + accessApiPath;
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
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            HttpContext currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/accesslistbyuser/" + userId;
            //var accessUri = clientUri + accessApiPath;
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
            //HttpClient _httpClient = new HttpClient();
            //string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            HttpContext currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                _httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                _httpClient.SetBearerToken(accessToken);
            }
            //_httpClient.BaseAddress = new Uri(clientUri);
            //_httpClient.DefaultRequestHeaders.Accept.Clear();
            //_httpClient.DefaultRequestHeaders.Accept.Add(
            //    new MediaTypeWithQualityHeaderValue("application/json"));

            UserInfo userinfo = new UserInfo();
            userinfo.UserEmail = userEmail;
            userinfo.ViewChild = childId;
            userinfo.UserId = userId;

            string setChildApiPath = "/api/userinfo/" + userId;
            //var setChildUri = clientUri + setChildApiPath;
            await _httpClient.PutAsJsonAsync(setChildApiPath, userinfo);
        }
    }
}
