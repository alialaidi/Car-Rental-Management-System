namespace _CarRental.Application.Common.Interfaces;

public interface ICurrentUser
{
    int UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
    Task<int> GetCustomerIdAsync();
    Task<int> GetEmployeeIdAsync();
    Task<int> GetBranchIdAsync();
}
