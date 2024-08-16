using System.Collections.Generic;
using KinaUnaWeb.Models.ItemViewModels;
using System.Threading.Tasks;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Services.HttpClients
{
    /// <summary>
    /// Provides methods to retrieve, update, add and delete pictures, videos, and comments.
    /// </summary>
    public interface IMediaHttpClient
    {
        /// <summary>
        /// Gets the picture with the given PictureId, with the PictureTime converted to the given time zone.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to get.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>Picture object with the given PictureId. Picture object with PictureId = 0 if the Picture isn't found.</returns>
        Task<Picture> GetPicture(int pictureId, string timeZone);

        /// <summary>
        /// Gets a random picture from the list of pictures a user has access to for a given progeny, with the PictureTime converted to the given time zone.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny that the Picture belongs to.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>Picture object.</returns>
        Task<Picture> GetRandomPicture(int progenyId, int accessLevel, string timeZone);

        /// <summary>
        /// Gets a list of all Pictures for a given progeny that a user has access to, with the PictureTime converted to the given time zone for each Picture.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>List of Picture objects.</returns>
        Task<List<Picture>> GetPictureList(int progenyId, int accessLevel, string timeZone);

        /// <summary>
        /// Adds a new Picture.
        /// </summary>
        /// <param name="picture">The new Picture object to add.</param>
        /// <returns>The added Picture.</returns>
        Task<Picture> AddPicture(Picture picture);

        /// <summary>
        /// Updates a Picture. The Picture with the same PictureId will be updated.
        /// </summary>
        /// <param name="picture">The Picture object with the updated properties.</param>
        /// <returns>The updated Picture object.</returns>
        Task<Picture> UpdatePicture(Picture picture);

        /// <summary>
        /// Removes the Picture with the given PictureId.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to remove.</param>
        /// <returns>bool: True if the Picture was deleted successfully.</returns>
        Task<bool> DeletePicture(int pictureId);

        /// <summary>
        /// Adds a Comment for a Picture.
        /// </summary>
        /// <param name="comment">The Comment object to add.</param>
        /// <returns>bool: True if the Comment was successfully added.</returns>
        Task<bool> AddPictureComment(Comment comment);

        /// <summary>
        /// Removes the Picture Comment with the given CommentId.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to remove.</param>
        /// <returns>bool: True if the Comment was removed successfully.</returns>
        Task<bool> DeletePictureComment(int commentId);

        /// <summary>
        /// Gets a PicturePageViewModel for a progeny that a user has access to.
        /// </summary>
        /// <param name="pageSize">The number of Pictures per page.</param>
        /// <param name="id">The current page number.</param>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="userAccessLevel">The user's access level for the Progeny.</param>
        /// <param name="sortBy">Sort order. 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="tagFilter">Only include Pictures tagged with this string. If null or empty include all Pictures.</param>
        /// <param name="timeZone">The time zone to use for PictureTime.</param>
        /// <returns>PicturePageViewModel</returns>
        Task<PicturePageViewModel> GetPicturePage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy,
            string tagFilter, string timeZone);

        /// <summary>
        /// Gets a PictureViewModel for the Picture with a given PictureId.
        /// PictureTime and Comment's time will be converted to the given time zone.
        /// </summary>
        /// <param name="id">The PictureId for the Picture to get..</param>
        /// <param name="userAccessLevel">The user's access level for the Progeny that the Picture belongs to.</param>
        /// <param name="sortBy">Sort order. 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="timeZone">The time zone to use for PictureTime and Comment's time.</param>
        /// <param name="tagFilter">Only include Pictures tagged with this string. If null or empty include all Pictures.</param>
        /// <returns>PictureVieModel.</returns>
        Task<PictureViewModel> GetPictureViewModel(int id, int userAccessLevel, int sortBy, string timeZone, string tagFilter = "");

        /// <summary>
        /// Gets the video with the given VideoId, with the VideoTime converted to the given time zone.
        /// </summary>
        /// <param name="videoId">The VideoId of the video to get.</param>
        /// <param name="timeZone">The time zone to use for VideoTime.(TimeZoneInfo.Id or UserInfo.Timezone).</param>
        /// <returns>Video object with the given VideoId. Video object with VideoId = 0 if the Video isn't found.</returns>
        Task<Video> GetVideo(int videoId, string timeZone);

        /// <summary>
        /// Gets a list of Videos for a given progeny that a user has access to, with the VideoTime converted to the given time zone for each video.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <param name="timeZone">The time zone to use for VideoTime.</param>
        /// <returns>List of Video objects.</returns>
        Task<List<Video>> GetVideoList(int progenyId, int accessLevel, string timeZone);
        
        /// <summary>
        /// Adds a new Video.
        /// </summary>
        /// <param name="video">The new Video object to add.</param>
        /// <returns>The added Video object.</returns>
        Task<Video> AddVideo(Video video);

        /// <summary>
        /// Updates a Video. The Video with the same VideoId will be updated.
        /// </summary>
        /// <param name="video">The Video with the updated properties.</param>
        /// <returns>The updated Video object.</returns>
        Task<Video> UpdateVideo(Video video);

        /// <summary>
        /// Removes the Video with the given VideoId.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video to remove.</param>
        /// <returns>bool: True if the Video was successfully removed.</returns>
        Task<bool> DeleteVideo(int videoId);

        /// <summary>
        /// Adds a comment for a Video.
        /// </summary>
        /// <param name="comment">The Comment object to add.</param>
        /// <returns>bool: True if the comment was successfully added.</returns>
        Task<bool> AddVideoComment(Comment comment);

        /// <summary>
        /// Removes a Video Comment with the given CommentId.
        /// </summary>
        /// <param name="commentId">The CommentId of the Comment to remove.</param>
        /// <returns>bool: True if the Comment was successfully removed.</returns>
        Task<bool> DeleteVideoComment(int commentId);

        /// <summary>
        /// Gets a VideoPageViewModel for a progeny that a user has access to.
        /// </summary>
        /// <param name="pageSize">The number of Videos per page.</param>
        /// <param name="id">The current page number.</param>
        /// <param name="progenyId">The Id of the Progeny.</param>
        /// <param name="userAccessLevel">The user's access level for the Progeny.</param>
        /// <param name="sortBy">Sort order. 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="tagFilter">Only include Videos tagged with this string. If null or empty include all Pictures.</param>
        /// <param name="timeZone">The time zone to use for VideoTime.</param>
        /// <returns>VideoPageViewModel</returns>
        Task<VideoPageViewModel> GetVideoPage(int pageSize, int id, int progenyId, int userAccessLevel, int sortBy,
            string tagFilter, string timeZone);

        /// <summary>
        /// Gets a VideoViewModel for the Video with a given VideoId.
        /// </summary>
        /// <param name="id">The VideoId for the Video to get the VideoViewModel for.</param>
        /// <param name="userAccessLevel">The user's access level for Progeny the Video belongs to.</param>
        /// <param name="sortBy">Sort order. 0 for oldest first, 1 (default) for newest first.</param>
        /// <param name="timeZone">The time zone to use for VideoTime and Comment's time.</param>
        /// <param name="tagFilter">Only include Videos tagged with this string. If null or empty include all Pictures.</param>
        /// <returns>VideoViewModel</returns>
        Task<VideoViewModel> GetVideoViewModel(int id, int userAccessLevel, int sortBy, string timeZone, string tagFilter = "");

        /// <summary>
        /// Gets a list of all Pictures for a given progeny that a user has access to.
        /// Time zone for PictureTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get Pictures for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Picture objects.</returns>
        Task<List<Picture>> GetProgenyPictureList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets a simplified PictureViewModel for a Picture entity with the provided PictureId.
        /// PictureNumber, PictureCount, CommentsList, and TagsList are not included. Time zone for PictureTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to get a PictureViewModel for.</param>
        /// <returns>PictureViewModel.</returns>
        Task<PictureViewModel> GetPictureElement(int pictureId);

        /// <summary>
        /// Gets a list of all Videos for a given progeny that a user has access to.
        /// Time zone for VideoTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="progenyId">The Id of the Progeny to get all Videos for.</param>
        /// <param name="accessLevel">The user's access level for the Progeny.</param>
        /// <returns>List of Video objects.</returns>
        Task<List<Video>> GetProgenyVideoList(int progenyId, int accessLevel);

        /// <summary>
        /// Gets a simplified VideoViewModel for a Video entity with the provided VideoId.
        /// VideoNumber, VideoCount, CommentsList, and TagsList are not included. Time zone for VideoTime will not be converted and should be assumed to be UTC.
        /// </summary>
        /// <param name="videoId">The VideoId of the Video to get a VideoViewModel for.</param>
        /// <returns>VideoViewModel.</returns>
        Task<VideoViewModel> GetVideoElement(int videoId);
    }
}
