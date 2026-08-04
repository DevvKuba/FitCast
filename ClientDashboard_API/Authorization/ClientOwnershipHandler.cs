using ClientDashboard_API.Entities;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace ClientDashboard_API.Authorization
{
    public class ClientOwnershipHandler : AuthorizationHandler<ResourceOwnerRequirement, Client>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, ResourceOwnerRequirement requirement, Client resource)
        {
            var sub = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (sub is null || !int.TryParse(sub, out var callerId))
                return Task.CompletedTask;

            var isOwningTrainer = context.User.IsInRole("Trainer") && resource.TrainerId == callerId;
            var isSelf = context.User.IsInRole("Client") && resource.Id == callerId;

            if (isOwningTrainer || isSelf)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }

}
