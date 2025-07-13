using ImageMagick;
using System;
using System.Globalization;

namespace KinaUna.Data.Extensions.ThirdPartyElements
{
    public static class MagickImageExtensions
    {
        /// <summary>
        /// Gets the file extension for the image format.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>A string with the file extension, including the dot before the extension.</returns>
        public static string FileExtensionString(this MagickImage image)
        {
            string fileExtension = image.Format switch
            {
                MagickFormat.Jpg => ".jpg",
                MagickFormat.Jpeg => ".jpg",
                MagickFormat.Png => ".png",
                MagickFormat.Gif => ".gif",
                MagickFormat.Bmp => ".bmp",
                MagickFormat.Tiff => ".tiff",
                MagickFormat.Tga => ".tga",
                MagickFormat.Jp2 => ".jp2",
                MagickFormat.Psd => ".psd",
                MagickFormat.Pdf => ".pdf",
                MagickFormat.Eps => ".eps",
                MagickFormat.Pcx => ".pcx",
                MagickFormat.Dds => ".dds",
                MagickFormat.J2c => ".j2c",
                MagickFormat.J2k => ".j2k",
                MagickFormat.WebP => ".webp",
                _ => "",
            };
            return fileExtension;
        }

        /// <summary>
        /// Gets the image rotation from the Exif profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>The rotation as an integer</returns>
        public static int GetRotationInDegrees(this IExifProfile profile)
        {
            int rotation;
            try
            {
                rotation = Convert.ToInt32(profile.GetValue(ExifTag.Orientation)?.Value);
                rotation = rotation switch
                {
                    1 => 0,
                    3 => 180,
                    6 => 90,
                    8 => 270,
                    _ => 0
                };
            }
            catch (ArgumentNullException)
            {
                rotation = 0;
            }
            catch (NullReferenceException)
            {
                rotation = 0;
            }

            return rotation;
        }

        /// <summary>
        /// Gets the GPS Longitude from the Exif profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>A string with the longitude, using invariant culture formatting. If the value cannot be obtained an empty string is returned.</returns>
        public static string GetLongitude(this IExifProfile profile)
        {
            string longitude = "";
            try
            {
                IExifValue gpsLongtitude = profile.GetValue(ExifTag.GPSLongitude);
                if (gpsLongtitude != null)
                {
                    if (gpsLongtitude.GetValue() is Rational[] longValues && (longValues[0].Denominator != 0 && longValues[1].Denominator != 0 &&
                                                                              longValues[2].Denominator != 0))
                    {
                        double long0 = longValues[0].Numerator / (double)longValues[0].Denominator;
                        double long1 = longValues[1].Numerator / (double)longValues[1].Denominator;
                        double long2 = longValues[2].Numerator / (double)longValues[2].Denominator;
                        longitude = (long0 + long1 / 60.0 + long2 / 3600).ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        longitude = "";
                    }
                }
            }
            catch (ArgumentNullException)
            {
                longitude = "";
            }
            catch (NullReferenceException)
            {
                longitude = "";
            }
            catch (Exception)
            {
                longitude = "";
            }

            return longitude;
        }

        /// <summary>
        /// Gets the GPS Latitude from the Exif profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>A string with the latitude, using invariant culture formatting. If the value cannot be obtained an empty string is returned.</returns>
        public static string GetLatitude(this IExifProfile profile)
        {
            string latitude = "";
            try
            {
                IExifValue gpsLatitude = profile.GetValue(ExifTag.GPSLatitude);
                if (gpsLatitude != null)
                {
                    if (gpsLatitude.GetValue() is Rational[] latValues && (latValues[0].Denominator != 0 && latValues[1].Denominator != 0 &&
                                                                              latValues[2].Denominator != 0))
                    {
                        double lat0 = latValues[0].Numerator / (double)latValues[0].Denominator;
                        double lat1 = latValues[1].Numerator / (double)latValues[1].Denominator;
                        double lat2 = latValues[2].Numerator / (double)latValues[2].Denominator;
                        latitude = (lat0 + lat1 / 60.0 + lat2 / 3600).ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        latitude = "";
                    }
                }
            }
            catch (ArgumentNullException)
            {
                latitude = "";
            }
            catch (NullReferenceException)
            {
                latitude = "";
            }
            catch (Exception)
            {
                latitude = "";
            }

            return latitude;
        }

        /// <summary>
        /// Gets the GPS Altitude from the Exif profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>A string with the altitude, using invariant culture formatting. If the value cannot be obtained an empty string is returned.</returns>
        public static string GetAltitude(this IExifProfile profile)
        {
            string altitude = "";
            try
            {
                IExifValue gpsAltitude = profile.GetValue(ExifTag.GPSAltitude);
                if (gpsAltitude != null)
                {
                    Rational altValues = (Rational)gpsAltitude.GetValue();
                    if (altValues.Denominator != 0)
                    {
                        double alt0 = altValues.Numerator / (double)altValues.Denominator;
                        altitude = alt0.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        altitude = "";
                    }
                }
            }
            catch (ArgumentNullException)
            {
                altitude = "";
            }
            catch (NullReferenceException)
            {
                altitude = "";
            }
            catch (Exception)
            {
                altitude = "";
            }

            return altitude;
        }

        /// <summary>
        /// Gets time the picture was taken from the Exif profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns>A DateTime with the time and date. If the value cannot be obtained, null is returned.</returns>
        public static DateTime? GetDateTime(this IExifProfile profile)
        {
            DateTime? dateTime = null;
            try
            {
                string date = profile.GetValue(ExifTag.DateTimeOriginal)?.Value;
                if (!string.IsNullOrEmpty(date))
                {
                    dateTime = new DateTime(
                        int.Parse(date[..4]), // year
                        int.Parse(date.Substring(5, 2)), // month
                        int.Parse(date.Substring(8, 2)), // day
                        int.Parse(date.Substring(11, 2)), // hour
                        int.Parse(date.Substring(14, 2)), // minute
                        int.Parse(date.Substring(17, 2)) // second
                    );
                    // Todo: Check if timezone can be extracted and UTC time found? Currently, it is assumed the image is in user's local time.
                }
            }
            catch (FormatException)
            {
                dateTime = null;
            }
            catch (OverflowException)
            {
                dateTime = null;
            }
            catch (ArgumentNullException)
            {
                dateTime = null;
            }
            catch (NullReferenceException)
            {
                dateTime = null;
            }

            return dateTime;
        }

        /// <summary>
        /// Gets the width of the image from the Exif profile and image data.
        /// If the width cannot be obtained from the Exif profile, the width of the image is returned.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="image"></param>
        /// <returns>Int with the pixel width of the image </returns>
        public static int GetPictureWidth(this IExifProfile profile, MagickImage image)
        {
            int pictureWidth;
            try
            {
                Number w = profile.GetValue(ExifTag.PixelXDimension)?.Value ?? new Number(0);

                pictureWidth = (uint)w != 0 ? Convert.ToInt32((uint)w) : (int)image.Width;
            }
            catch (FormatException)
            {
                pictureWidth = (int)image.Width;
            }
            catch (OverflowException)
            {
                pictureWidth = (int)image.Width;
            }
            catch (ArgumentNullException)
            {
                pictureWidth = (int)image.Width;
            }
            catch (NullReferenceException)
            {
                pictureWidth = (int)image.Width;
            }

            return pictureWidth;
        }

        /// <summary>
        /// Gets the height of the image from the Exif profile and image data.
        /// If the height cannot be obtained from the Exif profile, the height of the image is returned.
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="image"></param>
        /// <returns>Int with the pixel height of the image </returns>
        public static int GetPictureHeight(this IExifProfile profile, MagickImage image)
        {
            int pictureHeight;

            try
            {
                Number h = profile.GetValue(ExifTag.PixelYDimension)?.Value ?? new Number(0);
                pictureHeight = (uint)h != 0 ? Convert.ToInt32((uint)h) : (int)image.Height;
            }
            catch (FormatException)
            {
                pictureHeight = (int)image.Height;
            }
            catch (OverflowException)
            {
                pictureHeight = (int)image.Height;
            }
            catch (ArgumentNullException)
            {
                pictureHeight = (int)image.Height;
            }
            catch (NullReferenceException)
            {
                pictureHeight = (int)image.Height;
            }

            return pictureHeight;
        }
    }
}
