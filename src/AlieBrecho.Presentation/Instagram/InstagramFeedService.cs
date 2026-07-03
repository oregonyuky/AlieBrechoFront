using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace AlieBrecho.Presentation.Instagram;

public sealed class InstagramFeedService(HttpClient httpClient, IOptions<InstagramOptions> options)
{
    private readonly InstagramOptions _options = options.Value;

    public async Task<IReadOnlyList<InstagramPost>> GetLatestPostsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken) ||
            string.IsNullOrWhiteSpace(_options.InstagramBusinessAccountId))
        {
            return GetFallbackPosts();
        }

        try
        {
            var limit = Math.Clamp(_options.Limit, 1, 6);
            var url = BuildMediaUrl(limit);
            var response = await httpClient.GetFromJsonAsync<InstagramMediaResponse>(url, cancellationToken);
            var posts = response?.Data?
                .Where(item => !string.IsNullOrWhiteSpace(item.Permalink))
                .Select(MapPost)
                .Where(post => !string.IsNullOrWhiteSpace(post.ImageUrl))
                .Take(6)
                .ToList();

            return posts is { Count: > 0 } ? posts : GetFallbackPosts();
        }
        catch (HttpRequestException)
        {
            return GetFallbackPosts();
        }
        catch (TaskCanceledException)
        {
            return GetFallbackPosts();
        }
    }

    private string BuildMediaUrl(int limit)
    {
        var accountId = Uri.EscapeDataString(_options.InstagramBusinessAccountId ?? string.Empty);
        var fields = Uri.EscapeDataString("id,caption,media_type,media_url,permalink,thumbnail_url,timestamp,like_count,comments_count");
        var token = Uri.EscapeDataString(_options.AccessToken ?? string.Empty);

        return $"{_options.ApiBaseUrl.TrimEnd('/')}/{accountId}/media?fields={fields}&limit={limit}&access_token={token}";
    }

    private InstagramPost MapPost(InstagramMediaItem item)
    {
        return new InstagramPost
        {
            Id = item.Id ?? string.Empty,
            ImageUrl = string.IsNullOrWhiteSpace(item.ThumbnailUrl) ? item.MediaUrl ?? string.Empty : item.ThumbnailUrl,
            Permalink = item.Permalink ?? "https://www.instagram.com/alie.brecho/",
            Caption = item.Caption,
            LikeCount = item.LikeCount ?? 0,
            CommentsCount = item.CommentsCount ?? 0
        };
    }

    private IReadOnlyList<InstagramPost> GetFallbackPosts()
    {
        if (_options.FallbackPosts.Count > 0)
        {
            return _options.FallbackPosts.Take(6).Select(post => post with { IsFallback = true }).ToList();
        }

        return Enumerable.Range(1, 6)
            .Select(index => new InstagramPost
            {
                Id = $"fallback-{index}",
                ImageUrl = $"/images/instagram/fallback-{index}.svg",
                Permalink = "https://www.instagram.com/alie.brecho/",
                Caption = "Alie Brecho",
                IsFallback = true
            })
            .ToList();
    }

    private sealed record InstagramMediaResponse
    {
        public List<InstagramMediaItem>? Data { get; init; }
    }

    private sealed record InstagramMediaItem
    {
        public string? Id { get; init; }
        public string? Caption { get; init; }
        [JsonPropertyName("media_url")]
        public string? MediaUrl { get; init; }
        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; init; }
        public string? Permalink { get; init; }
        [JsonPropertyName("like_count")]
        public int? LikeCount { get; init; }
        [JsonPropertyName("comments_count")]
        public int? CommentsCount { get; init; }
    }
}
