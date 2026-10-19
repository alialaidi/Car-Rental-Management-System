using _CarRental.Application.Common.Interfaces;
using _CarRental.Application.Services;
using _CarRental.Domain.Entities;
using _CarRental.Domain.Exceptions;
using _CarRental.Domain.Interfaces.Repositories;
using Xunit;

namespace _CarRental.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task BranchManagerIsForcedToOwnBranch()
    {
        var reports = new FakeReportRepository();
        var service = new ReportService(new FakeUnitOfWork(reports), new FakeCurrentUser("BranchManager", 7));

        await service.GetFleetStatusAsync(null);

        Assert.Equal(7, reports.LastBranchId);
    }

    [Fact]
    public async Task BranchManagerCannotSelectAnotherBranch()
    {
        var service = new ReportService(new FakeUnitOfWork(new FakeReportRepository()), new FakeCurrentUser("BranchManager", 7));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetFleetStatusAsync(8));
    }

    [Fact]
    public async Task EmployeeCannotViewReports()
    {
        var service = new ReportService(null!, new FakeCurrentUser("Employee", 7));

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetFleetStatusAsync(null));
    }

    private sealed class FakeCurrentUser(string role, int branchId) : ICurrentUser
    {
        public int UserId => 1;
        public IReadOnlyCollection<string> Roles => [role];
        public bool IsInRole(string value) => role == value;
        public Task<int> GetCustomerIdAsync() => throw new NotSupportedException();
        public Task<int> GetEmployeeIdAsync() => throw new NotSupportedException();
        public Task<int> GetBranchIdAsync() => Task.FromResult(branchId);
    }

    private sealed class FakeReportRepository : IReportRepository
    {
        public int? LastBranchId { get; private set; }
        public Task<IReadOnlyList<FleetStatusReportRow>> GetFleetStatusAsync(int? branchId)
        { LastBranchId = branchId; return Task.FromResult<IReadOnlyList<FleetStatusReportRow>>([]); }
        public Task<IReadOnlyList<RevenueReportRow>> GetRevenueAsync(DateOnly from, DateOnly to, int? branchId) => Empty<RevenueReportRow>();
        public Task<IReadOnlyList<TopVehicleReportRow>> GetTopVehiclesAsync(DateOnly from, DateOnly to, int? branchId, int take) => Empty<TopVehicleReportRow>();
        public Task<IReadOnlyList<MaintenanceCostReportRow>> GetMaintenanceCostAsync(DateOnly from, DateOnly to, int? branchId) => Empty<MaintenanceCostReportRow>();
        public Task<IReadOnlyList<PaymentSummaryReportRow>> GetPaymentSummaryAsync(DateOnly from, DateOnly to, int? branchId) => Empty<PaymentSummaryReportRow>();
        private static Task<IReadOnlyList<T>> Empty<T>() => Task.FromResult<IReadOnlyList<T>>([]);
    }

    private sealed class FakeUnitOfWork(IReportRepository reports) : IUnitOfWork
    {
        public IReportRepository Reports => reports;
        public IUserRepository Users => throw new NotSupportedException();
        public ICustomerRepository Customers => throw new NotSupportedException();
        public IRefreshTokenRepository RefreshTokens => throw new NotSupportedException();
        public IBranchRepository Branches => throw new NotSupportedException();
        public IEmployeeRepository Employees => throw new NotSupportedException();
        public IVehicleRepository Vehicles => throw new NotSupportedException();
        public IVehicleImageRepository VehicleImages => throw new NotSupportedException();
        public ILookupRepository Lookups => throw new NotSupportedException();
        public IReservationRepository Reservations => throw new NotSupportedException();
        public IRentalContractRepository Contracts => throw new NotSupportedException();
        public IPaymentRepository Payments => throw new NotSupportedException();
        public IMaintenanceRepository Maintenance => throw new NotSupportedException();
        public IReviewRepository Reviews => throw new NotSupportedException();
        public Task BeginTransactionAsync() => throw new NotSupportedException();
        public Task CommitAsync() => throw new NotSupportedException();
        public Task RollbackAsync() => throw new NotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
