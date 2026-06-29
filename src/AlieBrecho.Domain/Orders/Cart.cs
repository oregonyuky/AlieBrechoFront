namespace AlieBrecho.Domain.Orders;

public sealed record Cart(IReadOnlyList<CartItem> Items)
{
    public int ItemCount => Items.Sum(item => item.Quantity);
    public decimal Subtotal => Items.Sum(item => item.Total);
    public bool IsEmpty => Items.Count == 0;
}
