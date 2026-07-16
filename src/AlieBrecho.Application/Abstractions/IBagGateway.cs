using AlieBrecho.Domain.Bags;
using AlieBrecho.Domain.Orders;

namespace AlieBrecho.Application.Abstractions;

public interface IBagGateway
{
    Task<BagCheckoutResult?> CreateBagCheckoutAsync(
        CheckoutRequest request,
        AlieBrecho.Domain.Orders.Cart cart,
        CancellationToken cancellationToken);

    Task<BagSummary?> GetActiveBagAsync(
        string customerId,
        CancellationToken cancellationToken);

    Task<BagSummary?> GetBagAsync(
        string bagId,
        CancellationToken cancellationToken);

    Task<BagFinalizeResult?> FinalizeBagAsync(
        string bagId,
        CancellationToken cancellationToken);
}

public sealed record BagCheckoutResult(
    string? BagId,
    string? PaymentUrl,
    string? PixQrCode,
    string? PixCode,
    string? PaymentId);

public sealed record BagFinalizeResult(
    string? BagId,
    string? Status,
    decimal? ShippingCost,
    decimal? TotalAmount);
