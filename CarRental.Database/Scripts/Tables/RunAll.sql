-- =====================================================================
-- RunAll.sql
-- Purpose: Executes every table script in the correct dependency order.
-- Run this file with SQLCMD mode enabled (SSMS: Query > SQLCMD Mode).
-- DEV RESET: run 99_DropAll.sql, then this file, then ../Seed/RunSeed.sql.
-- This script recreates tables and is destructive; do not run against data you need.
-- =====================================================================
:on error exit
:r .\00_CreateDatabase.sql
:r .\01_Roles.sql
:r .\02_Users.sql
:r .\03_UserRoles.sql
:r .\04_RefreshTokens.sql
:r .\05_Branches.sql
:r .\06_Customers.sql
:r .\07_Employees.sql
:r .\08_Branches_AddManagerFK.sql
:r .\09_VehicleCategories.sql
:r .\10_FuelTypes.sql
:r .\11_TransmissionTypes.sql
:r .\12_Vehicles.sql
:r .\13_VehicleImages.sql
:r .\14_Reservations.sql
:r .\15_RentalContracts.sql
:r .\20_ContractNumberSequence.sql
:r .\16_Payments.sql
:r .\17_MaintenanceRecords.sql
:r .\18_Reviews.sql
:r .\19_AuditLogs.sql

PRINT 'All CarRentalDB tables created successfully.';
GO
