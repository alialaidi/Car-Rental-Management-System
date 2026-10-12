using _CarRental.Domain.Entities;

namespace _CarRental.Domain.Interfaces.Repositories;

public interface IRentalContractRepository
{
    Task<int> CreateFromReservationAsync(RentalContract contract);
    Task CompleteAsync(RentalContract contract, int returnBranchId, bool requiresMaintenance);
    Task<RentalContract?> GetByIdAsync(int contractId);
    Task<(IReadOnlyList<RentalContract> Items, int TotalCount)> ListAsync(int? branchId, int page, int pageSize);
    Task<(IReadOnlyList<RentalContract> Items, int TotalCount)> ListByCustomerAsync(int customerId, int page, int pageSize);
}
