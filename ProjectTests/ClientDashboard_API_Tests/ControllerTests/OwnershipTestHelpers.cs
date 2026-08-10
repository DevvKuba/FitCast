using System.Security.Claims;
using ClientDashboard_API.Authorization;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API_Tests.ControllerTests
{
    // Runs the real IAuthorizationHandler implementations (ClientOwnershipHandler, PaymentOwnershipHandler,
    // WorkoutOwnershipHandler) instead of a hand-simulated fake, so tests exercise the actual ownership rules.
    public class TestAuthorizationService(IEnumerable<IAuthorizationHandler> handlers) : IAuthorizationService
    {
        public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            var context = new AuthorizationHandlerContext(requirements, user, resource);
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(context);
            }
            return context.HasSucceeded ? AuthorizationResult.Success() : AuthorizationResult.Failed();
        }

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IAuthorizationRequirement requirement)
            => AuthorizeAsync(user, resource, [requirement]);

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => throw new NotImplementedException("Policy-based authorization isn't used by this codebase.");
    }

    public class FakeHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    public static class TestAuthHelpers
    {
        // NameIdentifier, not Sub: ASP.NET Core's JwtBearer handler rewrites the "sub" claim to
        // ClaimTypes.NameIdentifier by default (MapInboundClaims), so that's what a real validated
        // token's principal actually carries by the time CurrentUserAccessor/the ownership handlers see it.
        public static ClaimsPrincipal CreateUser(string role, int userId)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        // Builds a real IAuthorizationService (backed by the actual ownership handlers) and a real
        // ICurrentUserAccessor, both reading from the same ClaimsPrincipal so a test only sets identity once.
        public static (IAuthorizationService AuthorizationService, ICurrentUserAccessor CurrentUserAccessor, FakeHttpContextAccessor HttpContextAccessor) CreateAuthInfrastructure(
            params IAuthorizationHandler[] handlers)
        {
            var httpContextAccessor = new FakeHttpContextAccessor();
            var currentUserAccessor = new CurrentUserAccessor(httpContextAccessor);
            var authorizationService = new TestAuthorizationService(handlers);
            return (authorizationService, currentUserAccessor, httpContextAccessor);
        }

        // Mutates the existing HttpContext's User rather than replacing HttpContext itself, so anything already
        // holding a reference to it (a controller's ControllerContext) picks up the new identity automatically.
        public static void SetCurrentUser(FakeHttpContextAccessor httpContextAccessor, string role, int userId)
        {
            httpContextAccessor.HttpContext ??= new DefaultHttpContext();
            httpContextAccessor.HttpContext.User = CreateUser(role, userId);
        }

        public static void AttachHttpContext(ControllerBase controller, FakeHttpContextAccessor httpContextAccessor)
        {
            httpContextAccessor.HttpContext ??= new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContextAccessor.HttpContext };
        }
    }
}
