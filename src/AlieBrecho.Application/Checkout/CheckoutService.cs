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
            return CheckoutResult.Failed("Seu carrinho esta vazio.");
        }

        if (!IsValidPaymentMethod(request.PaymentMethod))
        {
            return CheckoutResult.Failed("Escolha Pix.");
        }

        var orderId = await orderGateway.CreateOrderAsync(request, cart, cancellationToken);
        if (string.IsNullOrWhiteSpace(orderId))
        {
            return CheckoutResult.Failed("A API nao retornou o numero do pedido.");
        }

        var payment = await orderGateway.CreateMercadoPagoPixPaymentAsync(
            orderId,
            request,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(payment.PaymentUrl) &&
            string.IsNullOrWhiteSpace(payment.PixQrCode) &&
            string.IsNullOrWhiteSpace(payment.PixCode))
        {
            return CheckoutResult.Failed("A API nao retornou dados Pix.");
        }

        await cartService.ClearAsync(cancellationToken);

        return CheckoutResult.Succeeded(orderId, payment.PaymentUrl, payment.PixQrCode, payment.PixCode, payment.PaymentId);
    }

    private static bool IsValidPaymentMethod(string? paymentMethod)
    {
        return string.Equals(paymentMethod, "pix", StringComparison.OrdinalIgnoreCase);
    }
}
