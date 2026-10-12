CREATE OR ALTER PROCEDURE dbo.usp_Reservations_Create
    @CustomerId INT,
    @VehicleId INT,
    @PickupBranchId INT,
    @ReturnBranchId INT,
    @PickupDateTime DATETIME2(3),
    @ExpectedReturnDateTime DATETIME2(3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @VehicleBranchId INT;
    DECLARE @ReservationId INT;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @VehicleBranchId = v.BranchId
        FROM dbo.Vehicles AS v WITH (UPDLOCK, HOLDLOCK)
        WHERE v.VehicleId = @VehicleId;

        IF @VehicleBranchId IS NULL
            THROW 50044, 'Vehicle was not found.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerId = @CustomerId)
            THROW 50040, 'Customer was not found.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE BranchId = @PickupBranchId)
            THROW 50043, 'Pickup branch was not found.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE BranchId = @ReturnBranchId)
            THROW 50043, 'Return branch was not found.', 1;

        IF @VehicleBranchId <> @PickupBranchId
            THROW 50004, 'Pickup branch does not match the vehicle branch.', 1;

        IF @ExpectedReturnDateTime <= @PickupDateTime
            THROW 50002, 'Expected return must be after pickup.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.Customers
            WHERE CustomerId = @CustomerId
              AND DrivingLicenseExpiry >= CONVERT(date, @ExpectedReturnDateTime)
        )
            THROW 50006, 'Driving license must remain valid through the expected return date.', 1;

        IF NOT EXISTS
        (
            SELECT 1
            FROM dbo.fn_Vehicles_IsAvailable(@VehicleId, @PickupDateTime, @ExpectedReturnDateTime)
            WHERE IsAvailable = 1
        )
            THROW 50001, 'Vehicle is not available for the requested dates.', 1;

        INSERT dbo.Reservations
            (CustomerId, VehicleId, PickupBranchId, ReturnBranchId, PickupDateTime, ExpectedReturnDateTime, Status)
        VALUES
            (@CustomerId, @VehicleId, @PickupBranchId, @ReturnBranchId, @PickupDateTime, @ExpectedReturnDateTime, N'Pending');

        SET @ReservationId = CONVERT(int, SCOPE_IDENTITY());
        COMMIT TRANSACTION;

        SELECT @ReservationId;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
