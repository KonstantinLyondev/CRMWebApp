using System.Security.Claims;
using System.Threading.Tasks;
using CRMWebApp.Models;
using CRMWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace CRMWebApp.Authorization
{
    public class RecordOwnerAuthorizationHandler<T>
: AuthorizationHandler<OperationAuthorizationRequirement, T>
where T : class, IOwnedEntity
    {
        private readonly IUserContext _ctx;

        public RecordOwnerAuthorizationHandler(IUserContext ctx)
        {
            _ctx = ctx;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OperationAuthorizationRequirement requirement,
            T resource)
        {
            var realUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = context.User.IsInRole("Admin");
            var isImpersonating = context.User.HasClaim("impersonating", "true");

            var effectiveUserId = isImpersonating ? _ctx.UserId : realUserId;

            bool createdByRealUser = resource.CreatedById == realUserId;
            bool ownedByEffectiveUser = resource.UserId == effectiveUserId;

            if (requirement.Name is nameof(Operations.Edit) or nameof(Operations.Delete))
            {
                if (isAdmin)
                {
                    if (isImpersonating)
                    {
                        if (ownedByEffectiveUser)
                            context.Succeed(requirement);
                    }
                    else
                    {
                        if (createdByRealUser)
                            context.Succeed(requirement);
                    }
                }
                else
                {
                    if (ownedByEffectiveUser) context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}