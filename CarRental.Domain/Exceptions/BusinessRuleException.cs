namespace _CarRental.Domain.Exceptions;

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : this("BUSINESS_RULE_VIOLATION", message) { }

    public BusinessRuleException(string code, string message) : base(code, message) { }
}
