CREATE OR ALTER PROCEDURE dbo.usp_Reports_RevenueByPeriod
    @FromDate DATE,
    @ToDate DATE,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(DATE, p.PaymentDate) AS Period, b.BranchId, b.Name AS BranchName,
        SUM(CASE WHEN p.PaymentStatus = 'Paid' THEN p.Amount ELSE 0 END) AS PaidAmount,
        SUM(CASE WHEN p.PaymentStatus = 'Refunded' THEN p.Amount ELSE 0 END) AS RefundedAmount,
        SUM(CASE WHEN p.PaymentStatus = 'Paid' THEN p.Amount WHEN p.PaymentStatus = 'Refunded' THEN -p.Amount ELSE 0 END) AS NetRevenue
    FROM dbo.Payments p
    INNER JOIN dbo.RentalContracts c ON c.ContractId = p.ContractId
    LEFT JOIN dbo.Reservations r ON r.ReservationId = c.ReservationId
    INNER JOIN dbo.Vehicles v ON v.VehicleId = c.VehicleId
    INNER JOIN dbo.Branches b ON b.BranchId = COALESCE(r.PickupBranchId, v.BranchId)
    WHERE p.PaymentDate >= @FromDate AND p.PaymentDate < DATEADD(DAY, 1, CONVERT(DATETIME2, @ToDate))
      AND (@BranchId IS NULL OR b.BranchId = @BranchId)
    GROUP BY CONVERT(DATE, p.PaymentDate), b.BranchId, b.Name
    ORDER BY Period, b.Name;
END;
GO
