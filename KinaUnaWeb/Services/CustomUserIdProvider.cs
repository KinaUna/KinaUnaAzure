using Microsoft.AspNetCore.SignalR;

namespace KinaUnaWeb.Services
{
    /// <summary>
    /// Sets the default User Id for SignalR to sub, which is the user's Id in the ApplicationUser database.
    /// </summary>
    public sealed class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User.FindFirst("sub")?.Value;
        }
    }
}
