using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Cart;

public sealed class CartService(ICartStore cartStore, IProductCatalogGateway productCatalogGateway)
{
    public async Task<AlieBrecho.Domain.Orders.Cart> GetCartAsync(CancellationToken cancellationToken)
    {
        var items = await cartStore.GetItemsAsync(cancellationToken);
        var products = await productCatalogGateway.GetProductsAsync(cancellationToken);
        var cartItems = products
            .Where(product => product.Id is not null && items.ContainsKey(product.Id))
            .Select(product => new CartItem(product, Math.Max(1, items[product.Id!])))
            .ToList();

        return new AlieBrecho.Domain.Orders.Cart(cartItems);
    }

    public async Task AddAsync(string productId, CancellationToken cancellationToken)
    {
        var items = new Dictionary<string, int>(await cartStore.GetItemsAsync(cancellationToken));
        items[productId] = 1;

        await cartStore.SaveItemsAsync(items, cancellationToken);
    }

    public async Task RemoveAsync(string productId, CancellationToken cancellationToken)
    {
        var items = new Dictionary<string, int>(await cartStore.GetItemsAsync(cancellationToken));
        items.Remove(productId);
        await cartStore.SaveItemsAsync(items, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken)
    {
        return cartStore.ClearAsync(cancellationToken);
    }
}
