CREATE OR ALTER PROCEDURE dbo.usp_Reports_PaymentSummary
    @FromDate DATE,
    @ToDate DATE,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BranchId, b.Name AS BranchName, p.PaymentMethod, p.PaymentStatus,
        COUNT(*) AS PaymentCount, SUM(p.Amount) AS TotalAmount
    FROM dbo.Payments p
    INNER JOIN dbo.RentalContracts c ON c.ContractId = p.ContractId
    LEFT JOIN dbo.Reservations r ON r.ReservationId = c.ReservationId
    INNER JOIN dbo.Vehicles v ON v.VehicleId = c.VehicleId
    INNER JOIN dbo.Branches b ON b.BranchId = COALESCE(r.PickupBranchId, v.BranchId)
    WHERE p.PaymentDate >= @FromDate AND p.PaymentDate < DATEADD(DAY, 1, CONVERT(DATETIME2, @ToDate))
      AND (@BranchId IS NULL OR b.BranchId = @BranchId)
    GROUP BY b.BranchId, b.Name, p.PaymentMethod, p.PaymentStatus
    ORDER BY b.Name, p.PaymentMethod, p.PaymentStatus;
END;
GO
