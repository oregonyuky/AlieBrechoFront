namespace AlieBrecho.Domain.Products;

public sealed record Product
{
    public string? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CategoryId { get; init; }
    public decimal? UnitPrice { get; init; }
    public decimal? OldPrice { get; init; }
    public decimal? DiscountPercent { get; init; }
    public bool ProductAvailable { get; init; }
    public string? MainImageUrl { get; init; }
    public string? AltText { get; init; }
    public string? ShortDescription { get; init; }
    public string? LongDescription { get; init; }
    public IReadOnlyList<ProductSize> Sizes { get; init; } = [];

    public decimal DisplayPrice => UnitPrice ?? 0m;
}
