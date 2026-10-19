CREATE OR ALTER PROCEDURE dbo.usp_Maintenance_Cancel @MaintenanceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @OwnTransaction BIT = 0, @VehicleId INT, @Status NVARCHAR(20);
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @OwnTransaction = 1;
    END;
    BEGIN TRY
        SELECT @VehicleId = VehicleId FROM dbo.MaintenanceRecords WHERE MaintenanceId = @MaintenanceId;
        IF @VehicleId IS NULL THROW 50080, 'Maintenance record not found.', 1;
        SELECT @VehicleId = VehicleId FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK) WHERE VehicleId = @VehicleId;
        SELECT @Status = Status FROM dbo.MaintenanceRecords WITH (UPDLOCK, HOLDLOCK) WHERE MaintenanceId = @MaintenanceId;
        IF @Status <> N'InProgress' THROW 50085, 'Only in-progress maintenance can be cancelled.', 1;
        UPDATE dbo.MaintenanceRecords SET Status = N'Cancelled' WHERE MaintenanceId = @MaintenanceId AND Status = N'InProgress';
        IF @@ROWCOUNT = 0 THROW 50085, 'Maintenance record changed before cancellation.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.MaintenanceRecords WHERE VehicleId = @VehicleId AND Status = N'InProgress')
            UPDATE dbo.Vehicles SET Status = N'Available' WHERE VehicleId = @VehicleId AND Status = N'Maintenance';
        IF @OwnTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
