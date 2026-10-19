namespace _CarRental.Domain.Constants;

public static class AuditActions
{
    public const string LoginSucceeded = "Auth.LoginSucceeded";
    public const string LoginFailed = "Auth.LoginFailed";
    public const string RefreshTokenReuseDetected = "Auth.RefreshTokenReuseDetected";
    public const string UserRegistered = "Users.Registered";
    public const string UserRoleAssigned = "Users.RoleAssigned";
    public const string EmployeeCreated = "Employees.Created";
    public const string EmployeeUpdated = "Employees.Updated";
    public const string EmployeeDeactivated = "Employees.Deactivated";
    public const string BranchCreated = "Branches.Created";
    public const string BranchUpdated = "Branches.Updated";
    public const string BranchDeleted = "Branches.Deleted";
    public const string BranchManagerChanged = "Branches.ManagerChanged";
    public const string UserActivated = "Users.Activated";
    public const string UserDeactivated = "Users.Deactivated";
    public const string UserRoleRemoved = "Users.RoleRemoved";
    public const string VehicleCreated = "Vehicles.Created";
    public const string VehicleUpdated = "Vehicles.Updated";
    public const string VehicleDeleted = "Vehicles.Deleted";
    public const string VehicleImageChanged = "Vehicles.ImageChanged";
    public const string LookupChanged = "Lookups.Changed";
    public const string ReservationCreated = "Reservations.Created";
    public const string ReservationStatusChanged = "Reservations.StatusChanged";
    public const string RentalContractCreated = "RentalContracts.Created";
    public const string RentalContractReturned = "RentalContracts.Returned";
    public const string PaymentCompleted = "Payments.Completed";
    public const string MaintenanceCreated = "Maintenance.Created";
    public const string MaintenanceUpdated = "Maintenance.Updated";
    public const string MaintenanceCompleted = "Maintenance.Completed";
    public const string MaintenanceCancelled = "Maintenance.Cancelled";
    public const string ReviewCreated = "Reviews.Created";
    public const string ReviewUpdated = "Reviews.Updated";
    public const string ReviewDeleted = "Reviews.Deleted";
}
