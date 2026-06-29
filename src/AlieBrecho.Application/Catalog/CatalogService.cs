using AlieBrecho.Application.Abstractions;
using AlieBrecho.Domain.Products;

namespace AlieBrecho.Application.Catalog;

public sealed class CatalogService(IProductCatalogGateway gateway)
{
    public async Task<CatalogView> GetCatalogAsync(string? categoryId, CancellationToken cancellationToken)
    {
        var products = await gateway.GetProductsAsync(cancellationToken);
        var categories = await gateway.GetCategoriesAsync(cancellationToken);

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
