CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_HasOtherConfirmedReservation
    @VehicleId INT,
    @ExcludingReservationId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CONVERT(bit, CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Reservations
        WHERE VehicleId = @VehicleId
          AND ReservationId <> @ExcludingReservationId
          AND Status = N'Confirmed'
    ) THEN 1 ELSE 0 END);
END;
GO
