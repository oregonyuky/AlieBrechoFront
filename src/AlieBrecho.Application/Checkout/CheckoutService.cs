using AlieBrecho.Application.Abstractions;
using AlieBrecho.Application.Cart;
using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Checkout;

public sealed class CheckoutService(CartService cartService, IOrderGateway orderGateway)
{
    public async Task<CheckoutResult> CreateOrderAsync(CheckoutRequest request, CancellationToken cancellationToken)
    {
        var cart = await cartService.GetCartAsync(cancellationToken);
        if (cart.IsEmpty)
        {
            return CheckoutResult.Failed("Seu carrinho está vazio.");
        }

        var orderId = await orderGateway.CreateOrderAsync(request, cart, cancellationToken);
        await cartService.ClearAsync(cancellationToken);

        return CheckoutResult.Succeeded(orderId);
    }
}
