using AlieBrecho.Domain.Orders;
using AlieBrecho.Domain.Bags;

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

    Task<IReadOnlyList<OrderSummary>> GetOrdersByCustomerAsync(
        string customerId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BagItemSummary>> GetOrderItemsAsync(
        string orderId,
        CancellationToken cancellationToken);

    Task<AutomaticShippingQuote> CalculateAutomaticShippingAsync(
        string postCode,
        AlieBrecho.Domain.Orders.Cart cart,
        CancellationToken cancellationToken);
}

public sealed record AutomaticShippingQuote(bool Success, string? Message, decimal ShippingCost,
    string? PackageName, int OccupationPoints, int CapacityPoints, string? CarrierName);

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
    string PaymentMethod,
    IReadOnlyList<BagItemSummary> Items);
