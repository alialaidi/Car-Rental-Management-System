-- Educational sample dataset for local development and demonstrations only.
-- Demo accounts use the public password: Demo123!
-- Never use these credentials outside a disposable/local environment.
-- Run after 01_Roles.sql and 02_Lookups.sql (or Seed/RunSeed.sql).
:on error exit
USE CarRentalDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @DemoPasswordHash NVARCHAR(255) = N'$2a$11$feq1Dh3aiBRb5XCr2L2EOumJ.90n3l2CJhUyiAvUqXq6wwjzCItdm';
DECLARE @AdminRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Admin');
DECLARE @ManagerRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'BranchManager');
DECLARE @EmployeeRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Employee');
DECLARE @CustomerRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Customer');

IF @AdminRoleId IS NULL OR @ManagerRoleId IS NULL OR @EmployeeRoleId IS NULL OR @CustomerRoleId IS NULL
    THROW 51010, 'Run Seed/01_Roles.sql before the educational demo seed.', 1;

-- Branches (preserve the baseline Head Office created by 03_BootstrapBranchAndAdmin.sql).
IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Name = N'Amman City Branch')
    INSERT dbo.Branches (Name, Address, City, Phone) VALUES (N'Amman City Branch', N'King Abdullah II Street', N'Amman', N'+962-6-555-0101');
IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Name = N'Irbid Branch')
    INSERT dbo.Branches (Name, Address, City, Phone) VALUES (N'Irbid Branch', N'University Street', N'Irbid', N'+962-2-555-0102');
IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE Name = N'Aqaba Branch')
    INSERT dbo.Branches (Name, Address, City, Phone) VALUES (N'Aqaba Branch', N'King Hussein Street', N'Aqaba', N'+962-3-555-0103');

-- Demo identities. Existing rows are left untouched, including their passwords.
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'demo.manager')
    INSERT dbo.Users (Username, Email, PasswordHash, PhoneNumber, IsActive) VALUES (N'demo.manager', N'manager@carrental.local', @DemoPasswordHash, N'+962790000101', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'demo.employee')
    INSERT dbo.Users (Username, Email, PasswordHash, PhoneNumber, IsActive) VALUES (N'demo.employee', N'employee@carrental.local', @DemoPasswordHash, N'+962790000102', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'demo.customer')
    INSERT dbo.Users (Username, Email, PasswordHash, PhoneNumber, IsActive) VALUES (N'demo.customer', N'customer@carrental.local', @DemoPasswordHash, N'+962790000103', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'demo.customer2')
    INSERT dbo.Users (Username, Email, PasswordHash, PhoneNumber, IsActive) VALUES (N'demo.customer2', N'customer2@carrental.local', @DemoPasswordHash, N'+962790000104', 1);

DECLARE @ManagerUserId INT = (SELECT UserId FROM dbo.Users WHERE Username = N'demo.manager');
DECLARE @EmployeeUserId INT = (SELECT UserId FROM dbo.Users WHERE Username = N'demo.employee');
DECLARE @CustomerUserId INT = (SELECT UserId FROM dbo.Users WHERE Username = N'demo.customer');
DECLARE @Customer2UserId INT = (SELECT UserId FROM dbo.Users WHERE Username = N'demo.customer2');
DECLARE @ManagerBranchId INT = (SELECT BranchId FROM dbo.Branches WHERE Name = N'Amman City Branch');
DECLARE @EmployeeBranchId INT = (SELECT BranchId FROM dbo.Branches WHERE Name = N'Irbid Branch');

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @ManagerUserId AND RoleId = @ManagerRoleId)
    INSERT dbo.UserRoles (UserId, RoleId) VALUES (@ManagerUserId, @ManagerRoleId);
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @EmployeeUserId AND RoleId = @EmployeeRoleId)
    INSERT dbo.UserRoles (UserId, RoleId) VALUES (@EmployeeUserId, @EmployeeRoleId);
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @CustomerUserId AND RoleId = @CustomerRoleId)
    INSERT dbo.UserRoles (UserId, RoleId) VALUES (@CustomerUserId, @CustomerRoleId);
IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @Customer2UserId AND RoleId = @CustomerRoleId)
    INSERT dbo.UserRoles (UserId, RoleId) VALUES (@Customer2UserId, @CustomerRoleId);

IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE UserId = @ManagerUserId)
    INSERT dbo.Employees (UserId, BranchId, Position, HireDate) VALUES (@ManagerUserId, @ManagerBranchId, N'Branch Manager', '2022-03-01');
IF NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE UserId = @EmployeeUserId)
    INSERT dbo.Employees (UserId, BranchId, Position, HireDate) VALUES (@EmployeeUserId, @EmployeeBranchId, N'Rental Officer', '2023-06-15');
DECLARE @ManagerEmployeeId INT = (SELECT EmployeeId FROM dbo.Employees WHERE UserId = @ManagerUserId);
DECLARE @EmployeeId INT = (SELECT EmployeeId FROM dbo.Employees WHERE UserId = @EmployeeUserId);
UPDATE dbo.Branches SET ManagerEmployeeId = @ManagerEmployeeId WHERE BranchId = @ManagerBranchId AND ManagerEmployeeId IS NULL;
UPDATE dbo.Branches SET ManagerEmployeeId = @EmployeeId WHERE BranchId = @EmployeeBranchId AND ManagerEmployeeId IS NULL;

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE UserId = @CustomerUserId)
    INSERT dbo.Customers (UserId, FullName, DrivingLicenseNumber, DrivingLicenseExpiry, Address) VALUES (@CustomerUserId, N'Layla Haddad', N'DEMO-JO-10001', '2030-12-31', N'Amman, Jordan');
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE UserId = @Customer2UserId)
    INSERT dbo.Customers (UserId, FullName, DrivingLicenseNumber, DrivingLicenseExpiry, Address) VALUES (@Customer2UserId, N'Omar Khalil', N'DEMO-JO-10002', '2031-06-30', N'Irbid, Jordan');
DECLARE @CustomerId INT = (SELECT CustomerId FROM dbo.Customers WHERE UserId = @CustomerUserId);
DECLARE @Customer2Id INT = (SELECT CustomerId FROM dbo.Customers WHERE UserId = @Customer2UserId);

DECLARE @EconomyId INT = (SELECT CategoryId FROM dbo.VehicleCategories WHERE Name = N'Economy');
DECLARE @SedanId INT = (SELECT CategoryId FROM dbo.VehicleCategories WHERE Name = N'Sedan');
DECLARE @SUVId INT = (SELECT CategoryId FROM dbo.VehicleCategories WHERE Name = N'SUV');
DECLARE @ElectricId INT = (SELECT FuelTypeId FROM dbo.FuelTypes WHERE Name = N'Electric');
DECLARE @PetrolId INT = (SELECT FuelTypeId FROM dbo.FuelTypes WHERE Name = N'Petrol');
DECLARE @HybridId INT = (SELECT FuelTypeId FROM dbo.FuelTypes WHERE Name = N'Hybrid');
DECLARE @AutomaticId INT = (SELECT TransmissionTypeId FROM dbo.TransmissionTypes WHERE Name = N'Automatic');
DECLARE @ManualId INT = (SELECT TransmissionTypeId FROM dbo.TransmissionTypes WHERE Name = N'Manual');
IF @EconomyId IS NULL OR @SedanId IS NULL OR @SUVId IS NULL OR @PetrolId IS NULL OR @AutomaticId IS NULL
    THROW 51011, 'Run Seed/02_Lookups.sql before the educational demo seed.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-101')
    INSERT dbo.Vehicles (PlateNumber, Make, Model, Year, CategoryId, FuelTypeId, TransmissionTypeId, SeatsCount, DailyPrice, BranchId, Status, CurrentMileage)
    VALUES (N'DEMO-101', N'Toyota', N'Yaris', 2022, @EconomyId, @PetrolId, @AutomaticId, 5, 28.00, @ManagerBranchId, N'Available', 28500);
IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-102')
    INSERT dbo.Vehicles (PlateNumber, Make, Model, Year, CategoryId, FuelTypeId, TransmissionTypeId, SeatsCount, DailyPrice, BranchId, Status, CurrentMileage)
    VALUES (N'DEMO-102', N'Hyundai', N'Sonata', 2023, @SedanId, @HybridId, @AutomaticId, 5, 45.00, @ManagerBranchId, N'Rented', 16200);
IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-103')
    INSERT dbo.Vehicles (PlateNumber, Make, Model, Year, CategoryId, FuelTypeId, TransmissionTypeId, SeatsCount, DailyPrice, BranchId, Status, CurrentMileage)
    VALUES (N'DEMO-103', N'Kia', N'Sportage', 2024, @SUVId, @PetrolId, @AutomaticId, 5, 58.00, @EmployeeBranchId, N'Reserved', 9100);
IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-104')
    INSERT dbo.Vehicles (PlateNumber, Make, Model, Year, CategoryId, FuelTypeId, TransmissionTypeId, SeatsCount, DailyPrice, BranchId, Status, CurrentMileage)
    VALUES (N'DEMO-104', N'Nissan', N'Leaf', 2023, @EconomyId, @ElectricId, @AutomaticId, 5, 50.00, @EmployeeBranchId, N'Maintenance', 12100);
IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-105')
    INSERT dbo.Vehicles (PlateNumber, Make, Model, Year, CategoryId, FuelTypeId, TransmissionTypeId, SeatsCount, DailyPrice, BranchId, Status, CurrentMileage)
    VALUES (N'DEMO-105', N'Honda', N'Civic', 2021, @SedanId, @PetrolId, @ManualId, 5, 35.00, @ManagerBranchId, N'Available', 44200);

DECLARE @VehicleCompletedId INT = (SELECT VehicleId FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-101');
DECLARE @VehicleRentedId INT = (SELECT VehicleId FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-102');
DECLARE @VehicleReservedId INT = (SELECT VehicleId FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-103');
DECLARE @VehicleMaintenanceId INT = (SELECT VehicleId FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-104');
DECLARE @VehiclePendingId INT = (SELECT VehicleId FROM dbo.Vehicles WHERE PlateNumber = N'DEMO-105');

INSERT dbo.VehicleImages (VehicleId, ImageUrl, IsPrimary)
SELECT v.VehicleId, N'https://placehold.co/1200x800?text=' + v.PlateNumber, 1
FROM dbo.Vehicles v
WHERE v.PlateNumber LIKE N'DEMO-%'
  AND NOT EXISTS (SELECT 1 FROM dbo.VehicleImages i WHERE i.VehicleId = v.VehicleId AND i.IsPrimary = 1);

-- One revoked, non-usable example token demonstrates persisted session history.
IF NOT EXISTS (SELECT 1 FROM dbo.RefreshTokens WHERE Token = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', N'educational-demo-revoked-token'), 2))
    INSERT dbo.RefreshTokens (UserId, Token, ExpiresAt, RevokedAt)
    VALUES (@CustomerUserId, CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', N'educational-demo-revoked-token'), 2), '2026-01-01', '2026-01-01');

-- Historical completed rental, current active rental, future confirmed and pending examples.
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE CustomerId = @CustomerId AND VehicleId = @VehicleCompletedId AND PickupDateTime = '2026-09-01T09:00:00')
    INSERT dbo.Reservations (CustomerId, VehicleId, PickupBranchId, ReturnBranchId, PickupDateTime, ExpectedReturnDateTime, Status, ConfirmedByEmployeeId, CreatedAt)
    VALUES (@CustomerId, @VehicleCompletedId, @ManagerBranchId, @ManagerBranchId, '2026-09-01T09:00:00', '2026-09-04T09:00:00', N'Completed', @ManagerEmployeeId, '2026-08-25T12:00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE CustomerId = @CustomerId AND VehicleId = @VehicleRentedId AND PickupDateTime = '2026-10-19T09:00:00')
    INSERT dbo.Reservations (CustomerId, VehicleId, PickupBranchId, ReturnBranchId, PickupDateTime, ExpectedReturnDateTime, Status, ConfirmedByEmployeeId, CreatedAt)
    VALUES (@CustomerId, @VehicleRentedId, @ManagerBranchId, @ManagerBranchId, '2026-10-19T09:00:00', '2026-10-22T09:00:00', N'Confirmed', @ManagerEmployeeId, '2026-10-10T12:00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE CustomerId = @Customer2Id AND VehicleId = @VehicleReservedId AND PickupDateTime = '2026-11-05T10:00:00')
    INSERT dbo.Reservations (CustomerId, VehicleId, PickupBranchId, ReturnBranchId, PickupDateTime, ExpectedReturnDateTime, Status, ConfirmedByEmployeeId, CreatedAt)
    VALUES (@Customer2Id, @VehicleReservedId, @EmployeeBranchId, @EmployeeBranchId, '2026-11-05T10:00:00', '2026-11-08T10:00:00', N'Confirmed', @EmployeeId, '2026-10-18T10:00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE CustomerId = @CustomerId AND VehicleId = @VehiclePendingId AND PickupDateTime = '2026-11-15T10:00:00')
    INSERT dbo.Reservations (CustomerId, VehicleId, PickupBranchId, ReturnBranchId, PickupDateTime, ExpectedReturnDateTime, Status, CreatedAt)
    VALUES (@CustomerId, @VehiclePendingId, @ManagerBranchId, @ManagerBranchId, '2026-11-15T10:00:00', '2026-11-17T10:00:00', N'Pending', '2026-10-19T11:00:00');

DECLARE @CompletedReservationId INT = (SELECT ReservationId FROM dbo.Reservations WHERE CustomerId = @CustomerId AND VehicleId = @VehicleCompletedId AND PickupDateTime = '2026-09-01T09:00:00');
DECLARE @ActiveReservationId INT = (SELECT ReservationId FROM dbo.Reservations WHERE CustomerId = @CustomerId AND VehicleId = @VehicleRentedId AND PickupDateTime = '2026-10-19T09:00:00');

IF NOT EXISTS (SELECT 1 FROM dbo.RentalContracts WHERE ContractNumber = N'DEMO-CTR-2026-001')
    INSERT dbo.RentalContracts (ReservationId, ContractNumber, CustomerId, VehicleId, EmployeeId, ReturnedByEmployeeId, ActualPickupDate, ActualReturnDate, PickupMileage, ReturnMileage, PickupFuelLevel, ReturnFuelLevel, ConditionNotes, RentalDays, BasicRentalPrice, DiscountAmount, AdditionalFees, FuelCharges, DamageCharges, TotalAmount, Status)
    VALUES (@CompletedReservationId, N'DEMO-CTR-2026-001', @CustomerId, @VehicleCompletedId, @ManagerEmployeeId, @ManagerEmployeeId, '2026-09-01T09:00:00', '2026-09-04T09:00:00', 27500, 28500, N'Full', N'Full', N'Example completed contract for classroom demonstrations.', 3, 84.00, 4.00, 0.00, 0.00, 0.00, 80.00, N'Completed');
IF NOT EXISTS (SELECT 1 FROM dbo.RentalContracts WHERE ContractNumber = N'DEMO-CTR-2026-002')
    INSERT dbo.RentalContracts (ReservationId, ContractNumber, CustomerId, VehicleId, EmployeeId, ActualPickupDate, PickupMileage, PickupFuelLevel, ConditionNotes, RentalDays, BasicRentalPrice, TotalAmount, Status)
    VALUES (@ActiveReservationId, N'DEMO-CTR-2026-002', @CustomerId, @VehicleRentedId, @ManagerEmployeeId, '2026-10-19T09:00:00', 15900, N'Full', N'Active sample rental; return is expected on 2026-10-22.', 3, 135.00, 135.00, N'Active');

DECLARE @CompletedContractId INT = (SELECT ContractId FROM dbo.RentalContracts WHERE ContractNumber = N'DEMO-CTR-2026-001');
DECLARE @ActiveContractId INT = (SELECT ContractId FROM dbo.RentalContracts WHERE ContractNumber = N'DEMO-CTR-2026-002');
IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE TransactionReference = N'DEMO-PAY-001')
    INSERT dbo.Payments (ContractId, PaymentMethod, Amount, TransactionReference, PaymentStatus, PaymentDate) VALUES (@CompletedContractId, N'CreditCard', 80.00, N'DEMO-PAY-001', N'Paid', '2026-09-04T09:00:00');
IF NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE TransactionReference = N'DEMO-PAY-002')
    INSERT dbo.Payments (ContractId, PaymentMethod, Amount, TransactionReference, PaymentStatus, PaymentDate) VALUES (@ActiveContractId, N'Cash', 50.00, N'DEMO-PAY-002', N'Paid', '2026-10-19T09:05:00');

IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRecords WHERE VehicleId = @VehicleCompletedId AND MaintenanceType = N'Routine inspection' AND StartDate = '2026-09-05')
    INSERT dbo.MaintenanceRecords (VehicleId, CreatedByEmployeeId, MaintenanceType, ProblemDescription, StartDate, ExpectedEndDate, ActualEndDate, Cost, Status)
    VALUES (@VehicleCompletedId, @ManagerEmployeeId, N'Routine inspection', N'Completed scheduled safety inspection.', '2026-09-05', '2026-09-05', '2026-09-05', 45.00, N'Completed');
IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRecords WHERE VehicleId = @VehicleMaintenanceId AND MaintenanceType = N'Battery diagnostics' AND StartDate = '2026-10-18')
    INSERT dbo.MaintenanceRecords (VehicleId, CreatedByEmployeeId, MaintenanceType, ProblemDescription, StartDate, ExpectedEndDate, Cost, Status)
    VALUES (@VehicleMaintenanceId, @EmployeeId, N'Battery diagnostics', N'Example open maintenance job; vehicle is unavailable.', '2026-10-18', '2026-10-23', NULL, N'InProgress');

IF NOT EXISTS (SELECT 1 FROM dbo.Reviews WHERE ContractId = @CompletedContractId)
    INSERT dbo.Reviews (ContractId, Rating, Comment, CreatedAt) VALUES (@CompletedContractId, 5, N'Clean car and a smooth pickup process. Educational sample review.', '2026-09-05T10:00:00');

IF NOT EXISTS (SELECT 1 FROM dbo.AuditLogs WHERE Action = N'Demo.SeedCompleted' AND EntityId = N'DEMO-DATA-V1')
    INSERT dbo.AuditLogs (ActorUserId, Action, EntityName, EntityId, OldValue, NewValue, IPAddress, DeviceInfo, [Timestamp])
    VALUES (@ManagerUserId, N'Demo.SeedCompleted', N'DemoDataset', N'DEMO-DATA-V1', NULL, N'{"version":1,"purpose":"educational sample data"}', N'127.0.0.1', N'Local educational seed', SYSUTCDATETIME());

COMMIT TRANSACTION;
PRINT 'Educational demo data is installed. Demo accounts: demo.manager, demo.employee, demo.customer, demo.customer2. Password: Demo123!';
GO
