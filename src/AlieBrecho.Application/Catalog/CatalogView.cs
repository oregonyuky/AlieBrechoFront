using AlieBrecho.Domain.Products;

namespace AlieBrecho.Application.Catalog;

public sealed record CatalogView(
    IReadOnlyList<Product> Products,
    IReadOnlyList<Category> Categories,
    string? SelectedCategoryId);
