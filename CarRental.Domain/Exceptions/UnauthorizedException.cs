namespace _CarRental.Domain.Exceptions;

public sealed class UnauthorizedException(string message) : DomainException("UNAUTHORIZED", message);
