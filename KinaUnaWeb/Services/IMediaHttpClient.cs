using System.Collections.Generic;
using KinaUnaWeb.Models.ItemViewModels;
using System.Net.Http;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// The media http client.
    /// Contains the methods to retrieve, update, add and delete pictures, videos, and comments.
    /// </summary>
    public interface IMediaHttpClient
    {
        Task<HttpClient> GetClient();

        /// <summary>
        /// Gets the picture with the given PictureId, with the PictureTime converted to the given time zone.
        /// </summary>
        /// <param name="pictureId">int: The PictureId of the picture (Picture.PictureId).</param>
        /// <param name="timeZone">string: The time zone to use for PictureTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>Picture</returns>
        Task<Picture> GetPicture(int pictureId, string timeZone);

        /// <summary>
        /// Gets a random picture from the list of pictures a user has access to for a given progeny, with the PictureTime converted to the given time zone.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <param name="timeZone">string: The time zone to use for PictureTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>Picture</returns>
        Task<Picture> GetRandomPicture(int progenyId, int accessLevel, string timeZone);

        /// <summary>
        /// Gets a list of Pictures for a given progeny that a user has access to, with the PictureTime converted to the given time zone for each Picture.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <param name="timeZone">string: The time zone to use for PictureTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns></returns>
        Task<List<Picture>> GetPictureList(int progenyId, int accessLevel, string timeZone);

        Task<List<Picture>> GetAllPictures();

        /// <summary>
        /// Adds a new Picture.
        /// </summary>
        /// <param name="picture">Picture: The new Picture to add.</param>
        /// <returns>Picture</returns>
        Task<Picture> AddPicture(Picture picture);

        /// <summary>
        /// Updates a Picture. The Picture with the same PictureId will be updated.
        /// </summary>
        /// <param name="picture">Picture: The Picture to update.</param>
        /// <returns>Picture: The updated Picture object.</returns>
        Task<Picture> UpdatePicture(Picture picture);

        /// <summary>
        /// Removes the Picture with the given PictureId.
        /// </summary>
        /// <param name="pictureId">int: The PictureId of the Picture to remove (Picture.PictureId).</param>
        /// <returns>bool: True if the Picture was deleted successfully.</returns>
        Task<bool> DeletePicture(int pictureId);

        /// <summary>
        /// Adds a Comment for a Picture.
        /// </summary>
        /// <param name="comment">Comment: The Comment object to add.</param>
        /// <returns>bool: True if the Comment was successfully added.</returns>
        Task<bool> AddPictureComment(Comment comment);

        /// <summary>
        /// Removes the Picture Comment with the given CommentId.
        /// </summary>
        /// <param name="commentId">int: The CommentId of the Comment to remove (Comment.CommentId).</param>
        /// <returns>bool: True if the Comment was removed successfully.</returns>
        Task<bool> DeletePictureComment(int commentId);

        /// <summary>
        /// Gets a PicturePageViewModel for a progeny that a user has access to.
        /// </summary>
        /// <param name="pageSize">int: The number of Pictures per page.</param>
        /// <param name="id">int: The current page number.</param>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="userAccessLevel">int: The user's access level.</param>
        /// <param name="sortBy">int: 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="tagFilter">string: Only include Pictures tagged with this string. If null or empty include all Pictures.</param>
        /// <param name="timeZone">string: The time zone to use for PictureTime (TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>PicturePageViewModel</returns>
        Task<PicturePageViewModel> GetPicturePage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy,
            string tagFilter, string timeZone);

        /// <summary>
        /// Gets a PictureViewModel for the Picture with a given PictureId.
        /// </summary>
        /// <param name="id">int: The PictureId for the Picture to get (Picture.PictureId).</param>
        /// <param name="userAccessLevel">int: The user's access level.</param>
        /// <param name="sortBy">int: 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="timeZone">string: The time zone to use for PictureTime and Comment's time (TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns></returns>
        Task<PictureViewModel> GetPictureViewModel(int id, int userAccessLevel, int sortBy, string timeZone);

        /// <summary>
        /// Gets the video with the given VideoId, with the VideoTime converted to the given time zone.
        /// </summary>
        /// <param name="videoId">int: The VideoId of the video (Video.VideoId).</param>
        /// <param name="timeZone">string: The time zone to use for VideoTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>Video</returns>
        Task<Video> GetVideo(int videoId, string timeZone);

        /// <summary>
        /// Gets a list of Videos for a given progeny that a user has access to, with the VideoTime converted to the given time zone for each video.
        /// </summary>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id)</param>
        /// <param name="accessLevel">int: The user's access level.</param>
        /// <param name="timeZone">string: The time zone to use for VideoTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>List of Video objects.</returns>
        Task<List<Video>> GetVideoList(int progenyId, int accessLevel, string timeZone);
        Task<List<Video>> GetAllVideos();

        /// <summary>
        /// Adds a new Video.
        /// </summary>
        /// <param name="video">Video: The new Video to add.</param>
        /// <returns>Video</returns>
        Task<Video> AddVideo(Video video);

        /// <summary>
        /// Updates a Video. The Video with the same VideoId will be updated.
        /// </summary>
        /// <param name="video">Video: The Video to update.</param>
        /// <returns>Video: The updated Video object.</returns>
        Task<Video> UpdateVideo(Video video);

        /// <summary>
        /// Removes the Video with the given VideoId.
        /// </summary>
        /// <param name="videoId">int: The VideoId of the Video to remove (Video.VideoId).</param>
        /// <returns>bool: True if the Video was successfully removed.</returns>
        Task<bool> DeleteVideo(int videoId);

        /// <summary>
        /// Adds a comment for a Video.
        /// </summary>
        /// <param name="comment">Comment: The Comment object to add.</param>
        /// <returns>bool: True if the comment was successfully added.</returns>
        Task<bool> AddVideoComment(Comment comment);

        /// <summary>
        /// Removes a Video Comment with the given CommentId.
        /// </summary>
        /// <param name="commentId">int: The CommentId of the Comment to remove (Comment.CommentId).</param>
        /// <returns>bool: True if the Comment was successfully removed.</returns>
        Task<bool> DeleteVideoComment(int commentId);

        /// <summary>
        /// Gets a VideoPageViewModel for a progeny that a user has access to.
        /// </summary>
        /// <param name="pageSize">int: The number of Videos per page.</param>
        /// <param name="id">int: The current page number.</param>
        /// <param name="progenyId">int: The Id of the progeny (Progeny.Id).</param>
        /// <param name="userAccessLevel">int: The user's access level.</param>
        /// <param name="sortBy">int: 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="tagFilter">string: Only include Videos tagged with this string. If null or empty include all Pictures.</param>
        /// <param name="timeZone">string: The time zone to use for VideoTime (TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>VideoPageViewModel</returns>
        Task<VideoPageViewModel> GetVideoPage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy,
            string tagFilter, string timeZone);

        /// <summary>
        /// Gets a VideoViewModel for the Video with a given VideoId.
        /// </summary>
        /// <param name="id">int: The VideoId for the Video to get (Video.VideoId).</param>
        /// <param name="userAccessLevel">int: The user's access level.</param>
        /// <param name="sortBy">int: 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="timeZone">string: The time zone to use for VideoTime and Comment's time (TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns></returns>
        Task<VideoViewModel> GetVideoViewModel(int id, int userAccessLevel, int sortBy, string timeZone);
    }
}
