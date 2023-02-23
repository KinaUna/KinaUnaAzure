using System.Threading.Tasks;
using KinaUna.Data.Models;
using Microsoft.Azure.NotificationHubs;

namespace KinaUnaProgenyApi.Services;

public interface IAzureNotifications
{
    NotificationHubClient Hub { get; set; }
    Task ProgenyUpdateNotification(string title, string message, TimeLineItem timeLineItem, string iconLink = "");
}