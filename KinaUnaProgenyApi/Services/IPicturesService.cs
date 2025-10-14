using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace KinaUnaProgenyApi.Services
{
    public interface IPicturesService
    {
        /// <summary>
        /// Gets a Picture by PictureId.
        /// First tries to get the Picture from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get.</param>
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>Picture object with the given PictureId. Null if the Picture doesn't exist.</returns>
        Task<Picture> GetPicture(int id, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a Picture by PictureLink.
        /// </summary>
        /// <param name="link">The PictureLink of the Picture to get.</param>
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>Picture object with the given PictureLink. Null if the Picture doesn't exist.</returns>
        Task<Picture> GetPictureByLink(string link, UserInfo currentUserInfo);

        /// <summary>
        /// Adds a new Picture to the database and the cache.
        /// </summary>
        /// <param name="picture">The Picture to add.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The added Picture object.</returns>
        Task<Picture> AddPicture(Picture picture, UserInfo currentUserInfo);

        /// <summary>
        /// Updates a Picture in the database and the cache.
        /// </summary>
        /// <param name="picture">The Picture object with the updated properties.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The updated Picture object.</returns>
        Task<Picture> UpdatePicture(Picture picture, UserInfo currentUserInfo);

        /// <summary>
        /// Deletes a Picture from the database and the cache.
        /// </summary>
        /// <param name="picture">The Picture to delete.</param>
        /// <param name="currentUserInfo"></param>
        /// <returns>The deleted Picture object.</returns>
        Task<Picture> DeletePicture(Picture picture, UserInfo currentUserInfo);

        /// <summary>
        /// Processes a Picture: Extracts GPS data, dimensions, rotation, and timestamp.
        /// Also creates resized versions the image and saves the filenames in ImageLink600 and ImageLink1200.
        /// </summary>
        /// <param name="picture">The Picture object to process.</param>
        /// <returns>The updated Picture object.</returns>
        Task<Picture> ProcessPicture(Picture picture);

        /// <summary>
        /// Processes a Progeny profile picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile with the image data.</param>
        /// <returns>The filename of the saved profile picture.</returns>
        Task<string> ProcessProgenyPicture(IFormFile file);

        /// <summary>
        /// Processes a Profile Picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile object with the file data.</param>
        /// <returns>The filename of the saved image.</returns>
        Task<string> ProcessProfilePicture(IFormFile file);

        /// <summary>
        /// Processes a Friend Picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile object with the file.</param>
        /// <returns>The filename of the saved image.</returns>
        Task<string> ProcessFriendPicture(IFormFile file);

        /// <summary>
        /// Processes a Contact Picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile object with the file.</param>
        /// <returns>The filename of the saved image.</returns>
        Task<string> ProcessContactPicture(IFormFile file);

        /// <summary>
        /// Gets a Picture by PictureId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get and set.</param>
        /// <returns>Picture with the given PictureId. Null if the Picture doesn't exist.</returns>
        Task<Picture> SetPictureInCache(int id);

        /// <summary>
        /// Removes a Picture from the cache and updates the Pictures list for the Progeny in the cache.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Picture belongs to.</param>
        /// <returns></returns>
        Task RemovePictureFromCache(int pictureId, int progenyId);

        Task<Picture> RandomPicture(int progenyId, UserInfo currentUserInfo);
        /// <summary>
        /// Gets a list of all Pictures for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Pictures for.</param>
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Picture objects.</returns>
        Task<List<Picture>> GetPicturesList(int progenyId, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of all Pictures for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set all Pictures for.</param>
        /// <returns>List of Picture objects.</returns>
        Task<List<Picture>> SetPicturesListInCache(int progenyId);

        /// <summary>
        /// Gets a list of all Pictures containing a specific tag for a Progeny.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get pictures for.</param>
        /// <param name="tag">String with the tag.</param>
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>List of Picture objects.</returns>
        Task<List<Picture>> GetPicturesWithTag(int progenyId, string tag, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of distinct Locations for a Progeny's pictures.
        /// </summary>
        /// <param name="picturesLocationsRequest">PicturesLocationsRequest with the distance, in kilometers, to group picture locations by.</param>
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>PicturesLocationsResponse</returns>
        Task<PicturesLocationsResponse> GetPicturesLocations(PicturesLocationsRequest picturesLocationsRequest, UserInfo currentUserInfo);

        /// <summary>
        /// Gets a list of Pictures near a specific Location.
        /// </summary>
        /// <param name="nearByPhotosRequest">NearByPhotosRequest object with the location data.</param>
        /// <param name="currentUserInfo">UserInfo object for the current user, to check permissions.</param>
        /// <returns>NearByPhotosResponse, with the list of Picture objects.</returns>
        Task<NearByPhotosResponse> GetPicturesNearLocation(NearByPhotosRequest nearByPhotosRequest, UserInfo currentUserInfo);

        /// <summary>
        /// Extracts the file extension of an image file in a blob container and saves to a new file with the file extension.
        /// </summary>
        /// <param name="itemPictureGuid">The current filename.</param>
        /// <param name="container">The storage container of the file.</param>
        /// <returns>The new filename with extension.</returns>
        Task<string> UpdateItemPictureExtension(string itemPictureGuid, string container);

        /// <summary>
        /// Checks all pictures in the database for missing file extensions in their links and updates them if
        /// necessary.
        /// </summary>
        /// <remarks>This method retrieves all pictures from the database and verifies whether their <see
        /// cref="Picture.PictureLink"/>, <see cref="Picture.PictureLink600"/>, and <see
        /// cref="Picture.PictureLink1200"/> properties are valid and contain file extensions. If a picture's <see
        /// cref="Picture.PictureLink"/> does not start with "http" or contain a file extension, it attempts to update
        /// the link by calling <c>UpdatePictureLinkWithExtension</c>.</remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task CheckPicturesForExtensions();

        /// <summary>
        /// Checks all pictures in the database for missing image files in the blob storage and deletes the Picture
        /// </summary>
        /// <returns></returns>
        Task CheckPictureLinks();

        /// <summary>
        /// Ensures that all nullable properties of pictures in the database are set to default values if they are null.
        /// </summary>
        /// <remarks>This method retrieves all pictures from the database and checks for null values in
        /// specific properties. If a property is null, it is assigned a default value: <list type="bullet">
        /// <item><description><see cref="Picture.Altitude"/> is set to an empty string.</description></item>
        /// <item><description><see cref="Picture.Latitude"/> is set to an empty string.</description></item>
        /// <item><description><see cref="Picture.Longtitude"/> is set to an empty string.</description></item>
        /// <item><description><see cref="Picture.Location"/> is set to an empty string.</description></item>
        /// <item><description><see cref="Picture.Tags"/> is set to an empty string.</description></item>
        /// <item><description><see cref="Picture.PictureRotation"/> is set to 0.</description></item> </list> If any
        /// changes are made to a picture, the updated picture is saved back to the database.</remarks>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task CheckPicturePropertiesForNull();
    }
}
