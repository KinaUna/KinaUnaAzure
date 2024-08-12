using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Azure.NotificationHubs;

namespace KinaUnaProgenyApi.Services;

public interface IAzureNotifications
{
    /// <summary>
    /// Interface for dependency injection of the AzureNotifications class, used to manage mobile notifications.
    /// </summary>
    NotificationHubClient Hub { get; set; }

    /// <summary>
    /// Sends a notification to all users with access to a TimeLineItem.
    /// For use when a TimeLineItem is created or updated.
    /// Also saves the notification in the database for later retrieval.
    /// </summary>
    /// <param name="title">The title of the notification.</param>
    /// <param name="message">The body/content of the message.</param>
    /// <param name="timeLineItem">The TimeLineItem the notification is for.</param>
    /// <param name="iconLink">Link to the image used as icon when the notification is displayed in the notification history.</param>
    Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "");
}