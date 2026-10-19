CREATE OR ALTER PROCEDURE dbo.usp_Reports_MaintenanceCost
    @FromDate DATE,
    @ToDate DATE,
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BranchId, b.Name AS BranchName, SUM(COALESCE(m.Cost, 0)) AS TotalCost, COUNT(*) AS CompletedRecords
    FROM dbo.MaintenanceRecords m
    INNER JOIN dbo.Vehicles v ON v.VehicleId = m.VehicleId
    INNER JOIN dbo.Branches b ON b.BranchId = v.BranchId
    WHERE m.Status = 'Completed' AND m.ActualEndDate >= @FromDate AND m.ActualEndDate <= @ToDate
      AND (@BranchId IS NULL OR b.BranchId = @BranchId)
    GROUP BY b.BranchId, b.Name
    ORDER BY b.Name;
END;
GO
