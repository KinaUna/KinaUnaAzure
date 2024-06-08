using KinaUna.Data.Models;

namespace KinaUna.Data.Extensions
{
    public static class WebNotificationExtensions
    {
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
