using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.ItemViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class MediaHttpClient: IMediaHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public MediaHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _httpClient = httpClient;

        }

        private async Task<string> GetNewToken()
        {
            var discoveryClient = new HttpClient();
            
            var tokenResponse = await discoveryClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = _configuration.GetValue<string>("AuthenticationServer") + "/connect/token",

                ClientId = _configuration.GetValue<string>("AuthenticationServerClientId"),
                ClientSecret = _configuration.GetValue<string>("AuthenticationServerClientSecret"),
                Scope = Constants.MediaApiName
            });
            
            return tokenResponse.AccessToken;
        }

        public async Task<HttpClient> GetClient()
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;

            // get access token
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

            _httpClient.BaseAddress = new Uri(clientUri);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return _httpClient;
        }

        public async Task<Picture> GetPicture(int pictureId, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pictureApiPath = "/api/pictures/" + pictureId;
            var pictureUri = clientUri + pictureApiPath;

            var pictureResponseString = await pictureHttpClient.GetStringAsync(pictureUri);

            Picture picture = JsonConvert.DeserializeObject<Picture>(pictureResponseString);
            if (picture.PictureTime.HasValue)
            {
                picture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }
            return picture;
        }

        public async Task<Picture> GetRandomPicture(int progenyId, int accessLevel, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pictureApiPath = "/api/pictures/random/" + progenyId + "/" + accessLevel;
            var pictureUri = clientUri + pictureApiPath;
            var resp = await pictureHttpClient.GetAsync(pictureUri);
            string pictureResponseString = await resp.Content.ReadAsStringAsync();
            
            Picture resultPicture = JsonConvert.DeserializeObject<Picture>(pictureResponseString);
            if (timeZone != "")
            {
                if (resultPicture.PictureTime.HasValue)
                {
                    resultPicture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(resultPicture.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }

            }
            return resultPicture;
        }

        public async Task<List<Picture>> GetPictureList(int progenyId, int accessLevel, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pictureApiPath = "/api/pictures/progeny/" + progenyId + "/" + accessLevel;
            var pictureUri = clientUri + pictureApiPath;
            var resp = await pictureHttpClient.GetAsync(pictureUri);
            string pictureResponseString = await resp.Content.ReadAsStringAsync();

            List<Picture> resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(pictureResponseString);
            if (timeZone != "")
            {
                foreach (Picture pic in resultPictureList)
                {
                    if (pic.PictureTime.HasValue)
                    {
                        pic.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pic.PictureTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }
                }
            }
            return resultPictureList;
        }

        public async Task<List<Picture>> GetAllPictures()
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pictureApiPath = "/api/pictures";
            var pictureUri = clientUri + pictureApiPath;
            var resp = await pictureHttpClient.GetAsync(pictureUri);
            string pictureResponseString = await resp.Content.ReadAsStringAsync();

            List<Picture> resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(pictureResponseString);
            
            return resultPictureList;
        }

        public async Task<Picture> AddPicture(Picture picture)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient newPictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newPictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newPictureHttpClient.SetBearerToken(accessToken);
            }
            newPictureHttpClient.BaseAddress = new Uri(clientUri);
            newPictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            newPictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string newPictureApiPath = "/api/pictures/";
            var newPictureUri = clientUri + newPictureApiPath;

            await newPictureHttpClient.PostAsync(newPictureUri, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json")).Result.Content.ReadAsStringAsync();
            newPictureUri = clientUri + "/api/pictures/bylink/" + picture.PictureLink;
            var newPictureResponseString = await newPictureHttpClient.GetStringAsync(newPictureUri);
            Picture newPicture = JsonConvert.DeserializeObject<Picture>(newPictureResponseString);

            return newPicture;
        }

        public async Task<Picture> UpdatePicture(Picture picture)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            
            HttpClient updatePictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updatePictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updatePictureHttpClient.SetBearerToken(accessToken);
            }
            updatePictureHttpClient.BaseAddress = new Uri(clientUri);
            updatePictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            updatePictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updatePictureApiPath = "/api/pictures/" + picture.PictureId;
            var updatePictureUri = clientUri + updatePictureApiPath;
            var updatePictureResponseString = await updatePictureHttpClient.PutAsync(updatePictureUri, picture, new JsonMediaTypeFormatter());
            string returnString = await updatePictureResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Picture>(returnString);
        }

        public async Task<bool> DeletePicture(int pictureId)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient deletePictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                deletePictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                deletePictureHttpClient.SetBearerToken(accessToken);
            }
            deletePictureHttpClient.BaseAddress = new Uri(clientUri);
            deletePictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            deletePictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string deletePictureApiPath = "/api/pictures/" + pictureId;
            var deletePictureUri = clientUri + deletePictureApiPath;

            var deletePictureResponse = await deletePictureHttpClient.DeleteAsync(deletePictureUri).ConfigureAwait(false);
            if (deletePictureResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> AddPictureComment(Comment comment)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient newCommentHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newCommentHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newCommentHttpClient.SetBearerToken(accessToken);
            }
            newCommentHttpClient.BaseAddress = new Uri(clientUri);
            newCommentHttpClient.DefaultRequestHeaders.Accept.Clear();
            newCommentHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string newCommentApiPath = "/api/comments/";
            var newCommentUri = clientUri + newCommentApiPath;

            var newCommentResponse = await newCommentHttpClient.PostAsync(newCommentUri, new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeletePictureComment(int commentId)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient deleteCommentHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                deleteCommentHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                deleteCommentHttpClient.SetBearerToken(accessToken);
            }
            deleteCommentHttpClient.BaseAddress = new Uri(clientUri);
            deleteCommentHttpClient.DefaultRequestHeaders.Accept.Clear();
            deleteCommentHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string deleteCommentApiPath = "/api/comments/" + commentId;
            var deleteCommentUri = clientUri + deleteCommentApiPath;

            var newCommentResponse = await deleteCommentHttpClient.DeleteAsync(deleteCommentUri).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<PicturePageViewModel> GetPicturePage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy, string tagFilter, string timeZone)
        {

            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient pageHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pageHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pageHttpClient.SetBearerToken(accessToken);
            }
            pageHttpClient.BaseAddress = new Uri(clientUri);
            pageHttpClient.DefaultRequestHeaders.Accept.Clear();
            pageHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pageApiPath = "/api/pictures/page?pageSize=" + pageSize + "&pageIndex=" + id + "&progenyId=" + progenyId + "&accessLevel=" + userAccessLevel + "&sortBy=" + sortBy;
            if (tagFilter != "")
            {
                pageApiPath = pageApiPath + "&tagFilter=" + tagFilter;
            }
            
            var pageUri = clientUri + pageApiPath;

            var pageResponseString = await pageHttpClient.GetStringAsync(pageUri);
            PicturePageViewModel model = JsonConvert.DeserializeObject<PicturePageViewModel>(pageResponseString);
            if (timeZone != "")
            {
                foreach (Picture pic in model.PicturesList)
                {

                    if (pic.PictureTime.HasValue)
                    {
                        pic.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pic.PictureTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }


                }
            }
            
            return model;
        }

        public async Task<PictureViewModel> GetPictureViewModel(int id, int userAccessLevel, int sortBy, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient pictureHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pictureHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pictureHttpClient.SetBearerToken(accessToken);
            }
            pictureHttpClient.BaseAddress = new Uri(clientUri);
            pictureHttpClient.DefaultRequestHeaders.Accept.Clear();
            pictureHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pageApiPath = "/api/pictures/pictureviewmodel/" + id + "/" + userAccessLevel + "?sortBy=" + sortBy;
            var pictureUri = clientUri + pageApiPath;

            var pictureResponseString = await pictureHttpClient.GetStringAsync(pictureUri);

            PictureViewModel picture = JsonConvert.DeserializeObject<PictureViewModel>(pictureResponseString);
            if (timeZone != "")
            {
                if (picture.PictureTime.HasValue)
                {
                    picture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }
            }
            return picture;
        }

        public async Task<VideoPageViewModel> GetVideoPage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy, string tagFilter, string timeZone)
        {

            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient pageHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                pageHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                pageHttpClient.SetBearerToken(accessToken);
            }
            pageHttpClient.BaseAddress = new Uri(clientUri);
            pageHttpClient.DefaultRequestHeaders.Accept.Clear();
            pageHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pageApiPath = "/api/videos/page?pageSize=" + pageSize + "&pageIndex=" + id + "&progenyId=" + progenyId + "&accessLevel=" + userAccessLevel + "&sortBy=" + sortBy;
            if (tagFilter != "")
            {
                pageApiPath = pageApiPath + "&tagFilter=" + tagFilter;
            }

            pageApiPath = pageApiPath + "&timeZone=" + timeZone;

            var pageUri = clientUri + pageApiPath;

            var pageResponseString = await pageHttpClient.GetStringAsync(pageUri);
            VideoPageViewModel model = JsonConvert.DeserializeObject<VideoPageViewModel>(pageResponseString);

            if (timeZone != "")
            {
                foreach (Video vid in model.VideosList)
                {

                    if (vid.VideoTime.HasValue)
                    {
                        vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }
                }
            }
            return model;
        }

        public async Task<VideoViewModel> GetVideoViewModel(int id, int userAccessLevel, int sortBy, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient videoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                videoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                videoHttpClient.SetBearerToken(accessToken);
            }
            videoHttpClient.BaseAddress = new Uri(clientUri);
            videoHttpClient.DefaultRequestHeaders.Accept.Clear();
            videoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string pageApiPath = "/api/videos/videoviewmodel/" + id + "/" + userAccessLevel + "?sortBy=" + sortBy;
            var videoUri = clientUri + pageApiPath;

            var videoResponseString = await videoHttpClient.GetStringAsync(videoUri);

            VideoViewModel video = JsonConvert.DeserializeObject<VideoViewModel>(videoResponseString);

            if (timeZone != "")
            {
                if (video.VideoTime.HasValue)
                {
                   video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(video.VideoTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }
            }
            return video;
        }

        public async Task<Video> GetVideo(int videoId, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient videoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                videoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                videoHttpClient.SetBearerToken(accessToken);
            }
            videoHttpClient.BaseAddress = new Uri(clientUri);
            videoHttpClient.DefaultRequestHeaders.Accept.Clear();
            videoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string videoApiPath = "/api/videos/" + videoId;
            var videoUri = clientUri + videoApiPath;

            var videoResponseString = await videoHttpClient.GetStringAsync(videoUri);

            Video video = JsonConvert.DeserializeObject<Video>(videoResponseString);
            if (timeZone != "")
            {
                if (video.VideoTime.HasValue)
                {
                    video.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(video.VideoTime.Value,
                       TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }
            }
            return video;
        }

        public async Task<List<Video>> GetVideoList(int progenyId, int accessLevel, string timeZone)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient videoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                videoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                videoHttpClient.SetBearerToken(accessToken);
            }
            videoHttpClient.BaseAddress = new Uri(clientUri);
            videoHttpClient.DefaultRequestHeaders.Accept.Clear();
            videoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string videoApiPath = "/api/videos/progeny/" + progenyId + "/" + accessLevel;
            var videoUri = clientUri + videoApiPath;
            var resp = await videoHttpClient.GetAsync(videoUri);
            string videoResponseString = await resp.Content.ReadAsStringAsync();

            List<Video> resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoResponseString);
            if (timeZone != "")
            {
                foreach (Video vid in resultVideoList)
                {
                    if (vid.VideoTime.HasValue)
                    {
                        vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }
                }
            }
            return resultVideoList;
        }

        public async Task<List<Video>> GetAllVideos()
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient videoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                videoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                videoHttpClient.SetBearerToken(accessToken);
            }
            videoHttpClient.BaseAddress = new Uri(clientUri);
            videoHttpClient.DefaultRequestHeaders.Accept.Clear();
            videoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string videoApiPath = "/api/videos";
            var videoUri = clientUri + videoApiPath;
            var resp = await videoHttpClient.GetAsync(videoUri);
            string videoResponseString = await resp.Content.ReadAsStringAsync();

            List<Video> resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoResponseString);

            return resultVideoList;
        }
        public async Task<Video> AddVideo(Video video)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            // get the current HttpContext to access the tokens
            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient newVideoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newVideoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newVideoHttpClient.SetBearerToken(accessToken);
            }
            newVideoHttpClient.BaseAddress = new Uri(clientUri);
            newVideoHttpClient.DefaultRequestHeaders.Accept.Clear();
            newVideoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string newVideoApiPath = "/api/videos/";
            var newVideoUri = clientUri + newVideoApiPath;

            var newVideoResponse = await newVideoHttpClient.PostAsync(newVideoUri, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            
            var newVideoResponseString = await newVideoResponse.Content.ReadAsStringAsync();
            Video newVideo = JsonConvert.DeserializeObject<Video>(newVideoResponseString);

            return newVideo;
        }

        public async Task<Video> UpdateVideo(Video video)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);

            HttpClient updateVideoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                updateVideoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                updateVideoHttpClient.SetBearerToken(accessToken);
            }
            updateVideoHttpClient.BaseAddress = new Uri(clientUri);
            updateVideoHttpClient.DefaultRequestHeaders.Accept.Clear();
            updateVideoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            string updateVideoApiPath = "/api/videos/" + video.VideoId;
            var updateVideoUri = clientUri + updateVideoApiPath;
            var updateVideoResponseString = await updateVideoHttpClient.PutAsync(updateVideoUri, video, new JsonMediaTypeFormatter());
            string returnString = await updateVideoResponseString.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Video>(returnString);
        }

        public async Task<bool> DeleteVideo(int videoId)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient deleteVideoHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                deleteVideoHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                deleteVideoHttpClient.SetBearerToken(accessToken);
            }
            deleteVideoHttpClient.BaseAddress = new Uri(clientUri);
            deleteVideoHttpClient.DefaultRequestHeaders.Accept.Clear();
            deleteVideoHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string deleteVideoApiPath = "/api/videos/" + videoId;
            var deleteVideoUri = clientUri + deleteVideoApiPath;

            var deleteVideoResponse = await deleteVideoHttpClient.DeleteAsync(deleteVideoUri).ConfigureAwait(false);
            if (deleteVideoResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> AddVideoComment(Comment comment)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient newCommentHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                newCommentHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                newCommentHttpClient.SetBearerToken(accessToken);
            }
            newCommentHttpClient.BaseAddress = new Uri(clientUri);
            newCommentHttpClient.DefaultRequestHeaders.Accept.Clear();
            newCommentHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string newCommentApiPath = "/api/comments/";
            var newCommentUri = clientUri + newCommentApiPath;

            var newCommentResponse = await newCommentHttpClient.PostAsync(newCommentUri, new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteVideoComment(int commentId)
        {
            string clientUri = _configuration.GetValue<string>("MediaApiServer");

            var currentContext = _httpContextAccessor.HttpContext;
            string accessToken = await currentContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken).ConfigureAwait(false);
            HttpClient deleteCommentHttpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                deleteCommentHttpClient.SetBearerToken(accessToken);
            }
            else
            {
                accessToken = await GetNewToken();
                deleteCommentHttpClient.SetBearerToken(accessToken);
            }
            deleteCommentHttpClient.BaseAddress = new Uri(clientUri);
            deleteCommentHttpClient.DefaultRequestHeaders.Accept.Clear();
            deleteCommentHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));


            string deleteCommentApiPath = "/api/comments/" + commentId;
            var deleteCommentUri = clientUri + deleteCommentApiPath;

            var newCommentResponse = await deleteCommentHttpClient.DeleteAsync(deleteCommentUri).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

    }
}
