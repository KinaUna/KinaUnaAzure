using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using KinaUna.Data;
using Microsoft.Extensions.Hosting;

namespace KinaUnaProgenyApi.AuthorizationHandlers
{
    public class UserOrClientHandler(IHostEnvironment env) : AuthorizationHandler<UserOrClientRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOrClientRequirement requirement)
        {
            bool hasUser = context.User.HasClaim(c => c.Type == "sub") && (context.User.Identity?.IsAuthenticated ?? false);
            bool hasClient = context.User.HasClaim(c => c.Type == "client_id");

            List<string> allowedClients = AuthConstants.AllowedApiOnlyClients;
            if (env.IsDevelopment())
            {
                List<string> allowedDevelopmentClients = [];
                foreach (string client in AuthConstants.AllowedApiOnlyClients)
                {
                    allowedDevelopmentClients.Add(client + "local");
                }

                allowedClients = allowedDevelopmentClients;
            }

            if (env.IsStaging())
            {
                List<string> allowedStagingClients = [];
                foreach (string client in AuthConstants.AllowedApiOnlyClients)
                {
                    allowedStagingClients.Add(client + "azure");
                }

                allowedClients = allowedStagingClients;
            }

            // Check client_id value if it exists
            if (hasClient)
            {
                string clientId = context.User.FindFirst(c => c.Type == "client_id")?.Value;
                if (string.IsNullOrEmpty(clientId))
                {
                    hasClient = false; // Only allow the specific client_id
                }
                // Check if the client_id is one of the allowed values
                else if (!allowedClients.Contains(clientId))
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
