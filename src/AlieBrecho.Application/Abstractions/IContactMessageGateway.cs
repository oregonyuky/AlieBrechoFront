using AlieBrecho.Domain.Contact;

namespace AlieBrecho.Application.Abstractions;

public interface IContactMessageGateway
{
    Task SendAsync(ContactMessageRequest request, CancellationToken cancellationToken);
}
