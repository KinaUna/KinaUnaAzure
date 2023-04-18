using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using IdentityModel.Client;
using KinaUna.Data.Models;
using KinaUnaWeb.Models.ItemViewModels;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace KinaUnaWeb.Services
{
    public class MediaHttpClient: IMediaHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly ApiTokenInMemoryClient _apiTokenClient;

        public MediaHttpClient(HttpClient httpClient, IConfiguration configuration, ApiTokenInMemoryClient apiTokenClient)
        {
            _apiTokenClient = apiTokenClient;
            string clientUri = configuration.GetValue<string>("MediaApiServer");
            httpClient.BaseAddress = new Uri(clientUri!);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestVersion = new Version(2, 0);
            _httpClient = httpClient;
        }

        

        public async Task<Picture> GetPicture(int pictureId, string timeZone)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "/api/Pictures/" + pictureId;
            
            HttpResponseMessage pictureResponse = await _httpClient.GetAsync(pictureApiPath);
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
                Picture picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
                if (picture != null && picture.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
                {
                    picture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(picture.PictureTime.Value,
                        TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                }

                if (picture != null)
                {
                    return picture;
                }
            }
            
            return new Picture();
        }

        public async Task<Picture> GetRandomPicture(int progenyId, int accessLevel, string timeZone)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "api/Pictures/Random/" + progenyId + "/" + accessLevel;
            HttpResponseMessage pictureResponse = await _httpClient.GetAsync(pictureApiPath);
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureResponseString = await pictureResponse.Content.ReadAsStringAsync();
                Picture resultPicture = JsonConvert.DeserializeObject<Picture>(pictureResponseString);
                if (timeZone != "" && resultPicture != null)
                {
                    if (resultPicture.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
                    {
                        resultPicture.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(resultPicture.PictureTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }
                }

                return resultPicture;
            }

            return new Picture();
        }

        public async Task<List<Picture>> GetPictureList(int progenyId, int accessLevel, string timeZone)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "/api/Pictures/Progeny/" + progenyId + "/" + accessLevel;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pictureApiPath);
            if (picturesResponse.IsSuccessStatusCode)
            {
                string picturesListAsString = await picturesResponse.Content.ReadAsStringAsync();
                List<Picture> resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(picturesListAsString);
                if (timeZone != "" && resultPictureList != null)
                {
                    foreach (Picture pic in resultPictureList)
                    {
                        if (pic.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
                        {
                            pic.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pic.PictureTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                if (resultPictureList != null)
                {
                    return resultPictureList;
                }
            }

            return new List<Picture>();
        }

        public async Task<List<Picture>> GetAllPictures()
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pictureApiPath = "/api/Pictures";
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pictureApiPath);
            if (picturesResponse.IsSuccessStatusCode)
            {
                string pictureResponseString = await picturesResponse.Content.ReadAsStringAsync();

                List<Picture> resultPictureList = JsonConvert.DeserializeObject<List<Picture>>(pictureResponseString);

                if (resultPictureList != null)
                {
                    return resultPictureList;
                }
            }

            return new List<Picture>();
        }

        public async Task<Picture> AddPicture(Picture picture)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newPictureApiPath = "/api/Pictures/";
            
            HttpResponseMessage pictureResponse = await _httpClient.PostAsync(newPictureApiPath, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json"));
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
                picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
                if (picture != null)
                {
                    string newPictureUri = "/api/Pictures/ByLink/" + picture.PictureLink;
                    HttpResponseMessage newPictureResponse = await _httpClient.GetAsync(newPictureUri);
                    if (newPictureResponse.IsSuccessStatusCode)
                    {
                        string newPictureAsString = await newPictureResponse.Content.ReadAsStringAsync();
                        Picture newPicture = JsonConvert.DeserializeObject<Picture>(newPictureAsString);
                        if (newPicture != null)
                        {
                            return newPicture;
                        }
                    }
                }
            }

            return new Picture();
        }

        public async Task<Picture> UpdatePicture(Picture picture)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updatePictureApiPath = "/api/Pictures/" + picture.PictureId;

            HttpResponseMessage pictureResponse = await _httpClient.PutAsync(updatePictureApiPath, new StringContent(JsonConvert.SerializeObject(picture), Encoding.UTF8, "application/json"));
            if (pictureResponse.IsSuccessStatusCode)
            {
                string pictureAsString = await pictureResponse.Content.ReadAsStringAsync();
                picture = JsonConvert.DeserializeObject<Picture>(pictureAsString);
                if (picture != null)
                {
                    return picture;
                }
            }

            return new Picture();
        }

        public async Task<bool> DeletePicture(int pictureId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string deleteCommentApiPath = "/api/Comments/" + commentId;
            
            HttpResponseMessage newCommentResponse = await _httpClient.DeleteAsync(deleteCommentApiPath).ConfigureAwait(false);
            if (newCommentResponse.IsSuccessStatusCode)
            {
                return true;
            }

            return false;
        }

        public async Task<PicturePageViewModel> GetPicturePage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy, string tagFilter, string timeZone)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Pictures/Page?pageSize=" + pageSize + "&pageIndex=" + id + "&progenyId=" + progenyId + "&accessLevel=" + userAccessLevel + "&sortBy=" + sortBy;
            if (tagFilter != "")
            {
                pageApiPath = pageApiPath + "&tagFilter=" + tagFilter;
            }
            
            HttpResponseMessage picturePageResponse = await _httpClient.GetAsync(pageApiPath);
            if (picturePageResponse.IsSuccessStatusCode)
            {
                string pageResponseString = await picturePageResponse.Content.ReadAsStringAsync();

                PicturePageViewModel model = JsonConvert.DeserializeObject<PicturePageViewModel>(pageResponseString);
                if (timeZone != "" && model != null && model.PicturesList.Any())
                {
                    foreach (Picture pic in model.PicturesList)
                    {
                        if (pic.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
                        {
                            pic.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pic.PictureTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                if (model != null)
                {
                    return model;
                }
            }

            return new PicturePageViewModel();
        }

        public async Task<PictureViewModel> GetPictureViewModel(int id, int userAccessLevel, int sortBy, string timeZone, string tagFilter = "")
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Pictures/PictureViewModel/" + id + "/" + userAccessLevel + "?sortBy=" + sortBy + "&tagFilter=" + tagFilter;
            HttpResponseMessage picturesResponse = await _httpClient.GetAsync(pageApiPath);
            if (picturesResponse.IsSuccessStatusCode)
            {
                string picturesViewModelAsString = await picturesResponse.Content.ReadAsStringAsync();
                PictureViewModel pictureViewModel = JsonConvert.DeserializeObject<PictureViewModel>(picturesViewModelAsString);
                if (timeZone != "" && pictureViewModel != null)
                {
                    if (pictureViewModel.PictureTime.HasValue && !string.IsNullOrEmpty(timeZone))
                    {
                        pictureViewModel.PictureTime = TimeZoneInfo.ConvertTimeFromUtc(pictureViewModel.PictureTime.Value,
                            TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }

                    if (pictureViewModel.CommentsList.Count > 0 && !string.IsNullOrEmpty(timeZone))
                    {
                        foreach (Comment cmnt in pictureViewModel.CommentsList)
                        {
                            cmnt.Created = TimeZoneInfo.ConvertTimeFromUtc(cmnt.Created,
                                TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                if (pictureViewModel != null)
                {
                    return pictureViewModel;
                }
            }

            return new PictureViewModel();
        }

        public async Task<VideoPageViewModel> GetVideoPage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy, string tagFilter, string timeZone)
        {

            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
                VideoPageViewModel model = JsonConvert.DeserializeObject<VideoPageViewModel>(videoPageAsString);

                if (model != null && !string.IsNullOrEmpty(timeZone) && model.VideosList.Any())
                {
                    foreach (Video vid in model.VideosList)
                    {

                        if (vid.VideoTime.HasValue)
                        {
                            vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                if (model != null)
                {
                    return model;
                }
            }

            return new VideoPageViewModel();
        }

        public async Task<VideoViewModel> GetVideoViewModel(int id, int userAccessLevel, int sortBy, string timeZone, string tagFilter = "")
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string pageApiPath = "/api/Videos/VideoViewModel/" + id + "/" + userAccessLevel + "?sortBy=" + sortBy + "&tagFilter=" + tagFilter;
            
            HttpResponseMessage videoViewModelResponse = await _httpClient.GetAsync(pageApiPath);
            if (videoViewModelResponse.IsSuccessStatusCode)
            {
                string videoViewModelAsString = await videoViewModelResponse.Content.ReadAsStringAsync();
                VideoViewModel videoViewModel = JsonConvert.DeserializeObject<VideoViewModel>(videoViewModelAsString);

                if (videoViewModel != null && !string.IsNullOrEmpty(timeZone))
                {
                    if (videoViewModel.VideoTime.HasValue)
                    {
                        videoViewModel.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(videoViewModel.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                    }

                    if (videoViewModel.CommentsList.Count > 0)
                    {
                        foreach (Comment cmnt in videoViewModel.CommentsList)
                        {
                            cmnt.Created = TimeZoneInfo.ConvertTimeFromUtc(cmnt.Created, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                if (videoViewModel != null)
                {
                    return videoViewModel;
                }
            }

            return new VideoViewModel();
        }

        public async Task<Video> GetVideo(int videoId, string timeZone)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);

            string videoApiPath = "/api/Videos/" + videoId;

            HttpResponseMessage videoResponse = await _httpClient.GetAsync(videoApiPath);
            if (videoResponse.IsSuccessStatusCode)
            {
                string videoAsString = await videoResponse.Content.ReadAsStringAsync();
                Video resultVideo = JsonConvert.DeserializeObject<Video>(videoAsString);
                if (resultVideo != null)
                {
                    if (!string.IsNullOrEmpty(timeZone))
                    {
                        if (resultVideo.VideoTime.HasValue)
                        {
                            resultVideo.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(resultVideo.VideoTime.Value,
                                TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }

                    return resultVideo;
                }
            }

            return new Video();
        }

        public async Task<List<Video>> GetVideoList(int progenyId, int accessLevel, string timeZone)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string videoApiPath = "/api/Videos/Progeny/" + progenyId + "/" + accessLevel;
            HttpResponseMessage videosListReponse = await _httpClient.GetAsync(videoApiPath);
            if (videosListReponse.IsSuccessStatusCode)
            {
                string videoListAsString = await videosListReponse.Content.ReadAsStringAsync();
                List<Video> resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoListAsString);
                if (resultVideoList != null && !string.IsNullOrEmpty(timeZone))
                {
                    foreach (Video vid in resultVideoList)
                    {
                        if (vid.VideoTime.HasValue)
                        {
                            vid.VideoTime = TimeZoneInfo.ConvertTimeFromUtc(vid.VideoTime.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
                        }
                    }
                }

                if (resultVideoList != null)
                {
                    return resultVideoList;
                }
            }

            return new List<Video>();
        }

        public async Task<List<Video>> GetAllVideos()
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string videoApiPath = "/api/Videos";
            HttpResponseMessage videosResponse = await _httpClient.GetAsync(videoApiPath);
            if (videosResponse.IsSuccessStatusCode)
            {
                string videoResponseString = await videosResponse.Content.ReadAsStringAsync();

                List<Video> resultVideoList = JsonConvert.DeserializeObject<List<Video>>(videoResponseString);
                if (resultVideoList != null)
                {
                    return resultVideoList;
                }
            }

            return new List<Video>();
        }
        public async Task<Video> AddVideo(Video video)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string newVideoApiPath = "/api/Videos/";
            
            HttpResponseMessage newVideoResponse = await _httpClient.PostAsync(newVideoApiPath, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            if (newVideoResponse.IsSuccessStatusCode)
            {
                string videoAsString = await newVideoResponse.Content.ReadAsStringAsync();
                video = JsonConvert.DeserializeObject<Video>(videoAsString);
                if (video != null)
                {
                    return video;
                }
            }

            return new Video();
        }

        public async Task<Video> UpdateVideo(Video video)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
            _httpClient.SetBearerToken(accessToken);
            
            string updateVideoApiPath = "/api/Videos/" + video.VideoId;
            HttpResponseMessage videoResponse = await _httpClient.PutAsync(updateVideoApiPath, new StringContent(JsonConvert.SerializeObject(video), Encoding.UTF8, "application/json"));
            if (videoResponse.IsSuccessStatusCode)
            {
                string videoAsString = await videoResponse.Content.ReadAsStringAsync();
                video = JsonConvert.DeserializeObject<Video>(videoAsString);
                if (video != null)
                {
                    return video;
                }
            }

            return new Video();
        }

        public async Task<bool> DeleteVideo(int videoId)
        {
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
            string accessToken = await _apiTokenClient.GetProgenyAndMediaApiToken();
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
