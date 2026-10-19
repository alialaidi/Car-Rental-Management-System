CREATE OR ALTER PROCEDURE dbo.usp_Maintenance_Create
    @VehicleId INT, @CreatedByEmployeeId INT, @MaintenanceType NVARCHAR(100),
    @ProblemDescription NVARCHAR(1000) = NULL, @StartDate DATE, @ExpectedEndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @OwnTransaction BIT = 0, @VehicleStatus NVARCHAR(20), @BranchId INT, @MaintenanceId INT;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @OwnTransaction = 1;
    END;
    BEGIN TRY
        SELECT @VehicleStatus = Status, @BranchId = BranchId
        FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK) WHERE VehicleId = @VehicleId;
        IF @BranchId IS NULL THROW 50080, 'Vehicle not found.', 1;
        IF @VehicleStatus NOT IN (N'Available', N'Maintenance') THROW 50081, 'Vehicle state does not allow maintenance.', 1;
        IF @ExpectedEndDate IS NOT NULL AND @ExpectedEndDate < @StartDate THROW 50002, 'Expected end date precedes start date.', 1;
        IF EXISTS (SELECT 1 FROM dbo.MaintenanceRecords WITH (UPDLOCK, HOLDLOCK)
                   WHERE VehicleId = @VehicleId AND Status = N'InProgress')
            THROW 50083, 'Maintenance already in progress for vehicle.', 1;
        IF EXISTS (SELECT 1 FROM dbo.Reservations WITH (UPDLOCK, HOLDLOCK)
                   WHERE VehicleId = @VehicleId AND Status = N'Confirmed'
                     AND PickupDateTime < DATEADD(DAY, 1, COALESCE(@ExpectedEndDate, @StartDate))
                     AND ExpectedReturnDateTime > CONVERT(DATETIME2(3), @StartDate))
            THROW 50082, 'Confirmed reservation overlaps maintenance.', 1;

        INSERT dbo.MaintenanceRecords(VehicleId, CreatedByEmployeeId, MaintenanceType, ProblemDescription, StartDate, ExpectedEndDate, Status)
        VALUES (@VehicleId, @CreatedByEmployeeId, @MaintenanceType, @ProblemDescription, @StartDate, @ExpectedEndDate, N'InProgress');
        SET @MaintenanceId = CONVERT(INT, SCOPE_IDENTITY());
        UPDATE dbo.Vehicles SET Status = N'Maintenance' WHERE VehicleId = @VehicleId;
        IF @OwnTransaction = 1 COMMIT TRANSACTION;
        SELECT @MaintenanceId;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
