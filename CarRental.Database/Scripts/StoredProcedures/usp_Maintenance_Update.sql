CREATE OR ALTER PROCEDURE dbo.usp_Maintenance_Update
    @MaintenanceId INT, @MaintenanceType NVARCHAR(100), @ProblemDescription NVARCHAR(1000) = NULL, @ExpectedEndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @OwnTransaction BIT = 0, @VehicleId INT, @Status NVARCHAR(20), @StartDate DATE;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @OwnTransaction = 1;
    END;
    BEGIN TRY
        SELECT @VehicleId = VehicleId FROM dbo.MaintenanceRecords WHERE MaintenanceId = @MaintenanceId;
        IF @VehicleId IS NULL THROW 50080, 'Maintenance record not found.', 1;
        SELECT @VehicleId = VehicleId FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK) WHERE VehicleId = @VehicleId;
        SELECT @Status = Status, @StartDate = StartDate FROM dbo.MaintenanceRecords WITH (UPDLOCK, HOLDLOCK) WHERE MaintenanceId = @MaintenanceId;
        IF @Status <> N'InProgress' THROW 50085, 'Only in-progress maintenance can be updated.', 1;
        IF @ExpectedEndDate IS NOT NULL AND @ExpectedEndDate < @StartDate THROW 50002, 'Expected end date precedes start date.', 1;
        UPDATE dbo.MaintenanceRecords SET MaintenanceType = @MaintenanceType, ProblemDescription = @ProblemDescription,
            ExpectedEndDate = @ExpectedEndDate WHERE MaintenanceId = @MaintenanceId AND Status = N'InProgress';
        IF @@ROWCOUNT = 0 THROW 50085, 'Maintenance record changed before update.', 1;
        IF @OwnTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
