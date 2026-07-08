using AlieBrecho.Application.Abstractions;
using AlieBrecho.Application.Cart;
using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Checkout;

public sealed class CheckoutService(
    CartService cartService,
    IOrderGateway orderGateway,
    IBagGateway bagGateway)
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

        if (IsBagDelivery(request.DeliveryMode))
        {
            return await CreateBagCheckoutAsync(request, cart, cancellationToken);
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

    private async Task<CheckoutResult> CreateBagCheckoutAsync(
        CheckoutRequest request,
        Domain.Orders.Cart cart,
        CancellationToken cancellationToken)
    {
        var bag = await bagGateway.CreateBagCheckoutAsync(request, cart, cancellationToken);
        if (string.IsNullOrWhiteSpace(bag?.BagId))
        {
            return CheckoutResult.Failed("A API nao retornou a sacolinha.");
        }

        if (string.IsNullOrWhiteSpace(bag.PaymentUrl) &&
            string.IsNullOrWhiteSpace(bag.PixQrCode) &&
            string.IsNullOrWhiteSpace(bag.PixCode))
        {
            return CheckoutResult.Failed("A API nao retornou dados Pix da sacolinha.");
        }

        await cartService.ClearAsync(cancellationToken);

        return CheckoutResult.Succeeded(bag.BagId, bag.PaymentUrl, bag.PixQrCode, bag.PixCode, bag.PaymentId);
    }

    private static bool IsValidPaymentMethod(string? paymentMethod)
    {
        return string.Equals(paymentMethod, "pix", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBagDelivery(string? deliveryMode)
    {
        return string.Equals(deliveryMode, "bag", StringComparison.OrdinalIgnoreCase);
    }
}
