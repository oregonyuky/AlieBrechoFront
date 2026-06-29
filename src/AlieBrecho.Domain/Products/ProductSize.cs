namespace AlieBrecho.Domain.Products;

public sealed record ProductSize(
    string? Id,
    string? Size,
    decimal? Bust,
    decimal? Sleeve,
    decimal? Length);
