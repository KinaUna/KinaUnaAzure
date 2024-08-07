﻿using System.Net.Http.Headers;
using System.Text;
using IdentityModel.Client;
using KinaUna.Data;
using KinaUna.Data.Models;
using KinaUnaWebBlazor.Models.ItemViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Newtonsoft.Json;

namespace KinaUnaWebBlazor.Services
{
    public class MediaHttpClient: IMediaHttpClient
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public MediaHttpClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _apiTokenClient = apiTokenClient;
            string clientUri = _configuration.GetValue<string>("MediaApiServer") ?? throw new InvalidOperationException("MediaApiServer value missing in configuration");
            
            httpClient.BaseAddress = new Uri(clientUri);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
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

            string authenticationServerClientId = _configuration.GetValue<string>("AuthenticationServerClientId") ?? throw new InvalidOperationException("AuthenticationServerClientId value missing in configuration");

            string accessToken = await _apiTokenClient.GetApiToken(
                authenticationServerClientId,
                Constants.ProgenyApiName + " " + Constants.MediaApiName,
                _configuration.GetValue<string>("AuthenticationServerClientSecret") ?? throw new InvalidOperationException("AuthenticationServerClientSecret value missing in configuration"));
            return accessToken;
        }

        public async Task<Picture?> GetPicture(int pictureId, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "/api/Pictures/" + pictureId;
            
            HttpResponseMessage pictureResponse = await _httpClient.GetAsync(pictureApiPath);
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
                Picture? picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
                if (picture != null && picture.PictureTime.HasValue)
                {
                    picture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }

                return picture;
            }
            
            return new Picture();
        }

        public async Task<Picture?> GetRandomPicture(int progenyId, int accessLevel, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "api/Pictures/Random/" + progenyId + "/" + accessLevel;
            HttpResponseMessage pictureResponse = await _httpClient.GetAsync(pictureApiPath);
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureResponseString = await pictureResponse.Content.ReadAsStringAsync();
                Picture? resultPicture = JsonConvert.DeserializeObject<Picture>(pictureResponseString);
                if (timeZone != "")
                {
                    if (resultPicture != null && resultPicture.PictureTime.HasValue)
                    {
                        resultPicture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(resultPicture.PictureTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }
                }

                return resultPicture;
            }

            return new Picture();
        }

        public async Task<List<Picture>?> GetPictureList(int progenyId, int accessLevel, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "/api/Pictures/Progeny/" + progenyId + "/" + accessLevel;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pictureApiPath);
            if (picturesResponse.IsSuccessStatusCode)
            {
                string picturesListAsString = await picturesResponse.Content.ReadAsStringAsync();
                List<Picture>? resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(picturesListAsString);
                if (timeZone != "")
                {
                    if (resultPictureList != null)
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

            return [];
        }

        public async Task<List<Picture>?> GetAllPictures()
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "/api/Pictures";
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pictureApiPath);
            if (picturesResponse.IsSuccessStatusCode)
            {
                string pictureResponseString = await picturesResponse.Content.ReadAsStringAsync();

                List<Picture>? resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(pictureResponseString);

                return resultPictureList;
            }

            return [];
        }

        public async Task<Picture?> AddPicture(Picture? picture)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newPictureApiPath = "/api/Pictures/";
            
            HttpResponseMessage pictureResponse = await _httpClient.PostAsync(newPictureApiPath, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json"));
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
                picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
                string newPictureUri = "/api/Pictures/ByLink/" + picture?.PictureLink;
                HttpResponseMessage newPictureResponse = await _httpClient.GetAsync(newPictureUri);
                if (newPictureResponse.IsSuccessStatusCode)
                {
                    string newPictureAsString = await newPictureResponse.Content.ReadAsStringAsync();
                    Picture? newPicture = JsonConvert.DeserializeObject<Picture>(newPictureAsString);
                    return newPicture;
                }
            }

            return new Picture();
        }

        public async Task<Picture?> UpdatePicture(Picture? picture)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updatePictureApiPath = "/api/Pictures/" + picture?.PictureId;

            HttpResponseMessage pictureResponse = await _httpClient.PutAsync(updatePictureApiPath, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json"));
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
                picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
                return picture;
            }

            return new Picture();
        }

        public async Task<bool> DeletePicture(int pictureId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string deletePictureApiPath = "/api/Pictures/" + pictureId;
            
            HttpResponseMessage deletePictureResponse = await _httpClient.DeleteAsync(deletePictureApiPath);
            if (deletePictureResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> AddPictureComment(Comment comment)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newCommentApiPath = "/api/Comments/";
            
            HttpResponseMessage newCommentResponse = await _httpClient.PostAsync(newCommentApiPath, new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeletePictureComment(int commentId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string deleteCommentApiPath = "/api/Comments/" + commentId;
            
            HttpResponseMessage newCommentResponse = await _httpClient.DeleteAsync(deleteCommentApiPath).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<PicturePageViewModel?> GetPicturePage(int pageSize, int id, int progenyId, int sortBy, string tagFilter, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Pictures/Page?pageSize=" + pageSize + "&pageIndex=" + id + "&progenyId=" + progenyId + "&accessLevel=0" + "&sortBy=" + sortBy;
            if (tagFilter != "")
            {
                pageApiPath = pageApiPath + "&tagFilter=" + tagFilter;
            }
            
            HttpResponseMessage picturePageResponse = await _httpClient.GetAsync(pageApiPath);
            if (picturePageResponse.IsSuccessStatusCode)
            {
                string pageResponseString = await picturePageResponse.Content.ReadAsStringAsync();

                PicturePageViewModel? model = JsonConvert.DeserializeObject<PicturePageViewModel>(pageResponseString);
                if (model != null && timeZone != "" && model.PicturesList.Count != 0)
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

            return new PicturePageViewModel();
        }

        public async Task<PictureViewModel?> GetPictureViewModel(int id, int sortBy, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Pictures/PictureViewModel/" + id + "/0" + "?sortBy=" + sortBy;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pageApiPath);
            if (picturesResponse.IsSuccessStatusCode)
            {
                string picturesViewModelAsString = await picturesResponse.Content.ReadAsStringAsync();
                PictureViewModel? pictureViewModel = JsonConvert.DeserializeObject<PictureViewModel>(picturesViewModelAsString);
                if (pictureViewModel != null)
                {
                    pictureViewModel.Longitude = pictureViewModel.Longtitude;
                    if (timeZone != "")
                    {
                        if (pictureViewModel.PictureTime.HasValue)
                        {
                            pictureViewModel.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pictureViewModel.PictureTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }

                        if (pictureViewModel.CommentsList.Count > 0)
                        {
                            foreach (Comment cmnt in pictureViewModel.CommentsList)
                            {
                                cmnt.Created = TimeZoneInfo.ConvertTimeFromUtc(cmnt.Created,
                                    TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                            }
                        }
                    }

                    return pictureViewModel;
                }
            }

            return new PictureViewModel();
        }

        public async Task<VideoPageViewModel?> GetVideoPage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy, string tagFilter, string timeZone)
        {

            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Videos/Page?pageSize=" + pageSize + "&pageIndex=" + id + "&progenyId=" + progenyId + "&accessLevel=" + userAccessLevel + "&sortBy=" + sortBy;
            if (tagFilter != "")
            {
                pageApiPath = pageApiPath + "&tagFilter=" + tagFilter;
            }

            pageApiPath = pageApiPath + "&timeZone=" + timeZone;

            HttpResponseMessage videoResponse = await _httpClient.GetAsync(pageApiPath);
            if (videoResponse.IsSuccessStatusCode)
            {
                string videoPageAsString = await videoResponse.Content.ReadAsStringAsync();
                VideoPageViewModel? model = JsonConvert.DeserializeObject<VideoPageViewModel>(videoPageAsString);

                if (model != null && timeZone != "" && model.VideosList.Count != 0)
                {
                    foreach (Video vid in model.VideosList)
                    {

                        if (vid.VideoTime.HasValue)
                        {
                            vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                return model;
            }

            return new VideoPageViewModel();
        }

        public async Task<VideoViewModel?> GetVideoViewModel(int id, int userAccessLevel, int sortBy, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Videos/VideoViewModel/" + id + "/" + userAccessLevel + "?sortBy=" + sortBy;
            
            HttpResponseMessage videoViewModelResponse = await _httpClient.GetAsync(pageApiPath);
            if (videoViewModelResponse.IsSuccessStatusCode)
            {
                string videoViewModelAsString = await videoViewModelResponse.Content.ReadAsStringAsync();
                VideoViewModel? videoViewModel = JsonConvert.DeserializeObject<VideoViewModel>(videoViewModelAsString);

                if (timeZone != "")
                {
                    if (videoViewModel != null && videoViewModel.VideoTime.HasValue)
                    {
                        videoViewModel.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(videoViewModel.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }

                    if (videoViewModel != null && videoViewModel.CommentsList.Count > 0)
                    {
                        foreach (Comment cmnt in videoViewModel.CommentsList)
                        {
                            cmnt.Created = TimeZoneInfo.ConvertTimeFromUtc(cmnt.Created, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                return videoViewModel;
            }

            return new VideoViewModel();
        }

        public async Task<Video?> GetVideo(int videoId, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);

            string videoApiPath = "/api/Videos/" + videoId;

            HttpResponseMessage videoResponse = await _httpClient.GetAsync(videoApiPath);
            if (videoResponse.IsSuccessStatusCode)
            {
                string videoAsString = await videoResponse.Content.ReadAsStringAsync();
                Video? resultVideo = JsonConvert.DeserializeObject<Video>(videoAsString);
                if (timeZone != "")
                {
                    if (resultVideo != null && resultVideo.VideoTime.HasValue)
                    {
                        resultVideo.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(resultVideo.VideoTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }
                }

                return resultVideo;
            }

            return new Video();
        }

        public async Task<List<Video>?> GetVideoList(int progenyId, int accessLevel, string timeZone)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string videoApiPath = "/api/Videos/Progeny/" + progenyId + "/" + accessLevel;
            HttpResponseMessage videosListReponse = await _httpClient.GetAsync(videoApiPath);
            if (videosListReponse.IsSuccessStatusCode)
            {
                string videoListAsString = await videosListReponse.Content.ReadAsStringAsync();
                List<Video>? resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoListAsString);
                if (timeZone != "")
                {
                    if (resultVideoList != null)
                        foreach (Video vid in resultVideoList)
                        {
                            if (vid.VideoTime.HasValue)
                            {
                                vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                            }
                        }
                }

                return resultVideoList;
            }

            return [];
        }

        public async Task<List<Video>?> GetAllVideos()
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string videoApiPath = "/api/Videos";
            HttpResponseMessage videosResponse = await _httpClient.GetAsync(videoApiPath);
            if (videosResponse.IsSuccessStatusCode)
            {
                string videoResponseString = await videosResponse.Content.ReadAsStringAsync();

                List<Video>? resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoResponseString);
                return resultVideoList;
            }

            return [];
        }
        public async Task<Video?> AddVideo(Video? video)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newVideoApiPath = "/api/Videos/";
            
            HttpResponseMessage newVideoResponse = await _httpClient.PostAsync(newVideoApiPath, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            if (newVideoResponse.IsSuccessStatusCode)
            {
                string videoAsString = await newVideoResponse.Content.ReadAsStringAsync();
                video = JsonConvert.DeserializeObject<Video>(videoAsString);
                return video;
            }

            return new Video();
        }

        public async Task<Video?> UpdateVideo(Video? video)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateVideoApiPath = "/api/Videos/" + video?.VideoId;
            HttpResponseMessage videoResponse = await _httpClient.PutAsync(updateVideoApiPath, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            if (videoResponse.IsSuccessStatusCode)
            {
                string videoAsString = await videoResponse.Content.ReadAsStringAsync();
                video = JsonConvert.DeserializeObject<Video>(videoAsString);
                return video;
            }

            return new Video();
        }

        public async Task<bool> DeleteVideo(int videoId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string deleteVideoApiPath = "/api/videos/" + videoId;
            
            HttpResponseMessage deleteVideoResponse = await _httpClient.DeleteAsync(deleteVideoApiPath).ConfigureAwait(false);
            if (deleteVideoResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> AddVideoComment(Comment comment)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newCommentApiPath = "/api/comments/";
            HttpResponseMessage newCommentResponse = await _httpClient.PostAsync(newCommentApiPath, new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<bool> DeleteVideoComment(int commentId)
        {
            string accessToken = await GetNewToken();
            _httpClient.SetBearerToken(accessToken);
            
            string deleteCommentApiPath = "/api/comments/" + commentId;
            HttpResponseMessage newCommentResponse = await _httpClient.DeleteAsync(deleteCommentApiPath).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }
    }
}
