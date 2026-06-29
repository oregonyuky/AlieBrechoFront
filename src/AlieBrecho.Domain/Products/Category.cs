namespace AlieBrecho.Domain.Products;

public sealed record Category(
    string? Id,
    string Name,
    string? Description,
    bool IsActive);
