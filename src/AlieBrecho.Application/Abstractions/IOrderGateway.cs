using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Abstractions;

public interface IOrderGateway
{
    Task<string?> CreateOrderAsync(
        CheckoutRequest request,
        AlieBrecho.Domain.Orders.Cart cart,
        CancellationToken cancellationToken);

    Task<PaymentCheckoutResult> CreateInfinitePayCheckoutAsync(
        string orderId,
        string paymentMethod,
        CancellationToken cancellationToken);
}

public sealed record PaymentCheckoutResult(
    string? PaymentUrl,
    string? PixQrCode,
    string? PixCode);
