-- Stored procedure convention:
--   * one file per procedure: usp_<Entity>_<Action>.sql
--   * use CREATE OR ALTER PROCEDURE and SET NOCOUNT ON
--   * use THROW 50xxx for documented business-rule failures
-- Add each procedure here with :r .\usp_<Entity>_<Action>.sql when created.
:on error exit
USE CarRentalDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
:r ..\Functions\fn_Vehicles_IsAvailable.sql
:r .\usp_Users_Create.sql
:r .\usp_Users_GetById.sql
:r .\usp_Users_GetByUsernameOrEmail.sql
:r .\usp_Users_Exists.sql
:r .\usp_Roles_GetIdByName.sql
:r .\usp_UserRoles_Assign.sql
:r .\usp_UserRoles_GetNames.sql
:r .\usp_Customers_Create.sql
:r .\usp_Customers_GetByUserId.sql
:r .\usp_Customers_ExistsByLicense.sql
:r .\usp_RefreshTokens_Create.sql
:r .\usp_RefreshTokens_GetByToken.sql
:r .\usp_RefreshTokens_Revoke.sql
:r .\usp_RefreshTokens_RevokeAllForUser.sql
:r .\usp_RefreshTokens_TryRevoke.sql
:r .\usp_AuditLogs_Insert.sql
:r .\usp_AuditLogs_Query.sql
:r .\usp_Branches_Create.sql
:r .\usp_Branches_GetById.sql
:r .\usp_Branches_List.sql
:r .\usp_Branches_Count.sql
:r .\usp_Branches_Update.sql
:r .\usp_Branches_HasDependents.sql
:r .\usp_Branches_Delete.sql
:r .\usp_Branches_SetManager.sql
:r .\usp_Employees_Create.sql
:r .\usp_Employees_GetById.sql
:r .\usp_Employees_GetByUserId.sql
:r .\usp_Employees_ListByBranch.sql
:r .\usp_Employees_CountByBranch.sql
:r .\usp_Employees_Update.sql
:r .\usp_Users_List.sql
:r .\usp_Users_Count.sql
:r .\usp_Users_SetActive.sql
:r .\usp_UserRoles_Remove.sql
:r .\usp_Lookups_List.sql
:r .\usp_Lookups_Create.sql
:r .\usp_Lookups_Update.sql
:r .\usp_Lookups_Delete.sql
:r .\usp_Vehicles_Create.sql
:r .\usp_Vehicles_GetById.sql
:r .\usp_Vehicles_ListByBranch.sql
:r .\usp_Vehicles_CountByBranch.sql
:r .\usp_Vehicles_Update.sql
:r .\usp_Vehicles_Delete.sql
:r .\usp_Vehicles_Search.sql
:r .\usp_Vehicles_GetCatalogDetails.sql
:r .\usp_Vehicles_IsAvailable.sql
:r .\usp_Reservations_Create.sql
:r .\usp_Reservations_GetById.sql
:r .\usp_Reservations_ListByCustomer.sql
:r .\usp_Reservations_UpdateStatus.sql
:r .\usp_Reservations_ListForBranch.sql
:r .\usp_Vehicles_SetStatus.sql
:r .\usp_Vehicles_LockForReservationReview.sql
:r .\usp_Vehicles_HasOtherConfirmedReservation.sql
:r .\usp_RentalContracts_CreateFromReservation.sql
:r .\usp_RentalContracts_GetById.sql
:r .\usp_RentalContracts_List.sql
:r .\usp_RentalContracts_ListByCustomer.sql
:r .\usp_RentalContracts_Return.sql
:r .\usp_Payments_Create.sql
:r .\usp_Payments_GetById.sql
:r .\usp_Payments_UpdateStatus.sql
:r .\usp_Payments_ListByContract.sql
:r .\usp_Payments_ListByCustomer.sql
:r .\usp_Payments_GetBalance.sql
:r .\usp_VehicleImages_Add.sql
:r .\usp_VehicleImages_ListByVehicle.sql
:r .\usp_VehicleImages_Remove.sql
:r .\usp_VehicleImages_SetPrimary.sql

PRINT 'CarRentalDB core stored procedures created successfully.';
GO
