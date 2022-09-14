using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extensions for Picture data.
    /// </summary>
    public static class PictureExtensions
    {
        /// <summary>
        /// Replaces null strings with empty string.
        /// </summary>
        /// <param name="picture">Picture: The picture object to update.</param>
        
        public static void RemoveNullStrings(this Picture picture)
        {
            if (picture != null)
            {
                if (picture.Altitude == null)
                {
                    picture.Altitude = "";
                }

                if (picture.Tags == null)
                {
                    picture.Tags = "";
                }

                if (picture.Author == null)
                {
                    picture.Author = "";
                }

                if (picture.Latitude == null)
                {
                    picture.Latitude = "";
                }

                if (picture.Location == null)
                {
                    picture.Location = "";
                }

                if (picture.Longtitude == null)
                {
                    picture.Longtitude = "";
                }

                if (picture.Owners == null)
                {
                    picture.Owners = "";
                }

                if (picture.PictureLink1200 == null)
                {
                    picture.PictureLink1200 = "";
                }

                if (picture.PictureLink == null)
                {
                    picture.PictureLink = "";
                }

                if (picture.PictureLink600 == null)
                {
                    picture.PictureLink600 = "";
                }

                if (picture.TimeZone == null)
                {
                    picture.TimeZone = "";
                }
            }
        }

    }
}
