namespace AlieBrecho.Domain.Bags;

public sealed record BagSummary
{
    public string? Id { get; init; }
    public string? Status { get; init; }
    public DateTime? ExpirationDate { get; init; }
    public decimal TotalItemsValue { get; init; }
    public decimal? ShippingCost { get; init; }
    public int ItemCount { get; init; }
    public IReadOnlyList<BagItemSummary> Items { get; init; } = [];
}

public sealed record BagItemSummary
{
    public string? ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total => UnitPrice * Quantity;
}
