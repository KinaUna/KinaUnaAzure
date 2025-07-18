﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Extensions;
using KinaUna.Data.Extensions.ThirdPartyElements;
using KinaUna.Data.Models;
using KinaUna.Data.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Location = KinaUna.Data.Models.Location;

namespace KinaUnaProgenyApi.Services
{
    public class PicturesService : IPicturesService
    {
        private readonly MediaDbContext _mediaContext;
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions = new();
        private readonly DistributedCacheEntryOptions _cacheOptionsSliding = new();
        private readonly IImageStore _imageStore;

        public PicturesService(MediaDbContext mediaContext, IDistributedCache cache, IImageStore imageStore)
        {
            _mediaContext = mediaContext;
            _imageStore = imageStore;
            _cache = cache;
            _ = _cacheOptions.SetAbsoluteExpiration(new TimeSpan(0, 5, 0)); // Expire after 5 minutes.
            _ = _cacheOptionsSliding.SetSlidingExpiration(new TimeSpan(96, 0, 0)); // Expire after 24 hours.
        }

        /// <summary>
        /// Gets a Picture by PictureId.
        /// First tries to get the Picture from the cache, then from the database if it's not in the cache.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get.</param>
        /// <returns>Picture object with the given PictureId. Null if the Picture doesn't exist.</returns>
        public async Task<Picture> GetPicture(int id)
        {
            Picture picture = await GetPictureFromCache(id);
            if (picture == null || picture.PictureId == 0)
            {
                picture = await SetPictureInCache(id);
            }

            if (picture == null) return null;
            
            if (picture.PictureRotation != null)
            {
                return picture;
            }

            picture.PictureRotation = 0;
            _ = await UpdatePicture(picture);

            return picture;
        }

        /// <summary>
        /// Gets a Picture by PictureId from the cache.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get.</param>
        /// <returns>The Picture with the given PictureId. Null if it is not found in the cache.</returns>
        private async Task<Picture> GetPictureFromCache(int id)
        {
            string cachedPicture = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "picture" + id);
            if (string.IsNullOrEmpty(cachedPicture))
            {
                return null;
            }
            
            Picture picture = JsonConvert.DeserializeObject<Picture>(cachedPicture);
            return picture;
        }

        /// <summary>
        /// Adds a new Picture to the database and the cache.
        /// </summary>
        /// <param name="picture">The Picture to add.</param>
        /// <returns>The added Picture object.</returns>
        public async Task<Picture> AddPicture(Picture picture)
        {
            picture.RemoveNullStrings();
            Picture pictureToAdd = new();
            pictureToAdd.CopyPropertiesForAdd(picture);
            _ = _mediaContext.PicturesDb.Add(pictureToAdd);
            _ = await _mediaContext.SaveChangesAsync();

            _ = await SetPictureInCache(pictureToAdd.PictureId);

            return pictureToAdd;
        }

        /// <summary>
        /// For updating Picture with missing file extension in PictureLink.
        /// Updates PictureLink, PictureLink600, and PictureLink1200 with the correct file extension.
        /// </summary>
        /// <param name="picture">The Picture to update.</param>
        /// <returns>The updated Picture object.</returns>
        private async Task<Picture> UpdatePictureLinkWithExtension(Picture picture)
        {
            string originalPictureLink = picture.PictureLink;
            string originalPictureLink600 = picture.PictureLink600;
            string originalPictureLink1200 = picture.PictureLink1200;
            try
            {
                MemoryStream memoryStream = await _imageStore.GetStream(picture.PictureLink);
                if (memoryStream.Length > 0)
                {
                    memoryStream.Position = 0;

                    using (MagickImage image = new(memoryStream))
                    {
                        using MemoryStream memStream = new();
                        await image.WriteAsync(memStream);
                        memStream.Position = 0;
                        picture.PictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Pictures, image.FileExtensionString());
                    }
                }
                

                MemoryStream memoryStream600 = await _imageStore.GetStream(picture.PictureLink600);
                if (memoryStream600.Length > 0)
                {
                    memoryStream600.Position = 0;

                    using (MagickImage image600 = new(memoryStream600))
                    {
                        using MemoryStream memStream600 = new();
                        await image600.WriteAsync(memStream600);
                        memStream600.Position = 0;
                        picture.PictureLink600 = await _imageStore.SaveImage(memStream600, BlobContainers.Pictures, image600.FileExtensionString());
                    }
                }
                

                MemoryStream memoryStream1200 = await _imageStore.GetStream(picture.PictureLink1200);
                if (memoryStream1200.Length > 0)
                {
                    memoryStream1200.Position = 0;

                    using (MagickImage image1200 = new(memoryStream1200))
                    {
                        using MemoryStream memStream1200 = new();
                        await image1200.WriteAsync(memStream1200);
                        memStream1200.Position = 0;
                        picture.PictureLink1200 = await _imageStore.SaveImage(memStream1200, BlobContainers.Pictures, image1200.FileExtensionString());
                    }
                }
                

                picture = await UpdatePicture(picture);
                _ = await _imageStore.DeleteImage(originalPictureLink, BlobContainers.Pictures);
                _ = await _imageStore.DeleteImage(originalPictureLink600, BlobContainers.Pictures);
                _ = await _imageStore.DeleteImage(originalPictureLink1200, BlobContainers.Pictures);

                return picture;
            }
            catch (Exception)
            {
                picture.PictureLink = originalPictureLink;
                picture.PictureLink600 = originalPictureLink600;
                picture.PictureLink1200 = originalPictureLink1200;
                return picture;
            }
        }

        /// <summary>
        /// Processes a Picture: Extracts GPS data, dimensions, rotation, and timestamp.
        /// Also creates resized versions the image and saves the filenames in ImageLink600 and ImageLink1200.
        /// </summary>
        /// <param name="picture">The Picture object to process.</param>
        /// <returns>The updated Picture object.</returns>
        public async Task<Picture> ProcessPicture(Picture picture)
        {
            MemoryStream memoryStream = await _imageStore.GetStream(picture.PictureLink);
            if (memoryStream.Length > 0)
            {
                memoryStream.Position = 0;

                using (MagickImage image = new(memoryStream))
                {
                    IExifProfile profile = image.GetExifProfile();
                    if (profile != null)
                    {
                        picture.Longtitude = profile.GetLongitude();
                        picture.Latitude = profile.GetLatitude();
                        picture.Altitude = profile.GetAltitude();

                        picture.PictureRotation = profile.GetRotationInDegrees();

                        picture.PictureTime = profile.GetDateTime();

                        picture.PictureWidth = profile.GetPictureWidth(image);
                        picture.PictureHeight = profile.GetPictureHeight(image);
                    }
                    else
                    {
                        picture.PictureWidth = (int)image.Width;
                        picture.PictureHeight = (int)image.Height;

                    }

                    if (picture.PictureRotation != null)
                    {
                        if (picture.PictureRotation != 0)
                        {
                            image.Rotate((int)picture.PictureRotation);
                        }

                    }

                    if (picture.PictureWidth > 600)
                    {
                        const int newWidth = 600;
                        int newHeight = (600 / picture.PictureWidth) * picture.PictureHeight;

                        image.Resize(newWidth, (uint)newHeight);
                    }

                    image.Strip();

                    using MemoryStream memStream = new();
                    await image.WriteAsync(memStream);
                    memStream.Position = 0;

                    picture.PictureLink600 = await _imageStore.SaveImage(memStream, BlobContainers.Pictures, image.FileExtensionString());
                }
            }
            
            using (MagickImage image = new(memoryStream))
            {
                if (picture.PictureRotation != null)
                {
                    if (picture.PictureRotation != 0)
                    {
                        image.Rotate((int)picture.PictureRotation);
                    }

                }

                if (picture.PictureWidth > 1200)
                {
                    const int newWidth = 1200;
                    int newHeight = (1200 / picture.PictureWidth) * picture.PictureHeight;

                    image.Resize(newWidth, (uint)newHeight);
                }

                image.Strip();

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;
                picture.PictureLink1200 = await _imageStore.SaveImage(memStream,BlobContainers.Pictures, image.FileExtensionString());
            }

            if (picture.PictureTime != null)
            {
                picture.PictureTime = TimeZoneInfo.ConvertTimeToUtc(picture.PictureTime.Value,
                    TimeZoneInfo.FindSystemTimeZoneById(picture.TimeZone));
            }

            if (picture.PictureTime == null || picture.PictureTime.Value.Year < 2)
            {
                picture.PictureTime = DateTime.UtcNow;
            }

            if (picture.Longtitude == "" || picture.Latitude == "") return picture;

            if (string.IsNullOrEmpty(picture.Location))
            {
                picture.Location = picture.Latitude + ", " + picture.Longtitude;
            }

            return picture;
        }

        /// <summary>
        /// Processes a Progeny profile picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile with the image data.</param>
        /// <returns>The filename of the saved profile picture.</returns>
        public async Task<string> ProcessProgenyPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();
                
                if (rotation != 0)
                {
                    image.Rotate(rotation);
                }
            }

            int newWidth = (int)((180 / image.Height) * image.Width);
            int newHeight = 180;
            if (image.Width > image.Height)
            {
                newWidth = 180;
                newHeight = (int)((180 / image.Width) * image.Height);
            }
            image.Resize((uint)newWidth, (uint)newHeight);
            image.Strip();

            using MemoryStream memStream = new();
            await image.WriteAsync(memStream);
            memStream.Position = 0;
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Progeny, image.FileExtensionString());

            return pictureLink;
        }

        /// <summary>
        /// Processes a Profile Picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile object with the file data.</param>
        /// <returns>The filename of the saved image.</returns>
        public async Task<string> ProcessProfilePicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();
                
                if (rotation != 0)
                {
                    image.Rotate(rotation);
                }
            }

            int newWidth = (int)((180 / image.Height) * image.Width);
            int newHeight = 180;
            if (image.Width > image.Height)
            {
                newWidth = 180;
                newHeight = (int)((180 / image.Width) * image.Height);
            }
            image.Resize((uint)newWidth, (uint)newHeight);
            image.Strip();

            using MemoryStream memStream = new();
            await image.WriteAsync(memStream);
            memStream.Position = 0;
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Profiles, image.FileExtensionString());

            return pictureLink;
        }

        /// <summary>
        /// Extracts the file extension of an image file in a blob container and saves to a new file with the file extension.
        /// </summary>
        /// <param name="itemPictureGuid">The current filename.</param>
        /// <param name="container">The storage container of the file.</param>
        /// <returns>The new filename with extension.</returns>
        public async Task<string> UpdateItemPictureExtension(string itemPictureGuid, string container)
        {
            MemoryStream memoryStream = await _imageStore.GetStream(itemPictureGuid, container);
            if (memoryStream.Length > 0)
            {
                memoryStream.Position = 0;

                using MagickImage image = new(memoryStream);

                using MemoryStream memStream = new();
                await image.WriteAsync(memStream);
                memStream.Position = 0;

                string pictureLink = await _imageStore.SaveImage(memStream, container, image.FileExtensionString());

                return pictureLink;
            }

            return itemPictureGuid;
        }
        
        /// <summary>
        /// Processes a Friend Picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile object with the file.</param>
        /// <returns>The filename of the saved image.</returns>
        public async Task<string> ProcessFriendPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();

                if (rotation != 0)
                {
                    image.Rotate(rotation);
                }
            }

            int newWidth = (int)((180 / image.Height) * image.Width);
            int newHeight = 180;
            if (image.Width > image.Height)
            {
                newWidth = 180;
                newHeight = (int)((180 / image.Width) * image.Height);
            }
            image.Resize((uint)newWidth, (uint)newHeight);
            image.Strip();

            using MemoryStream memStream = new();
            await image.WriteAsync(memStream);
            memStream.Position = 0;
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Friends, image.FileExtensionString());

            return pictureLink;
        }

        /// <summary>
        /// Processes a Contact Picture: Resizes the image and rotates it if necessary.
        /// </summary>
        /// <param name="file">IFormFile object with the file.</param>
        /// <returns>The filename of the saved image.</returns>
        public async Task<string> ProcessContactPicture(IFormFile file)
        {
            using MagickImage image = new(file.OpenReadStream());
            IExifProfile profile = image.GetExifProfile();
            if (profile != null)
            {
                int rotation = profile.GetRotationInDegrees();

                if (rotation != 0)
                {
                    image.Rotate(rotation);
                }
            }

            int newWidth = (int)((180 / image.Height) * image.Width);
            int newHeight = 180;
            if (image.Width > image.Height)
            {
                newWidth = 180;
                newHeight = (int)((180 / image.Width) * image.Height);
            }
            image.Resize((uint)newWidth, (uint)newHeight);
            image.Strip();

            using MemoryStream memStream = new();
            await image.WriteAsync(memStream);
            memStream.Position = 0;
            
            string pictureLink = await _imageStore.SaveImage(memStream, BlobContainers.Contacts, image.FileExtensionString());
            
            return pictureLink;
        }

        /// <summary>
        /// Gets a Picture by PictureLink.
        /// </summary>
        /// <param name="link">The PictureLink of the Picture to get.</param>
        /// <returns>Picture object with the given PictureLink. Null if the Picture doesn't exist.</returns>
        public async Task<Picture> GetPictureByLink(string link)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureLink == link);
            return picture;
        }

        /// <summary>
        /// Gets a Picture by PictureId from the database and adds it to the cache.
        /// </summary>
        /// <param name="id">The PictureId of the Picture to get and set.</param>
        /// <returns>Picture with the given PictureId. Null if the Picture doesn't exist.</returns>
        public async Task<Picture> SetPictureInCache(int id)
        {
            Picture picture = await _mediaContext.PicturesDb.AsNoTracking().SingleOrDefaultAsync(p => p.PictureId == id);
            if (picture == null) return null;

            if (picture.Tags != null && picture.Tags.Trim().EndsWith(','))
            {
                picture.Tags = picture.Tags.Trim().TrimEnd(',');
                _ = await UpdatePicture(picture);
            }

            if (!picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) && !picture.PictureLink.Contains('.')) // Some pictures do not have file extensions. If they don't, update the links.
            {
                try
                {
                    picture = await UpdatePictureLinkWithExtension(picture);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
            
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "picture" + id, JsonConvert.SerializeObject(picture), _cacheOptionsSliding);

            _ = await SetPicturesListInCache(picture.ProgenyId);

            return picture;
        }

        /// <summary>
        /// Updates a Picture in the database and the cache.
        /// </summary>
        /// <param name="picture">The Picture object with the updated properties.</param>
        /// <returns>The updated Picture object.</returns>
        public async Task<Picture> UpdatePicture(Picture picture)
        {
            picture.RemoveNullStrings();
            Picture pictureToUpdate = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == picture.PictureId);
            if (pictureToUpdate == null) return null;

            pictureToUpdate.CopyPropertiesForUpdate(picture);

            _ = _mediaContext.PicturesDb.Update(pictureToUpdate);
            _ = await _mediaContext.SaveChangesAsync();
            _ = await SetPictureInCache(pictureToUpdate.PictureId);

            return pictureToUpdate;

        }

        /// <summary>
        /// Deletes a Picture from the database and the cache.
        /// </summary>
        /// <param name="picture">The Picture to delete.</param>
        /// <returns>The deleted Picture object.</returns>
        public async Task<Picture> DeletePicture(Picture picture)
        {
            Picture pictureToDelete = await _mediaContext.PicturesDb.SingleOrDefaultAsync(p => p.PictureId == picture.PictureId);
            if (pictureToDelete != null)
            {
                _ = _mediaContext.PicturesDb.Remove(pictureToDelete);
                _ = await _mediaContext.SaveChangesAsync();
            }

            List<Picture> picturesWithThisImageLink = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.PictureLink == picture.PictureLink).ToListAsync();
            if (picturesWithThisImageLink.Count == 0)
            {
                _ = await _imageStore.DeleteImage(picture.PictureLink);
            }

            List<Picture> picturesWithThisImageLink600 = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.PictureLink600 == picture.PictureLink600).ToListAsync();
            if (picturesWithThisImageLink600.Count == 0)
            {
                _ = await _imageStore.DeleteImage(picture.PictureLink600);
            }

            List<Picture> picturesWithThisImageLink1200 = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.PictureLink1200 == picture.PictureLink1200).ToListAsync();
            if (picturesWithThisImageLink1200.Count == 0)
            {
                _ = await _imageStore.DeleteImage(picture.PictureLink1200);
            }

            await RemovePictureFromCache(picture.PictureId, picture.ProgenyId);
            return pictureToDelete;
        }

        /// <summary>
        /// Removes a Picture from the cache and updates the Pictures list for the Progeny in the cache.
        /// </summary>
        /// <param name="pictureId">The PictureId of the Picture to remove.</param>
        /// <param name="progenyId">The ProgenyId of the Progeny that the Picture belongs to.</param>
        /// <returns></returns>
        public async Task RemovePictureFromCache(int pictureId, int progenyId)
        {
            await _cache.RemoveAsync(Constants.AppName + Constants.ApiVersion + "picture" + pictureId);

            _ = await SetPicturesListInCache(progenyId);
        }

        /// <summary>
        /// Gets a list of all Pictures for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Pictures for.</param>
        /// <param name="accessLevel">The access level for the user.</param>
        /// <returns>List of Picture objects.</returns>
        public async Task<List<Picture>> GetPicturesList(int progenyId, int accessLevel)
        {
            List<Picture> picturesList = await GetPicturesListFromCache(progenyId);
            if (picturesList.Count == 0)
            {
                picturesList = await SetPicturesListInCache(progenyId);
            }

            picturesList = picturesList.Where(p => p.AccessLevel >= accessLevel).ToList();

            return picturesList;
        }

        /// <summary>
        /// Gets a list of all Pictures containing a specific tag for a Progeny.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get pictures for.</param>
        /// <param name="tag">String with the tag.</param>
        /// <param name="accessLevel">The access level for the user.</param>
        /// <returns>List of Picture objects.</returns>
        public async Task<List<Picture>> GetPicturesWithTag(int progenyId, string tag, int accessLevel)
        {
            List<Picture> allItems = await GetPicturesList(progenyId, accessLevel);
            if (!string.IsNullOrEmpty(tag))
            {
                allItems = [.. allItems.Where(p => p.Tags != null && p.Tags.Contains(tag, StringComparison.CurrentCultureIgnoreCase))];
            }
            return allItems;
        }

        // Todo: add unit tests for these methods.
        /// <summary>
        /// Gets a list of distinct Locations for a Progeny's pictures.
        /// </summary>
        /// <param name="picturesLocationsRequest">PicturesLocationsRequest with the distance, in kilometers, to group picture locations by.</param>
        /// <param name="userAccesses">List of UserAccess objects with the access level for each Progeny.</param>
        /// <returns>PicturesLocationsResponse</returns>
        public async Task<PicturesLocationsResponse> GetPicturesLocations(PicturesLocationsRequest picturesLocationsRequest, List<UserAccess> userAccesses)
        {
            List<Picture> allPictures = [];

            foreach (int progenyId in picturesLocationsRequest.Progenies)
            {
                int accessLevel = userAccesses.FirstOrDefault(u => u.ProgenyId == progenyId)?.AccessLevel ?? 5;
                List<Picture> progenyPictures = await GetPicturesList(progenyId, accessLevel);
                progenyPictures = [.. progenyPictures.Where(p => !string.IsNullOrEmpty(p.Longtitude))];
                
                allPictures.AddRange(progenyPictures);
            }

            // Group by location, using the distance parameter.
            List<Location> locations = [];
            foreach (Picture picture in allPictures)
            {
                bool latitudeParsed = double.TryParse(picture.Latitude, out double latitude);
                bool longitudeParsed = double.TryParse(picture.Longtitude, out double longitude);

                if (!latitudeParsed || !longitudeParsed) continue;

                Location location = new()
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    ProgenyId = picture.ProgenyId
                };
                if (locations.Any(l => l.Distance(location.Latitude, location.Longitude) < picturesLocationsRequest.Distance))
                {
                    continue;
                }
                locations.Add(location);
            }

            PicturesLocationsResponse picturesLocationsResponse = new()
            {
                ProgenyId = picturesLocationsRequest.ProgenyId,
                LocationsList = locations,
                NumberOfLocations = locations.Count
            };

            return picturesLocationsResponse;
        }

        /// <summary>
        /// Gets a list of Pictures near a specific Location.
        /// </summary>
        /// <param name="nearByPhotosRequest">NearByPhotosRequest object with the location data.</param>
        /// <param name="userAccesses">List of UserAccess objects with the access level for each Progeny.</param>
        /// <returns>NearByPhotosResponse, with the list of Picture objects.</returns>
        public async Task<NearByPhotosResponse> GetPicturesNearLocation(NearByPhotosRequest nearByPhotosRequest, List<UserAccess> userAccesses)
        {
            // Todo: Add unit tests for this method.
            // Todo: Add sort order parameter.

            List<Picture> allPictures = [];

            foreach (int progenyId in nearByPhotosRequest.Progenies)
            {
                int accessLevel = userAccesses.FirstOrDefault(u => u.ProgenyId == progenyId)?.AccessLevel ?? 5;
                List<Picture> progenyPictures = await GetPicturesList(progenyId, accessLevel);
                progenyPictures = [.. progenyPictures.Where(p => !string.IsNullOrEmpty(p.Longtitude))];
                
                allPictures.AddRange(progenyPictures);
            }

            List<Picture> nearPictures = [];
            foreach (Picture picture in allPictures)
            {
                bool latitudeParsed = double.TryParse(picture.Latitude, out double latitude);
                bool longitudeParsed = double.TryParse(picture.Longtitude, out double longitude);
                
                if (!latitudeParsed || !longitudeParsed) continue;

                double distance = nearByPhotosRequest.LocationItem.Distance(latitude, longitude);
                if (distance < nearByPhotosRequest.Distance)
                {
                    nearPictures.Add(picture);
                }
            }

            nearPictures = [.. nearPictures.OrderBy(p => p.PictureTime)];
            if (nearByPhotosRequest.SortOrder == 1)
            {
                nearPictures = [.. nearPictures.OrderByDescending(p => p.PictureTime)];
            }

            NearByPhotosResponse nearByPhotosResponse = new()
            {
                ProgenyId = nearByPhotosRequest.ProgenyId,
                LocationItem = nearByPhotosRequest.LocationItem,
                PicturesList = nearPictures,
                NumberOfPictures = nearPictures.Count
            };

            return nearByPhotosResponse;
        }

        /// <summary>
        /// Gets a list of all Pictures for a Progeny from the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get all Pictures for.</param>
        /// <returns>List of Picture objects.</returns>
        private async Task<List<Picture>> GetPicturesListFromCache(int progenyId)
        {
            List<Picture> picturesList = [];
            string cachedPicturesList = await _cache.GetStringAsync(Constants.AppName + Constants.ApiVersion + "pictureslist" + progenyId);
            if (!string.IsNullOrEmpty(cachedPicturesList))
            {
                picturesList = JsonConvert.DeserializeObject<List<Picture>>(cachedPicturesList);
            }

            return picturesList;
        }

        /// <summary>
        /// Gets a list of all Pictures for a Progeny from the database and adds it to the cache.
        /// </summary>
        /// <param name="progenyId">The ProgenyId of the Progeny to get and set all Pictures for.</param>
        /// <returns>List of Picture objects.</returns>
        public async Task<List<Picture>> SetPicturesListInCache(int progenyId)
        {
            List<Picture> picturesList = await _mediaContext.PicturesDb.AsNoTracking().Where(p => p.ProgenyId == progenyId).ToListAsync();
            await _cache.SetStringAsync(Constants.AppName + Constants.ApiVersion + "pictureslist" + progenyId, JsonConvert.SerializeObject(picturesList), _cacheOptionsSliding);

            return picturesList;
        }

        /// <summary>
        /// Replaces all null strings in the Pictures table with empty strings.
        /// </summary>
        /// <returns></returns>
        public async Task UpdateAllPictures()
        {
            List<Picture> allPicturesList = await _mediaContext.PicturesDb.ToListAsync();
            foreach (Picture picture in allPicturesList)
            {
                picture.RemoveNullStrings();
                _ = await UpdatePicture(picture);
            }
        }

        public async Task CheckPicturesForExtensions()
        {
            List<Picture> allPicturesList = await _mediaContext.PicturesDb.ToListAsync();
            foreach (Picture picture in allPicturesList)
            {
                if(string.IsNullOrEmpty(picture.PictureLink) || string.IsNullOrEmpty(picture.PictureLink600) || string.IsNullOrEmpty(picture.PictureLink1200)) continue;

                if (!picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) && !picture.PictureLink.Contains('.'))
                {
                    _ = await UpdatePictureLinkWithExtension(picture);
                }
            }
        }

        public async Task CheckPictureLinks()
        {
            List<Picture> allPicturesList = await _mediaContext.PicturesDb.ToListAsync();
            foreach (Picture picture in allPicturesList)
            {
                if (string.IsNullOrEmpty(picture.PictureLink) || string.IsNullOrEmpty(picture.PictureLink600) || string.IsNullOrEmpty(picture.PictureLink1200))
                {
                    _ = await DeletePicture(picture);
                    continue;
                }

                if (picture.PictureLink.StartsWith("http", StringComparison.CurrentCultureIgnoreCase)) continue;
                if (!await _imageStore.ImageExists(picture.PictureLink, BlobContainers.Pictures))
                {
                    _ = await DeletePicture(picture);
                }
            }
        }

        public async Task CheckPicturePropertiesForNull()
        {
            List<Picture> allPicturesList = await _mediaContext.PicturesDb.ToListAsync();
            foreach (Picture picture in allPicturesList)
            {
                bool changed = false;
                if (picture.Altitude == null)
                {
                    picture.Altitude = "";
                    changed = true;
                }

                if (picture.Latitude == null)
                {
                    picture.Latitude = "";
                    changed = true;
                }

                if (picture.Longtitude == null)
                {
                    picture.Longtitude = "";
                    changed = true;
                }

                if (picture.Location == null)
                {
                    picture.Location = "";
                    changed = true;
                }

                if (picture.Tags == null)
                {
                    picture.Tags = "";
                    changed = true;
                }
                
                if (picture.PictureRotation == null)
                {
                    picture.PictureRotation = 0;
                    changed = true;
                }

                if (changed)
                {
                    _ = await UpdatePicture(picture);
                }
            }
        }
    }
}
