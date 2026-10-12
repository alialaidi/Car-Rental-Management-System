CREATE OR ALTER PROCEDURE dbo.usp_Reservations_UpdateStatus
    @ReservationId INT,
    @ExpectedStatus NVARCHAR(20),
    @NewStatus NVARCHAR(20),
    @ConfirmedByEmployeeId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Reservations
    SET Status = @NewStatus,
        ConfirmedByEmployeeId = CASE WHEN @NewStatus = N'Confirmed' THEN @ConfirmedByEmployeeId ELSE ConfirmedByEmployeeId END
    WHERE ReservationId = @ReservationId
      AND Status = @ExpectedStatus;

    IF @@ROWCOUNT = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Reservations WHERE ReservationId = @ReservationId)
            THROW 50040, 'Reservation was not found.', 1;
        THROW 50004, 'Reservation status changed before the request could be applied.', 1;
    END
END;
GO
