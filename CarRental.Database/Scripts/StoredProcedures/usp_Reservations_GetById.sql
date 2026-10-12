CREATE OR ALTER PROCEDURE dbo.usp_Reservations_GetById
    @ReservationId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT ReservationId, CustomerId, VehicleId, PickupBranchId, ReturnBranchId,
           PickupDateTime, ExpectedReturnDateTime, Status, ConfirmedByEmployeeId, CreatedAt
    FROM dbo.Reservations
    WHERE ReservationId = @ReservationId;
END;
GO
