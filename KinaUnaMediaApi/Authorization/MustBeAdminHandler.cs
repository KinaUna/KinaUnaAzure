using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;

namespace KinaUnaMediaApi.Authorization
{
    public class MustBeAdminHandler : AuthorizationHandler<MustBeAdminRequirement>
    {
        
        public MustBeAdminHandler() //IGalleryRepository galleryRepository
        {
            //_galleryRepository = galleryRepository;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, MustBeAdminRequirement requirement)
        {
            var filterContext = context.Resource as AuthorizationFilterContext;
            if (filterContext == null)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            //var imageId = filterContext.RouteData.Values["id"].ToString();

            //if (!Guid.TryParse(imageId, out Guid imageIdAsGuid))
            //{
            //    context.Fail();
            //    return Task.CompletedTask;
            //}

            // var ownerId = context.User.Claims.FirstOrDefault(c => c.Type == "sub").Value;

            //if (!_galleryRepository.IsImageOwner(imageIdAsGuid, ownerId))
            //{
            //    context.Fail();
            //    return Task.CompletedTask;
            //}

            // all checks out
            context.Succeed(requirement);
            return Task.CompletedTask;

        }
    }
}
