using ImageMagick;
using System;
using System.Globalization;

namespace KinaUna.Data.Extensions.ThirdPartyElements
{
    public static class MagickImageExtensions
    {
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
                _ => "",
            };
            return fileExtension;
        }

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
                        longitude = (long0 + long1 / 60.0 + long2 / 3600).ToString(CultureInfo.CurrentCulture);
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
                    // Todo: Check if timezone can be extracted and UTC time found?
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

        public static int GetPictureWidth(this IExifProfile profile, MagickImage image)
        {
            int pictureWidth;
            try
            {
                Number w = profile.GetValue(ExifTag.PixelXDimension)?.Value ?? new Number(0);

                pictureWidth = (uint)w != 0 ? Convert.ToInt32((uint)w) : image.Width;
            }
            catch (FormatException)
            {
                pictureWidth = image.Width;
            }
            catch (OverflowException)
            {
                pictureWidth = image.Width;
            }
            catch (ArgumentNullException)
            {
                pictureWidth = image.Width;
            }
            catch (NullReferenceException)
            {
                pictureWidth = image.Width;
            }

            return pictureWidth;
        }

        public static int GetPictureHeight(this IExifProfile profile, MagickImage image)
        {
            int pictureHeight;

            try
            {
                Number h = profile.GetValue(ExifTag.PixelYDimension)?.Value ?? new Number(0);
                pictureHeight = (uint)h != 0 ? Convert.ToInt32((uint)h) : image.Height;
            }
            catch (FormatException)
            {
                pictureHeight = image.Height;
            }
            catch (OverflowException)
            {
                pictureHeight = image.Height;
            }
            catch (ArgumentNullException)
            {
                pictureHeight = image.Height;
            }
            catch (NullReferenceException)
            {
                pictureHeight = image.Height;
            }

            return pictureHeight;
        }
    }
}
