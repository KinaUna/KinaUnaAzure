﻿namespace KinaUna.Data.Extensions
{
    public static class FileContentTypeHelpers
    {
        public static string GetContentTypeString(string value)
        {
            
            string contentType = "image/jpeg";
            if (value.EndsWith(".jpg", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/jpeg";
            }
            else if (value.EndsWith(".jpeg", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/jpeg";
            }
            else
            if (value.EndsWith(".png", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/png";
            }
            else if (value.EndsWith(".gif", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/gif";
            }
            else if (value.EndsWith(".bmp", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/bmp";
            }
            else if (value.EndsWith(".tiff", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/tiff";
            }
            else if (value.EndsWith(".webp", System.StringComparison.CurrentCultureIgnoreCase))
            {
                contentType = "image/webp";
            }

            return contentType;
        }
    }
}