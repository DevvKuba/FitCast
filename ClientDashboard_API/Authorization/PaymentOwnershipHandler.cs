using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Resources;
using System.Security.Claims;

namespace ClientDashboard_API.Authorization
{
    public class PaymentOwnershipHandler : AuthorizationHandler<ResourceOwnerRequirement, Payment>
    {
        protected override Task HandleRequirementAsync
            (AuthorizationHandlerContext context, ResourceOwnerRequirement requirement, Payment resource)
        {
            var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (sub is null || !int.TryParse(sub, out var callerId))
                return Task.CompletedTask;
            
            var isOwningTrainer = context.User.IsInRole("Trainer") && resource.TrainerId == callerId;

            if (isOwningTrainer)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
