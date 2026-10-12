-- =====================================================================
-- DEV ONLY: drop the CarRentalDB schema so RunAll.sql can recreate it.
-- Run in SQLCMD mode. This deletes all data in CarRentalDB.
-- =====================================================================
:on error exit
USE CarRentalDB;
GO

IF DB_NAME() <> N'CarRentalDB'
    THROW 51001, 'Refusing to reset a database other than CarRentalDB.', 1;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Branches_ManagerEmployee')
    ALTER TABLE dbo.Branches DROP CONSTRAINT FK_Branches_ManagerEmployee;
GO

DROP TABLE IF EXISTS dbo.AuditLogs;
DROP TABLE IF EXISTS dbo.Reviews;
DROP TABLE IF EXISTS dbo.MaintenanceRecords;
DROP TABLE IF EXISTS dbo.Payments;
DROP TABLE IF EXISTS dbo.RentalContracts;
DROP TABLE IF EXISTS dbo.Reservations;
DROP TABLE IF EXISTS dbo.VehicleImages;
DROP TABLE IF EXISTS dbo.Vehicles;
DROP TABLE IF EXISTS dbo.TransmissionTypes;
DROP TABLE IF EXISTS dbo.FuelTypes;
DROP TABLE IF EXISTS dbo.VehicleCategories;
DROP TABLE IF EXISTS dbo.Employees;
DROP TABLE IF EXISTS dbo.Customers;
DROP TABLE IF EXISTS dbo.Branches;
DROP TABLE IF EXISTS dbo.RefreshTokens;
DROP TABLE IF EXISTS dbo.UserRoles;
DROP TABLE IF EXISTS dbo.Users;
DROP TABLE IF EXISTS dbo.Roles;
DROP SEQUENCE IF EXISTS dbo.ContractNumberSequence;
GO

PRINT 'CarRentalDB tables dropped. Run Tables/RunAll.sql to recreate them.';
GO
