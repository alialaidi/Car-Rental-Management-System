namespace _CarRental.Domain.Exceptions;

public sealed class ForbiddenException(string message) : DomainException("FORBIDDEN", message);
