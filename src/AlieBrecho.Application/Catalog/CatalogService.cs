using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Products;

namespace AlieBrecho.Application.Catalog;

public sealed class CatalogService(IProductCatalogGateway gateway)
{
    public async Task<CatalogView> GetCatalogAsync(string? categoryId, CancellationToken cancellationToken)
    {
        var productsTask = gateway.GetProductsAsync(cancellationToken);
        var categoriesTask = gateway.GetCategoriesAsync(cancellationToken);

        await Task.WhenAll(productsTask, categoriesTask);

        var products = await productsTask;
        var categories = await categoriesTask;

        var visibleProducts = products
            .Where(product => product.ProductAvailable)
            .Where(product => string.IsNullOrWhiteSpace(categoryId) || product.CategoryId == categoryId)
            .OrderBy(product => product.Name)
            .ToList();

        return new CatalogView(visibleProducts, categories, categoryId);
    }

    public Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken)
    {
        return gateway.GetProductAsync(productId, cancellationToken);
    }
}
