CREATE OR ALTER PROCEDURE dbo.usp_RentalContracts_CreateFromReservation
    @ReservationId INT,
    @EmployeeId INT,
    @ActualPickupDate DATETIME2(3),
    @PickupMileage INT,
    @PickupFuelLevel NVARCHAR(20),
    @ConditionNotes NVARCHAR(1000) = NULL,
    @RentalDays INT,
    @BasicRentalPrice DECIMAL(18,2),
    @DiscountAmount DECIMAL(18,2),
    @AdditionalFees DECIMAL(18,2),
    @TotalAmount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @VehicleId INT, @CustomerId INT, @BranchId INT, @ReservationStatus NVARCHAR(20);
    DECLARE @VehicleStatus NVARCHAR(20), @CurrentMileage INT;

    -- Read the immutable vehicle link, then lock vehicle before reservation to
    -- match the lock order used by reservation review procedures.
    SELECT @VehicleId = VehicleId
    FROM dbo.Reservations
    WHERE ReservationId = @ReservationId;
    IF @VehicleId IS NULL THROW 50060, 'Reservation was not found.', 1;

    SELECT @VehicleStatus = Status, @CurrentMileage = CurrentMileage
    FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
    WHERE VehicleId = @VehicleId;
    IF @VehicleStatus IS NULL THROW 50061, 'Vehicle is unavailable.', 1;

    SELECT @CustomerId = CustomerId, @BranchId = PickupBranchId, @ReservationStatus = Status
    FROM dbo.Reservations WITH (UPDLOCK, HOLDLOCK)
    WHERE ReservationId = @ReservationId;

    IF @ReservationStatus <> N'Confirmed' OR @VehicleStatus <> N'Reserved'
        THROW 50061, 'Only a confirmed reservation with a reserved vehicle can be converted.', 1;
    IF @PickupMileage < @CurrentMileage
        THROW 50062, 'Pickup mileage cannot be less than the current mileage.', 1;
    IF @BasicRentalPrice <> ROUND((SELECT DailyPrice FROM dbo.Vehicles WHERE VehicleId = @VehicleId) * @RentalDays, 2)
        THROW 50063, 'Vehicle price changed while the contract was being prepared.', 1;
    IF @RentalDays <= 0 OR @BasicRentalPrice < 0 OR @DiscountAmount < 0 OR @AdditionalFees < 0
       OR @DiscountAmount > @BasicRentalPrice + @AdditionalFees
       OR @TotalAmount <> @BasicRentalPrice + @AdditionalFees - @DiscountAmount
        THROW 50002, 'Contract amounts are invalid.', 1;
    IF @PickupFuelLevel NOT IN (N'Empty', N'Quarter', N'Half', N'ThreeQuarters', N'Full')
        THROW 50002, 'Pickup fuel level is invalid.', 1;

    DECLARE @SequenceValue BIGINT = NEXT VALUE FOR dbo.ContractNumberSequence;
    DECLARE @ContractNumber NVARCHAR(50) = N'RC-' + CONVERT(CHAR(8), @ActualPickupDate, 112) + N'-'
        + RIGHT(N'000000' + CONVERT(NVARCHAR(20), @SequenceValue), 6);

    INSERT dbo.RentalContracts
    (
        ReservationId, ContractNumber, CustomerId, VehicleId, EmployeeId, ActualPickupDate,
        PickupMileage, PickupFuelLevel, ConditionNotes, RentalDays, BasicRentalPrice,
        DiscountAmount, AdditionalFees, FuelCharges, DamageCharges, TotalAmount, Status
    )
    VALUES
    (
        @ReservationId, @ContractNumber, @CustomerId, @VehicleId, @EmployeeId, @ActualPickupDate,
        @PickupMileage, @PickupFuelLevel, @ConditionNotes, @RentalDays, @BasicRentalPrice,
        @DiscountAmount, @AdditionalFees, 0, 0, @TotalAmount, N'Active'
    );

    DECLARE @ContractId INT = CONVERT(INT, SCOPE_IDENTITY());
    UPDATE dbo.Vehicles SET Status = N'Rented', CurrentMileage = @PickupMileage WHERE VehicleId = @VehicleId;
    SELECT @ContractId;
END;
GO
