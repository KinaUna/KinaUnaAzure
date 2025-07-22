using IdentityModel.Client;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using KinaUnaWeb.Models.ItemViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to retrieve, update, add and delete pictures, videos, and comments.
    /// </summary>
    // Todo: Refactor into PictureHttpClient, VideoHttpClient, CommentsHttpClient.
    public class MediaHttpClient : IMediaHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MediaHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient, IHttpContextAccessor httpContextAccessor, IHostEnvironment env)
        {
            _apiTokenClient = apiTokenClient;
            _httpContextAccessor = httpContextAccessor;
            string clientUri = configuration.GetValue<string>("ProgenyApiServer");
            if (env.IsDevelopment())
            {
                clientUri = configuration.GetValue<string>("ProgenyApiServerLocal");
            }
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets the picture with the given PictureId, with the PictureTime converted to the given time zone.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to get.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>Picture object with the given PictureId. Picture object with PictureId = 0 if the Picture isn't found.</returns>
        public async Task<Picture> GetPicture(int pictureId, string timeZone)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pictureApiPath = "/api/Pictures/" + pictureId;

            HttpResponseMessage pictureResponse = await _httpClient.GetAsync(pictureApiPath);
            if (!pictureResponse.IsSuccessStatusCode) return new Picture();

            string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
            Picture picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
            if (picture != null && picture.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
            {
                picture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }

            return picture ?? new Picture();
        }

        /// <summary>
        /// Gets a random picture from the list of pictures a user has access to for a given progeny, with the PictureTime converted to the given time zone.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny that the Picture belongs to.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>Picture object.</returns>
        public async Task<Picture> GetRandomPicture(int progenyId, string timeZone)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pictureApiPath = "api/Pictures/Random/" + progenyId;
            HttpResponseMessage pictureResponse = await _httpClient.GetAsync(pictureApiPath);
            if (!pictureResponse.IsSuccessStatusCode) return new Picture();

            string pictureResponseString = await pictureResponse.Content.ReadAsStringAsync();
            Picture resultPicture = JsonConvert.DeserializeObject<Picture>(pictureResponseString);
            if (timeZone == "" || resultPicture == null) return resultPicture;

            if (resultPicture.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
            {
                resultPicture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(resultPicture.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }

            return resultPicture;

        }

        /// <summary>
        /// Gets a list of all Pictures for a given progeny that a user has access to, with the PictureTime converted to the given time zone for each Picture.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>List of Picture objects.</returns>
        public async Task<List<Picture>> GetPictureList(int progenyId, string timeZone)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pictureApiPath = "/api/Pictures/Progeny/" + progenyId;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pictureApiPath);
            if (!picturesResponse.IsSuccessStatusCode) return [];

            string picturesListAsString = await picturesResponse.Content.ReadAsStringAsync();
            List<Picture> resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(picturesListAsString);
            if (timeZone == "" || resultPictureList == null) return resultPictureList ?? [];

            foreach (Picture pic in resultPictureList)
            {
                if (pic.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
                {
                    pic.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pic.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }
            }

            return resultPictureList;
        }

        /// <summary>
        /// Gets a list of all Pictures for a given progeny that a user has access to.
        /// Time zone for PictureTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Pictures for.</param>
        /// <returns>List of Picture objects.</returns>
        public async Task<List<Picture>> GetProgenyPictureList(int progenyId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pictureApiPath = "/api/Pictures/ProgenyPicturesList/" + progenyId;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pictureApiPath);
            if (!picturesResponse.IsSuccessStatusCode) return [];

            string picturesListAsString = await picturesResponse.Content.ReadAsStringAsync();
            List<Picture> resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(picturesListAsString);
            
            return resultPictureList;
        }

        /// <summary>
        /// Adds a new Picture.
        /// </summary>
        /// <param name="picture">The new Picture object to add.</param>
        /// <returns>The added Picture.</returns>
        public async Task<Picture> AddPicture(Picture picture)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string newPictureApiPath = "/api/Pictures/";

            HttpResponseMessage pictureResponse = await _httpClient.PostAsync(newPictureApiPath, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json"));
            if (!pictureResponse.IsSuccessStatusCode) return new Picture();

            string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
            picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
            if (picture == null) return new Picture();

            string newPictureUri = "/api/Pictures/ByLink/" + picture.PictureLink;
            HttpResponseMessage newPictureResponse = await _httpClient.GetAsync(newPictureUri);
            if (!newPictureResponse.IsSuccessStatusCode) return new Picture();

            string newPictureAsString = await newPictureResponse.Content.ReadAsStringAsync();
            Picture newPicture = JsonConvert.DeserializeObject<Picture>(newPictureAsString);
            return newPicture ?? new Picture();

        }

        /// <summary>
        /// Updates a Picture. The Picture with the same PictureId will be updated.
        /// </summary>
        /// <param name="picture">The Picture object with the updated properties.</param>
        /// <returns>The updated Picture object.</returns>
        public async Task<Picture> UpdatePicture(Picture picture)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string updatePictureApiPath = "/api/Pictures/" + picture.PictureId;

            HttpResponseMessage pictureResponse = await _httpClient.PutAsync(updatePictureApiPath, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json"));
            if (!pictureResponse.IsSuccessStatusCode) return new Picture();

            string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
            picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
            return picture ?? new Picture();
        }

        /// <summary>
        /// Removes the Picture with the given PictureId.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to remove.</param>
        /// <returns>bool: True if the Picture was deleted successfully.</returns>
        public async Task<bool> DeletePicture(int pictureId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string deletePictureApiPath = "/api/Pictures/" + pictureId;

            HttpResponseMessage deletePictureResponse = await _httpClient.DeleteAsync(deletePictureApiPath);
            return deletePictureResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Adds a Comment for a Picture.
        /// </summary>
        /// <param name="comment">The Comment object to add.</param>
        /// <returns>bool: True if the Comment was successfully added.</returns>
        public async Task<bool> AddPictureComment(Comment comment)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string newCommentApiPath = "/api/Comments/";

            HttpResponseMessage newCommentResponse = await _httpClient.PostAsync(newCommentApiPath, new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            return newCommentResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Removes the Picture Comment with the given CommentId.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to remove.</param>
        /// <returns>bool: True if the Comment was removed successfully.</returns>
        public async Task<bool> DeletePictureComment(int commentId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string deleteCommentApiPath = "/api/Comments/" + commentId;

            HttpResponseMessage newCommentResponse = await _httpClient.DeleteAsync(deleteCommentApiPath).ConfigureAwait(false);
            return newCommentResponse.IsSuccessStatusCode;
        }
        
        /// <summary>
        /// Gets a PictureViewModel for the Picture with a given PictureId.
        /// PictureTime and Comment's time will be converted to the given time zone.
        /// </summary>
        /// <param name="request">PictureViewModelRequest object with PictureId, SortOrder, TimeZone, and TagFilter.</param>
        /// <returns>PictureVieModel.</returns>
        public async Task<PictureViewModel> GetPictureViewModel(PictureViewModelRequest request)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string pageApiPath = "/api/Pictures/PictureViewModel/";
            HttpResponseMessage picturesResponse = await _httpClient.PostAsync(pageApiPath, new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            if (!picturesResponse.IsSuccessStatusCode) return new PictureViewModel();

            string picturesViewModelAsString = await picturesResponse.Content.ReadAsStringAsync();
            PictureViewModel pictureViewModel = JsonConvert.DeserializeObject<PictureViewModel>(picturesViewModelAsString);
            if (request.TimeZone == "" || pictureViewModel == null) return pictureViewModel ?? new PictureViewModel();

            if (pictureViewModel.PictureTime.HasValue && !string.IsNullOrEmpty(request.TimeZone))
            {
                pictureViewModel.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pictureViewModel.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone));
            }

            if (pictureViewModel.CommentsList.Count <= 0 || string.IsNullOrEmpty(request.TimeZone)) return pictureViewModel;

            foreach (Comment cmnt in pictureViewModel.CommentsList)
            {
                cmnt.Created = TimeZoneInfo.ConvertTimeFromUtc(cmnt.Created,
                    TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone));
            }

            return pictureViewModel;
        }

        /// <summary>
        /// Gets a simplified PictureViewModel for a Picture entity with the provided PictureId.
        /// PictureNumber, PictureCount, CommentsList, and TagsList are not included. Time zone for PictureTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to get a PictureViewModel for.</param>
        /// <returns>PictureViewModel.</returns>
        public async Task<PictureViewModel> GetPictureElement(int pictureId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pageApiPath = "/api/Pictures/PictureElement/" + pictureId;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pageApiPath);
            if (!picturesResponse.IsSuccessStatusCode) return new PictureViewModel();

            string picturesViewModelAsString = await picturesResponse.Content.ReadAsStringAsync();
            PictureViewModel pictureViewModel = JsonConvert.DeserializeObject<PictureViewModel>(picturesViewModelAsString);
            
            return pictureViewModel;
        }

        public async Task<PicturesLocationsResponse> GetPictureLocations(PicturesLocationsRequest picturesLocationsRequest)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string picturesApiPath = "/api/Pictures/GetPictureLocations/";
            HttpResponseMessage picturesResponse = await _httpClient.PostAsync(picturesApiPath, new StringContent(JsonConvert.SerializeObject(picturesLocationsRequest), Encoding.UTF8, "application/json"));
            if (!picturesResponse.IsSuccessStatusCode) return new PicturesLocationsResponse();

            string picturesLocationsResponseAsString = await picturesResponse.Content.ReadAsStringAsync();
            PicturesLocationsResponse resultPicturesLocationResponse = JsonConvert.DeserializeObject<PicturesLocationsResponse>(picturesLocationsResponseAsString);
            return resultPicturesLocationResponse;
        }

        public async Task<NearByPhotosResponse> GetPicturesNearLocation(NearByPhotosRequest nearByPhotosRequest)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string locationApiPath = "/api/Pictures/GetPicturesNearLocation/";
            HttpResponseMessage picturesResponse = await _httpClient.PostAsync(locationApiPath, new StringContent(JsonConvert.SerializeObject(nearByPhotosRequest), Encoding.UTF8, "application/json"));
            if (!picturesResponse.IsSuccessStatusCode) return new NearByPhotosResponse();

            string nearByPhotosResponseAsString = await picturesResponse.Content.ReadAsStringAsync();
            NearByPhotosResponse resultNearbyPhotosResponse = JsonConvert.DeserializeObject<NearByPhotosResponse>(nearByPhotosResponseAsString);
            return resultNearbyPhotosResponse;
        }

        /// <summary>
        /// Gets a VideoPageViewModel for a progeny that a user has access to.
        /// </summary>
        /// <param name="pageSize">The number of Videos per page.</param>
        /// <param name="id">The current page number.</param>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="sortBy">Sort order. 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="tagFilter">Only include Videos tagged with this string. If null or empty include all Pictures.</param>
        /// <param name="timeZone">The time zone to use for VideoTime.</param>
        /// <returns>VideoPageViewModel</returns>
        public async Task<VideoPageViewModel> GetVideoPage(int pageSize, int id, int progenyId, int sortBy, string tagFilter, string timeZone)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pageApiPath = "/api/Videos/Page?pageSize=" + pageSize + "&pageIndex=" + id + "&progenyId=" + progenyId + "&sortBy=" + sortBy;
            if (tagFilter != "")
            {
                pageApiPath = pageApiPath + "&tagFilter=" + tagFilter;
            }

            pageApiPath = pageApiPath + "&timeZone=" + timeZone;

            HttpResponseMessage videoResponse = await _httpClient.GetAsync(pageApiPath);
            if (!videoResponse.IsSuccessStatusCode) return new VideoPageViewModel();

            string videoPageAsString = await videoResponse.Content.ReadAsStringAsync();
            VideoPageViewModel model = JsonConvert.DeserializeObject<VideoPageViewModel>(videoPageAsString);

            if (model == null || string.IsNullOrEmpty(timeZone) || model.VideosList.Count == 0) return model ?? new VideoPageViewModel();

            foreach (Video vid in model.VideosList)
            {

                if (vid.VideoTime.HasValue)
                {
                    vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }
            }

            return model;
        }

        /// <summary>
        /// Gets a VideoViewModel for the Video with a given VideoId.
        /// </summary>
        /// <param name="request">VideoViewModelRequest object with VideoId, TimeZone, progenies, sort order.</param>
        /// <returns>VideoViewModel</returns>
        public async Task<VideoViewModel> GetVideoViewModel(VideoViewModelRequest request)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pageApiPath = "/api/Videos/VideoViewModel";

            HttpResponseMessage videoViewModelResponse = await _httpClient.PostAsync(pageApiPath, new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json"));
            if (!videoViewModelResponse.IsSuccessStatusCode) return new VideoViewModel();

            string videoViewModelAsString = await videoViewModelResponse.Content.ReadAsStringAsync();
            VideoViewModel videoViewModel = JsonConvert.DeserializeObject<VideoViewModel>(videoViewModelAsString);

            if (videoViewModel == null || string.IsNullOrEmpty(request.TimeZone)) return videoViewModel ?? new VideoViewModel();

            if (videoViewModel.VideoTime.HasValue)
            {
                videoViewModel.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(videoViewModel.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone));
            }

            if (videoViewModel.CommentsList.Count <= 0) return videoViewModel;

            foreach (Comment cmnt in videoViewModel.CommentsList)
            {
                cmnt.Created = TimeZoneInfo.ConvertTimeFromUtc(cmnt.Created, TimeZoneInfo.FindSystemTimeZoneById(request.TimeZone));
            }

            return videoViewModel;
        }

        /// <summary>
        /// Gets the video with the given VideoId, with the VideoTime converted to the given time zone.
        /// </summary>
        /// <param name="videoId">The VideoId of the video to get.</param>
        /// <param name="timeZone">The time zone to use for VideoTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>Video object with the given VideoId. Video object with VideoId = 0 if the Video isn't found.</returns>
        public async Task<Video> GetVideo(int videoId, string timeZone)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string videoApiPath = "/api/Videos/" + videoId;

            HttpResponseMessage videoResponse = await _httpClient.GetAsync(videoApiPath);
            if (!videoResponse.IsSuccessStatusCode) return new Video();

            string videoAsString = await videoResponse.Content.ReadAsStringAsync();
            Video resultVideo = JsonConvert.DeserializeObject<Video>(videoAsString);
            if (resultVideo == null) return new Video();

            if (string.IsNullOrEmpty(timeZone)) return resultVideo;

            if (resultVideo.VideoTime.HasValue)
            {
                resultVideo.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(resultVideo.VideoTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            }

            return resultVideo;

        }

        /// <summary>
        /// Gets a list of Videos for a given progeny that a user has access to, with the VideoTime converted to the given time zone for each video.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny</param>
        /// <param name="timeZone">The time zone to use for VideoTime.</param>
        /// <returns>List of Video objects.</returns>
        public async Task<List<Video>> GetVideoList(int progenyId, string timeZone)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string videoApiPath = "/api/Videos/Progeny/" + progenyId;
            HttpResponseMessage videosListReponse = await _httpClient.GetAsync(videoApiPath);
            if (!videosListReponse.IsSuccessStatusCode) return [];

            string videoListAsString = await videosListReponse.Content.ReadAsStringAsync();
            List<Video> resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoListAsString);
            if (resultVideoList == null || string.IsNullOrEmpty(timeZone)) return resultVideoList ?? [];

            foreach (Video vid in resultVideoList)
            {
                if (vid.VideoTime.HasValue)
                {
                    vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }
            }

            return resultVideoList;
        }

        /// <summary>
        /// Gets a list of all Videos for a given progeny that a user has access to.
        /// Time zone for VideoTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get all Videos for.</param>
        /// <returns>List of Video objects.</returns>
        public async Task<List<Video>> GetProgenyVideoList(int progenyId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string videoApiPath = "/api/Videos/ProgenyVideosList/" + progenyId;
            HttpResponseMessage videosResponse = await _httpClient.GetAsync(videoApiPath);
            if (!videosResponse.IsSuccessStatusCode) return [];

            string videosAsListAsString = await videosResponse.Content.ReadAsStringAsync();
            List<Video> resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videosAsListAsString);

            return resultVideoList;
        }

        /// <summary>
        /// Adds a new Video.
        /// </summary>
        /// <param name="video">The new Video object to add.</param>
        /// <returns>The added Video object.</returns>
        public async Task<Video> AddVideo(Video video)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string newVideoApiPath = "/api/Videos/";

            HttpResponseMessage newVideoResponse = await _httpClient.PostAsync(newVideoApiPath, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            if (!newVideoResponse.IsSuccessStatusCode) return new Video();

            string videoAsString = await newVideoResponse.Content.ReadAsStringAsync();
            video = JsonConvert.DeserializeObject<Video>(videoAsString);
            return video ?? new Video();
        }

        /// <summary>
        /// Updates a Video. The Video with the same VideoId will be updated.
        /// </summary>
        /// <param name="video">The Video with the updated properties.</param>
        /// <returns>The updated Video object.</returns>
        public async Task<Video> UpdateVideo(Video video)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string updateVideoApiPath = "/api/Videos/" + video.VideoId;
            HttpResponseMessage videoResponse = await _httpClient.PutAsync(updateVideoApiPath, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            if (!videoResponse.IsSuccessStatusCode) return new Video();

            string videoAsString = await videoResponse.Content.ReadAsStringAsync();
            video = JsonConvert.DeserializeObject<Video>(videoAsString);
            return video ?? new Video();
        }

        /// <summary>
        /// Removes the Video with the given VideoId.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video to remove.</param>
        /// <returns>bool: True if the Video was successfully removed.</returns>
        public async Task<bool> DeleteVideo(int videoId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string deleteVideoApiPath = "/api/videos/" + videoId;

            HttpResponseMessage deleteVideoResponse = await _httpClient.DeleteAsync(deleteVideoApiPath).ConfigureAwait(false);
            return deleteVideoResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Adds a comment for a Video.
        /// </summary>
        /// <param name="comment">The Comment object to add.</param>
        /// <returns>bool: True if the comment was successfully added.</returns>
        public async Task<bool> AddVideoComment(Comment comment)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            const string newCommentApiPath = "/api/comments/";
            HttpResponseMessage newCommentResponse = await _httpClient.PostAsync(newCommentApiPath, new StringContent(JsonConvert.SerializeObject(comment), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            return newCommentResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Removes a Video Comment with the given CommentId.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to remove.</param>
        /// <returns>bool: True if the Comment was successfully removed.</returns>
        public async Task<bool> DeleteVideoComment(int commentId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string deleteCommentApiPath = "/api/comments/" + commentId;
            HttpResponseMessage newCommentResponse = await _httpClient.DeleteAsync(deleteCommentApiPath).ConfigureAwait(false);
            return newCommentResponse.IsSuccessStatusCode;
        }

        /// <summary>
        /// Gets a simplified VideoViewModel for a Video entity with the provided VideoId.
        /// VideoNumber, VideoCount, CommentsList, and TagsList are not included. Time zone for VideoTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video to get a VideoViewModel for.</param>
        /// <returns>VideoViewModel.</returns>
        public async Task<VideoViewModel> GetVideoElement(int videoId)
        {
            bool isAuthenticated = _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken(!isAuthenticated);
            _httpClient.SetBearerToken(accessToken);

            string pageApiPath = "/api/Videos/VideoElement/" + videoId;
            HttpResponseMessage videosResponse = await _httpClient.GetAsync(pageApiPath);
            if (!videosResponse.IsSuccessStatusCode) return new VideoViewModel();

            string videosViewModelAsString = await videosResponse.Content.ReadAsStringAsync();
            VideoViewModel videoViewModel = JsonConvert.DeserializeObject<VideoViewModel>(videosViewModelAsString);

            return videoViewModel;
        }
    }
}
