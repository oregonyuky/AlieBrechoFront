namespace AlieBrecho.Application.Abstractions;

public interface ICartStore
{
    Task<IReadOnlyDictionary<string, int>> GetItemsAsync(CancellationToken cancellationToken);
    Task SaveItemsAsync(IReadOnlyDictionary<string, int> items, CancellationToken cancellationToken);
    Task ClearAsync(CancellationToken cancellationToken);
}
