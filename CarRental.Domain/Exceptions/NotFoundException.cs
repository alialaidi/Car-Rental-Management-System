namespace _CarRental.Domain.Exceptions;

public sealed class NotFoundException(string message) : DomainException("NOT_FOUND", message);
