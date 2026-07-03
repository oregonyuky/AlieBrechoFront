namespace AlieBrecho.Domain.Marketing;

public sealed class DropConfig
{
    public string? Id { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public string? Subtitulo { get; init; }
    public DateTime DataLiberacao { get; init; }
    public bool Ativo { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
