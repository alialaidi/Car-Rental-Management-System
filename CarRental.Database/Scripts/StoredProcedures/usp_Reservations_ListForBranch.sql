CREATE OR ALTER PROCEDURE dbo.usp_Reservations_ListForBranch
    @BranchId INT = NULL,
    @Status NVARCHAR(20) = NULL,
    @PickupFrom DATETIME2(3) = NULL,
    @PickupTo DATETIME2(3) = NULL,
    @Page INT,
    @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS TotalCount
    FROM dbo.Reservations AS r
    WHERE (@BranchId IS NULL OR r.PickupBranchId = @BranchId)
      AND (@Status IS NULL OR r.Status = @Status)
      AND (@PickupFrom IS NULL OR r.PickupDateTime >= @PickupFrom)
      AND (@PickupTo IS NULL OR r.PickupDateTime <= @PickupTo);

    SELECT r.ReservationId, r.CustomerId, r.VehicleId, r.PickupBranchId, r.ReturnBranchId,
           r.PickupDateTime, r.ExpectedReturnDateTime, r.Status, r.ConfirmedByEmployeeId, r.CreatedAt
    FROM dbo.Reservations AS r
    WHERE (@BranchId IS NULL OR r.PickupBranchId = @BranchId)
      AND (@Status IS NULL OR r.Status = @Status)
      AND (@PickupFrom IS NULL OR r.PickupDateTime >= @PickupFrom)
      AND (@PickupTo IS NULL OR r.PickupDateTime <= @PickupTo)
    ORDER BY r.CreatedAt DESC, r.ReservationId DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
