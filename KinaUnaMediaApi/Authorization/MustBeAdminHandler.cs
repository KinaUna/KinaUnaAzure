﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace KinaUnaMediaApi.Authorization
{
    public class MustBeAdminHandler : AuthorizationHandler<MustBeAdminRequirement>
    {
        
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, MustBeAdminRequirement requirement)
        {
            AuthorizationFilterContext filterContext = context.Resource as AuthorizationFilterContext;
            if (filterContext == null)
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
