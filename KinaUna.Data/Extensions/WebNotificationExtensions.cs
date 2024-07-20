using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    /// <summary>
    /// Extension methods for the WebNotification class.
    /// </summary>
    public static class WebNotificationExtensions
    {
        /// <summary>
        /// Provides a URL for the icon image for the notification.
        /// </summary>
        /// <param name="webNotification"></param>
        /// <returns>string: The URL for the icon image.</returns>
        public static string GetIconUrl(this WebNotification webNotification)
        {
            if (webNotification == null || webNotification.Icon == null)
            {
                return Constants.ProfilePictureUrl;
            }

            if (webNotification.Icon.StartsWith("http", System.StringComparison.CurrentCultureIgnoreCase) || webNotification.Icon.StartsWith('/'))
            {
                return webNotification.Icon;
            }

            string pictureUrl = "/Account/ProfilePictureFromBlob/" + webNotification.Icon;

            return pictureUrl;
        }
    }
}
