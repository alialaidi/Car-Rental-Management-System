CREATE OR ALTER PROCEDURE dbo.usp_Reservations_ListByCustomer
    @CustomerId INT,
    @Page INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.Reservations
    WHERE CustomerId = @CustomerId;

    SELECT ReservationId, CustomerId, VehicleId, PickupBranchId, ReturnBranchId,
           PickupDateTime, ExpectedReturnDateTime, Status, ConfirmedByEmployeeId, CreatedAt
    FROM dbo.Reservations
    WHERE CustomerId = @CustomerId
    ORDER BY CreatedAt DESC, ReservationId DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
