using AlieBrecho.Domain.Products;

namespace AlieBrecho.Application.Abstractions;

public interface IProductCatalogGateway
{
    Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken);
    Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken);
}
