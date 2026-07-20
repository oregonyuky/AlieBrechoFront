namespace AlieBrecho.Domain.Contact;

public sealed record ContactMessageRequest(
    string Name,
    string Email,
    string Phone,
    string Subject,
    string Message);
