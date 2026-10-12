namespace _CarRental.Domain.Exceptions;

public sealed class ConflictException : DomainException
{
    public ConflictException(string message) : this("CONFLICT", message) { }

    public ConflictException(string code, string message) : base(code, message) { }
}
