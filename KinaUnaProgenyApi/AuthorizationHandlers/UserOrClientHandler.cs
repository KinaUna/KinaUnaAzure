using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace KinaUnaProgenyApi.AuthorizationHandlers
{
    public class UserOrClientHandler : AuthorizationHandler<UserOrClientRequirement>
    {
        private readonly List<string> _allowedClients =
        [
            "kinaunawebclient",
            "kinaunawebclientlocal",
            "kinaunawebclientazure",
            "kinaunaauthclient",
            "kinaunaauthclientlocal",
            "kinaunaauthclientazure"
        ];

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOrClientRequirement requirement)
        {
            bool hasUser = context.User.HasClaim(c => c.Type == "sub") && (context.User.Identity?.IsAuthenticated ?? false);
            bool hasClient = context.User.HasClaim(c => c.Type == "client_id");

            // Check client_id value if it exists
            if (hasClient)
            {
                string clientId = context.User.FindFirst(c => c.Type == "client_id")?.Value;
                if (string.IsNullOrEmpty(clientId))
                {
                    hasClient = false; // Only allow the specific client_id
                }
                // Check if the client_id is one of the allowed values
                else if (!_allowedClients.Contains(clientId))
                {
                    hasClient = false; // Only allow the specific client_id
                }
            }

            if (hasUser || hasClient)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
