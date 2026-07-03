namespace AlieBrecho.Presentation.Instagram;

public sealed record InstagramPost
{
    public string Id { get; init; } = string.Empty;
    public string ImageUrl { get; init; } = string.Empty;
    public string Permalink { get; init; } = "https://www.instagram.com/alie.brecho/";
    public string? Caption { get; init; }
    public int LikeCount { get; init; }
    public int CommentsCount { get; init; }
    public bool IsFallback { get; init; }
}
