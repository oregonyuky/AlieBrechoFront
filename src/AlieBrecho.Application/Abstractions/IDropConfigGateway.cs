using AlieBrecho.Domain.Marketing;

namespace AlieBrecho.Application.Abstractions;

public interface IDropConfigGateway
{
    Task<DropConfig?> GetActiveDropConfigAsync(CancellationToken cancellationToken);
}
