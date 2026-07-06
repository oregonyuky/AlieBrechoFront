using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Abstractions;

public interface IOrderGateway
{
    Task<string?> CreateOrderAsync(
        CheckoutRequest request,
        AlieBrecho.Domain.Orders.Cart cart,
        CancellationToken cancellationToken);

    Task<PaymentCheckoutResult> CreateMercadoPagoPixPaymentAsync(
        string orderId,
        CheckoutRequest request,
        CancellationToken cancellationToken);

    Task<PixPaymentStatusResult?> GetMercadoPagoPixPaymentStatusAsync(
        string paymentId,
        CancellationToken cancellationToken);

    Task<OrderSummary?> GetOrderSummaryAsync(
        string orderId,
        CancellationToken cancellationToken);
}

public sealed record PaymentCheckoutResult(
    string? PaymentUrl,
    string? PixQrCode,
    string? PixCode,
    string? PaymentId);

public sealed record PixPaymentStatusResult(
    string? PaymentId,
    string? Status,
    string? StatusDetail,
    string? OrderStatus);

public sealed record OrderSummary(
    string? OrderId,
    string? Status,
    decimal? TotalAmount,
    decimal? ShippingCost,
    decimal? AmountPaid,
    string PaymentMethod);
