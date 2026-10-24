using System.Web.Mvc;

namespace HealthTrack3.Helpers
{
    /// <summary>
    /// Restricts a controller/action to the given roles.
    /// Logged-in users with the wrong role are sent to the Access Denied page;
    /// anonymous users are sent to the Login page.
    /// </summary>
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        public RoleAuthorizeAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectResult("~/Home/AccessDenied");
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}