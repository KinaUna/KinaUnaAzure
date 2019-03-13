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
        public ProgenyManager(IHttpContextAccessor httpContextAccessor, IConfiguration configuration, IIdentityParser<ApplicationUser> userManager, ImageStore imageStore)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _imageStore = imageStore;
        }

        private async Task<string> GetNewToken()
        {
            var discoveryClient = new HttpClient();

            var tokenResponse = await discoveryClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = _configuration.GetValue<string>("AuthenticationServer"),

                ClientId = _configuration.GetValue<string>("AuthenticationServerClientId"),
                ClientSecret = _configuration.GetValue<string>("AuthenticationServerClientSecret"),
                Scope = Constants.ProgenyApiName
            });

            return tokenResponse.AccessToken;
        }


        public async Task<UserInfo> GetInfo(string userEmail)
        {
            UserInfo userinfo = new UserInfo();
            
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            string userinfoApiPath = "/api/userinfo/byemail/" + userEmail;
            var userinfoUri = clientUri + userinfoApiPath;
            try
            {
                var userinfoResponseString = await httpClient.GetStringAsync(userinfoUri);
                userinfo = JsonConvert.DeserializeObject<UserInfo>(userinfoResponseString);
            }
            catch (Exception e)
            {
                userinfo.UserId = "401";
                userinfo.UserName = e.Message;
                return userinfo;
            }

            if (userinfo.UserEmail == "Unknown")
            {
                ApplicationUser userId = _userManager.Parse(currentContext.User);
                HttpClient newUserinfoHttpClient = new HttpClient();
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    // set as Bearer token
                    newUserinfoHttpClient.SetBearerToken(accessToken);
                }
                else
                {
                    accessToken = await GetNewToken();
                    newUserinfoHttpClient.SetBearerToken(accessToken);
                }
                newUserinfoHttpClient.BaseAddress = new Uri(clientUri);
                newUserinfoHttpClient.DefaultRequestHeaders.Accept.Clear();
                newUserinfoHttpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
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
                var newUserinfoUri = clientUri + newUserinfoApiPath;

                var newUserResponseString = await newUserinfoHttpClient.PostAsync(newUserinfoUri, new StringContent(JsonConvert.SerializeObject(newUserinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
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
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient newUserinfoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newUserinfoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newUserinfoHttpClient.SetBearerToken(accessToken);
            }
            newUserinfoHttpClient.BaseAddress = new Uri(clientUri);
            newUserinfoHttpClient.DefaultRequestHeaders.Accept.Clear();
            newUserinfoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Todo: ProfilePicture
            string newUserinfoApiPath = "/api/userinfo/" + userinfo.Id;
            var newUserinfoUri = clientUri + newUserinfoApiPath;

            var newUserResponseString = await newUserinfoHttpClient.PutAsync(newUserinfoUri, new StringContent(JsonConvert.SerializeObject(userinfo), System.Text.Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            var updatedUserinfo = JsonConvert.DeserializeObject<UserInfo>(newUserResponseString);
            return updatedUserinfo;
        }

        public async Task<Progeny> CurrentChildAsync(int progenyId, string userId)
        {
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;
            // get access token
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string progenyApiPath = "/api/progeny/" + progenyId;
            var progenyUri = clientUri + progenyApiPath;
            var progenyResponseString = await httpClient.GetStringAsync(progenyUri);
            Progeny child = JsonConvert.DeserializeObject<Progeny>(progenyResponseString);

            bool hasAccess = false;
            string accessApiPath = "/api/access/progeny/" + progenyId;
            var accessUri = clientUri + accessApiPath;
            var accessResponseString = await httpClient.GetStringAsync(accessUri);
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
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string accessApiPath = "/api/access/accesslistbyuser/" + userId;
            var accessUri = clientUri + accessApiPath;
            var accessResponseString = await httpClient.GetStringAsync(accessUri);
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
            HttpClient httpClient = new HttpClient();
            string clientUri = _configuration.GetValue<string>("ProgenyApiServer");
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await AuthenticationHttpContextExtensions.GetTokenAsync(currentContext, OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // set as Bearer token
                httpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                httpClient.SetBearerToken(accessToken);
            }
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            UserInfo userinfo = new UserInfo();
            userinfo.UserEmail = userEmail;
            userinfo.ViewChild = childId;
            userinfo.UserId = userId;

            string setChildApiPath = "/api/userinfo/" + userId;
            var setChildUri = clientUri + setChildApiPath;
            await httpClient.PutAsJsonAsync(setChildUri, userinfo);
        }
    }
}
