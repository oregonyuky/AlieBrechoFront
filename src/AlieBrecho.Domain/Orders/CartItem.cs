using AlieBrecho.Domain.Products;

namespace AlieBrecho.Domain.Orders;

public sealed record CartItem(Product Product, int Quantity)
{
    public decimal Total => Product.DisplayPrice * Quantity;
}
