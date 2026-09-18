using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace UserService.Authorization
{
    public class SameUserHandler : AuthorizationHandler<SameUserRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SameUserHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SameUserRequirement requirement)
        {
            var routeId = _httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();

            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                               ?? context.User.FindFirst("sub")?.Value;

            if(routeId != null && userIdClaim != null && routeId == userIdClaim)
            {
                context.Succeed(requirement);
            }

            // If not matched, we simply don't call Succeed — this results in
            // authorization failing (403 Forbidden), without needing an explicit Fail() call
            return Task.CompletedTask;
        }
    }
}
