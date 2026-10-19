using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _CarRental.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        IUserRepository Users { get; }
        ICustomerRepository Customers { get; }
        IRefreshTokenRepository RefreshTokens { get; }
        IBranchRepository Branches { get; }
        IEmployeeRepository Employees { get; }
        IVehicleRepository Vehicles { get; }
        IVehicleImageRepository VehicleImages { get; }
        ILookupRepository Lookups { get; }
        IReservationRepository Reservations { get; }
        IRentalContractRepository Contracts { get; }
        IPaymentRepository Payments { get; }
        IMaintenanceRepository Maintenance { get; }
        IReviewRepository Reviews { get; }
        IReportRepository Reports { get; }

        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
