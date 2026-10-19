CREATE OR ALTER PROCEDURE dbo.usp_Maintenance_ListByBranch @BranchId INT, @Page INT, @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS TotalCount FROM dbo.MaintenanceRecords m INNER JOIN dbo.Vehicles v ON v.VehicleId = m.VehicleId WHERE v.BranchId = @BranchId;
    SELECT m.MaintenanceId, m.VehicleId, v.BranchId, m.CreatedByEmployeeId, m.MaintenanceType,
        m.ProblemDescription, m.StartDate, m.ExpectedEndDate, m.ActualEndDate, m.Cost, m.Status
    FROM dbo.MaintenanceRecords m INNER JOIN dbo.Vehicles v ON v.VehicleId = m.VehicleId
    WHERE v.BranchId = @BranchId ORDER BY m.StartDate DESC, m.MaintenanceId DESC
    OFFSET (@Page - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
