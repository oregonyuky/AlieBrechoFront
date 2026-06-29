namespace AlieBrecho.Domain.Orders;

public sealed record CheckoutRequest
{
    public string CustomerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string? Complement { get; init; }
    public string Neighborhood { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostCode { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
