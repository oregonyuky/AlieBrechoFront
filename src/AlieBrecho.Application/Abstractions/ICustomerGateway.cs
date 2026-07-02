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
}
