CREATE OR ALTER PROCEDURE dbo.usp_Reports_FleetStatusByBranch
    @BranchId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT b.BranchId, b.Name AS BranchName, v.Status AS VehicleStatus, COUNT(*) AS VehicleCount
    FROM dbo.Vehicles v
    INNER JOIN dbo.Branches b ON b.BranchId = v.BranchId
    WHERE @BranchId IS NULL OR b.BranchId = @BranchId
    GROUP BY b.BranchId, b.Name, v.Status
    ORDER BY b.Name, v.Status;
END;
GO
