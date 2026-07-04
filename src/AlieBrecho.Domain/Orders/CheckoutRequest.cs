namespace AlieBrecho.Domain.Orders;

public sealed record CheckoutRequest
{
    public string? CustomerId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Cpf { get; init; }
    public string CustomerName => string.Join(' ', new[] { FirstName, LastName }
        .Where(part => !string.IsNullOrWhiteSpace(part))
        .Select(part => part.Trim()));
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string? Complement { get; init; }
    public string Neighborhood { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostCode { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = "pix";
    public string? Notes { get; init; }
}
