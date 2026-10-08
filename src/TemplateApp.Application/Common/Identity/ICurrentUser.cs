namespace TemplateApp.Application.Common.Identity;

public interface ICurrentUser
{
    /// <summary>Gets the authenticated user's identifier, or <c>null</c> outside an authenticated request.</summary>
    string? UserId { get; }
}
