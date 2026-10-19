using _CarRental.Domain.Interfaces.Repositories;
using _CarRental.Infrastructure.Persistence.Repositories;

namespace _CarRental.Infrastructure.Persistence.UnitOfWork;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbSession _session;
    private readonly SqlExecutor _executor;
    private UserRepository? _users;
    private CustomerRepository? _customers;
    private RefreshTokenRepository? _refreshTokens;
    private BranchRepository? _branches;
    private EmployeeRepository? _employees;
    private VehicleRepository? _vehicles;
    private VehicleImageRepository? _vehicleImages;
    private LookupRepository? _lookups;
    private ReservationRepository? _reservations;
    private RentalContractRepository? _contracts;
    private PaymentRepository? _payments;
    private MaintenanceRepository? _maintenance;
    private ReviewRepository? _reviews;
    private ReportRepository? _reports;


    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _session = new DbSession(connectionFactory);
        _executor = new SqlExecutor(_session);
    }

    public IUserRepository Users => _users ??= new UserRepository(_executor);
    public ICustomerRepository Customers => _customers ??= new CustomerRepository(_executor);
    public IRefreshTokenRepository RefreshTokens => _refreshTokens ??= new RefreshTokenRepository(_executor);
    public IBranchRepository Branches => _branches ??= new BranchRepository(_executor);
    public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(_executor);
    public IVehicleRepository Vehicles => _vehicles ??= new VehicleRepository(_executor);
    public IVehicleImageRepository VehicleImages => _vehicleImages ??= new VehicleImageRepository(_executor);
    public ILookupRepository Lookups => _lookups ??= new LookupRepository(_executor);
    public IReservationRepository Reservations => _reservations ??= new ReservationRepository(_executor);
    public IRentalContractRepository Contracts => _contracts ??= new RentalContractRepository(_executor);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_executor);
    public IMaintenanceRepository Maintenance => _maintenance ??= new MaintenanceRepository(_executor);
    public IReviewRepository Reviews => _reviews ??= new ReviewRepository(_executor);
    public IReportRepository Reports => _reports ??= new ReportRepository(_executor);

    public Task BeginTransactionAsync() => _session.BeginTransactionAsync();
    public Task CommitAsync() => _session.CommitAsync();
    public Task RollbackAsync() => _session.RollbackAsync();
    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
