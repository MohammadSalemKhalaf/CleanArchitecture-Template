using TemplateApp.Application.Common.Identity;

namespace TemplateApp.Api.Http;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // Inbound claim mapping is disabled, so the JWT "sub" claim keeps its name.
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
}
