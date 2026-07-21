namespace AlieBrecho.Application.Abstractions;

public interface ISiteSettingsGateway
{
    Task<SiteSettings?> GetAsync(CancellationToken cancellationToken);
}

public sealed record SiteSettings(string? HeroImageUrl);
