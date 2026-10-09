namespace TemplateApp.Application.Common.Interfaces;

public interface IUser
{
    /// <summary>Gets the authenticated user's identifier, or <c>null</c> outside an authenticated request.</summary>
    string? Id { get; }
}
