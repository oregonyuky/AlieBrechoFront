using System.Text.Json;
using AlieBrecho.Application.Abstractions;

namespace AlieBrecho.Presentation.Infrastructure;

public sealed class SessionCartStore(IHttpContextAccessor httpContextAccessor) : ICartStore
{
    public const string SessionKey = "aliebrecho.cart";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyDictionary<string, int>> GetItemsAsync(CancellationToken cancellationToken)
    {
        var json = GetSession().GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult<IReadOnlyDictionary<string, int>>(new Dictionary<string, int>());
        }

        var items = JsonSerializer.Deserialize<Dictionary<string, int>>(json, JsonOptions)
            ?? new Dictionary<string, int>();

        return Task.FromResult<IReadOnlyDictionary<string, int>>(items);
    }

    public Task SaveItemsAsync(IReadOnlyDictionary<string, int> items, CancellationToken cancellationToken)
    {
        GetSession().SetString(SessionKey, JsonSerializer.Serialize(items, JsonOptions));
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        GetSession().Remove(SessionKey);
        return Task.CompletedTask;
    }

    private ISession GetSession()
    {
        return httpContextAccessor.HttpContext?.Session
            ?? throw new InvalidOperationException("A sessao HTTP nao esta disponivel.");
    }
}
