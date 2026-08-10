using ClientDashboard_API.Entities;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ClientDashboard_API.Authorization
{
    public class WorkoutOwnershipHandler : AuthorizationHandler<ResourceOwnerRequirement, Workout>
    {
        protected override Task HandleRequirementAsync
            (AuthorizationHandlerContext context, ResourceOwnerRequirement requirement, Workout resource)
        {
            var sub = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (sub is null || !int.TryParse(sub, out var callerId))
                return Task.CompletedTask;

            var isOwningTrainer = context.User.IsInRole("Trainer") && resource.Client!.TrainerId == callerId;

            if (isOwningTrainer)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}
