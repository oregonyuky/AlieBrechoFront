namespace AlieBrecho.Application.Abstractions;

public interface ICustomerGateway
{
    Task CreateCustomerForRegistrationAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string confirmPassword,
        CancellationToken cancellationToken);

    Task<CustomerProfile?> GetCustomerProfileAsync(
        string customerId,
        CancellationToken cancellationToken);
}

public sealed record CustomerProfile
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Cpf { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Street { get; init; }
    public string? Number { get; init; }
    public string? Neighborhood { get; init; }
    public string? Complement { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
}
