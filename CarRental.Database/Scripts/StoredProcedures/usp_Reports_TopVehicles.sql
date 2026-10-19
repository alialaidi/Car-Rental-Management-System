CREATE OR ALTER PROCEDURE dbo.usp_Reports_TopVehicles
    @FromDate DATE,
    @ToDate DATE,
    @BranchId INT = NULL,
    @Take INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Take) v.VehicleId, v.PlateNumber, v.Make, v.Model,
        COUNT(*) AS RentalCount, SUM(c.TotalAmount) AS ContractValue
    FROM dbo.RentalContracts c
    INNER JOIN dbo.Vehicles v ON v.VehicleId = c.VehicleId
    LEFT JOIN dbo.Reservations r ON r.ReservationId = c.ReservationId
    WHERE c.Status = 'Completed' AND c.ActualReturnDate >= @FromDate
      AND c.ActualReturnDate < DATEADD(DAY, 1, CONVERT(DATETIME2, @ToDate))
      AND (@BranchId IS NULL OR COALESCE(r.PickupBranchId, v.BranchId) = @BranchId)
    GROUP BY v.VehicleId, v.PlateNumber, v.Make, v.Model
    ORDER BY RentalCount DESC, ContractValue DESC, v.VehicleId;
END;
GO
