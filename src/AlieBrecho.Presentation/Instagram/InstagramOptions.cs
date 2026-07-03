namespace AlieBrecho.Presentation.Instagram;

public sealed class InstagramOptions
{
    public const string SectionName = "Instagram";

    public string ApiBaseUrl { get; init; } = "https://graph.facebook.com/v20.0";
    public string? InstagramBusinessAccountId { get; init; }
    public string? AccessToken { get; init; }
    public int Limit { get; init; } = 6;
    public List<InstagramPost> FallbackPosts { get; init; } = [];
}
