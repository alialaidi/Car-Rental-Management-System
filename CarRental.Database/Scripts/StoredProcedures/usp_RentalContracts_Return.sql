CREATE OR ALTER PROCEDURE dbo.usp_RentalContracts_Return
    @ContractId INT,
    @ReturnedByEmployeeId INT,
    @ActualReturnDate DATETIME2(3),
    @ReturnMileage INT,
    @ReturnFuelLevel NVARCHAR(20),
    @ConditionNotes NVARCHAR(1000) = NULL,
    @RentalDays INT,
    @BasicRentalPrice DECIMAL(18,2),
    @FuelCharges DECIMAL(18,2),
    @DamageCharges DECIMAL(18,2),
    @TotalAmount DECIMAL(18,2),
    @ReturnBranchId INT,
    @VehicleStatus NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OwnTransaction BIT = 0;
    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @OwnTransaction = 1;
    END;

    BEGIN TRY
        DECLARE @VehicleId INT, @ReservationId INT, @StoredStatus NVARCHAR(20);
        DECLARE @PickupDate DATETIME2(3), @PickupMileage INT, @StoredDays INT;
        DECLARE @StoredBasic DECIMAL(18,2), @AdditionalFees DECIMAL(18,2), @Discount DECIMAL(18,2);
        DECLARE @ReservationStatus NVARCHAR(20), @ExpectedReturnBranchId INT;
        DECLARE @CurrentMileage INT, @CurrentVehicleStatus NVARCHAR(20);

        -- Resolve immutable links first without holding locks, then lock vehicle,
        -- contract and reservation in that order to avoid conflicting lock order.
        SELECT @VehicleId = VehicleId, @ReservationId = ReservationId
        FROM dbo.RentalContracts WHERE ContractId = @ContractId;
        IF @VehicleId IS NULL THROW 50044, 'Contract was not found.', 1;
        IF @ReservationId IS NULL THROW 50064, 'Walk-in contracts cannot be returned through this operation.', 1;

        SELECT @CurrentMileage = CurrentMileage, @CurrentVehicleStatus = Status
        FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK) WHERE VehicleId = @VehicleId;
        IF @CurrentVehicleStatus IS NULL THROW 50069, 'Vehicle was not found.', 1;

        SELECT @StoredStatus = Status, @PickupDate = ActualPickupDate, @PickupMileage = PickupMileage,
               @StoredDays = RentalDays, @StoredBasic = BasicRentalPrice,
               @AdditionalFees = AdditionalFees, @Discount = DiscountAmount
        FROM dbo.RentalContracts WITH (UPDLOCK, HOLDLOCK)
        WHERE ContractId = @ContractId;

        SELECT @ReservationStatus = Status, @ExpectedReturnBranchId = ReturnBranchId
        FROM dbo.Reservations WITH (UPDLOCK, HOLDLOCK)
        WHERE ReservationId = @ReservationId;

        IF @StoredStatus <> N'Active' THROW 50067, 'Only active contracts can be returned.', 1;
        IF @CurrentVehicleStatus <> N'Rented' THROW 50069, 'Vehicle is not in rented status.', 1;
        IF @ReservationStatus <> N'Confirmed' THROW 50068, 'Reservation is not in a returnable state.', 1;
        IF @ReturnBranchId <> @ExpectedReturnBranchId THROW 50068, 'Return branch changed.', 1;
        IF @ActualReturnDate < @PickupDate THROW 50065, 'Return date is before pickup.', 1;
        IF @ReturnMileage < @PickupMileage OR @ReturnMileage < @CurrentMileage
            THROW 50066, 'Return mileage cannot be less than pickup or current mileage.', 1;
        DECLARE @ExpectedDays INT = CASE WHEN @ActualReturnDate <= @PickupDate THEN 1
            ELSE CONVERT(INT, CEILING(DATEDIFF_BIG(MILLISECOND, @PickupDate, @ActualReturnDate) / 86400000.0)) END;
        IF @RentalDays <> @ExpectedDays THROW 50070, 'Rental days do not match the return date.', 1;
        IF @ReturnFuelLevel NOT IN (N'Empty', N'Quarter', N'Half', N'ThreeQuarters', N'Full')
            THROW 50002, 'Return fuel level is invalid.', 1;
        IF @RentalDays <= 0 OR @BasicRentalPrice < 0 OR @FuelCharges < 0 OR @DamageCharges < 0
           OR @VehicleStatus NOT IN (N'Available', N'Maintenance')
           OR LEN(COALESCE(@ConditionNotes, N'')) > 1000
            THROW 50002, 'Return details are invalid.', 1;

        DECLARE @ExpectedBasic DECIMAL(18,2) = ROUND((@StoredBasic / @StoredDays) * @RentalDays, 2);
        DECLARE @ExpectedTotal DECIMAL(18,2) = ROUND(@ExpectedBasic + @AdditionalFees + @FuelCharges + @DamageCharges - @Discount, 2);
        IF @BasicRentalPrice <> @ExpectedBasic OR @TotalAmount <> @ExpectedTotal OR @ExpectedTotal < 0
            THROW 50002, 'Final rental amounts are invalid.', 1;

        UPDATE dbo.RentalContracts
        SET ActualReturnDate = @ActualReturnDate,
            ReturnedByEmployeeId = @ReturnedByEmployeeId,
            ReturnMileage = @ReturnMileage,
            ReturnFuelLevel = @ReturnFuelLevel,
            ConditionNotes = @ConditionNotes,
            RentalDays = @RentalDays,
            BasicRentalPrice = @BasicRentalPrice,
            FuelCharges = @FuelCharges,
            DamageCharges = @DamageCharges,
            TotalAmount = @TotalAmount,
            Status = N'Completed'
        WHERE ContractId = @ContractId AND Status = N'Active';
        IF @@ROWCOUNT = 0 THROW 50067, 'Contract changed before return could be applied.', 1;

        UPDATE dbo.Vehicles
        SET Status = @VehicleStatus, BranchId = @ReturnBranchId, CurrentMileage = @ReturnMileage
        WHERE VehicleId = @VehicleId AND Status = N'Rented';
        IF @@ROWCOUNT = 0 THROW 50069, 'Vehicle changed before return could be applied.', 1;

        UPDATE dbo.Reservations SET Status = N'Completed'
        WHERE ReservationId = @ReservationId AND Status = N'Confirmed';
        IF @@ROWCOUNT = 0 THROW 50068, 'Reservation changed before return could be applied.', 1;

        IF @OwnTransaction = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @OwnTransaction = 1 AND XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
