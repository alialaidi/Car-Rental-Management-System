CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_LockForReservationReview
    @VehicleId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FoundVehicleId INT;
    SELECT @FoundVehicleId = VehicleId
    FROM dbo.Vehicles WITH (UPDLOCK, HOLDLOCK)
    WHERE VehicleId = @VehicleId;

    IF @FoundVehicleId IS NULL
        THROW 50044, 'Vehicle was not found.', 1;
END;
GO
