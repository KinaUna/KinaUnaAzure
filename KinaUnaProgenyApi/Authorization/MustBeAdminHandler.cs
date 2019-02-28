using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KinaUnaProgenyApi.Authorization
{
    public class MustBeAdminHandler : AuthorizationHandler<MustBeAdminRequirement>
    {
       protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, MustBeAdminRequirement requirement)
        {
            var filterContext = context.Resource as AuthorizationFilterContext;
            if (filterContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var imageId = filterContext.RouteData.Values["id"].ToString();

            // ReSharper disable once UnusedVariable
            if (!Guid.TryParse(imageId, out Guid imageIdAsGuid))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // all checks out
            context.Succeed(requirement);
            return Task.CompletedTask;

        }
    }
}
