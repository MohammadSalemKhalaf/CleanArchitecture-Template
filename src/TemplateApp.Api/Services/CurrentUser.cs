using TemplateApp.Application.Common.Interfaces;

namespace TemplateApp.Api.Services;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : IUser
{
    // Inbound claim mapping is disabled, so the JWT "sub" claim keeps its name.
    public string? Id => httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
}
