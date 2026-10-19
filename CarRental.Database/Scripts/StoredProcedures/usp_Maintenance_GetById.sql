CREATE OR ALTER PROCEDURE dbo.usp_Maintenance_GetById @MaintenanceId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.MaintenanceId, m.VehicleId, v.BranchId, m.CreatedByEmployeeId, m.MaintenanceType,
        m.ProblemDescription, m.StartDate, m.ExpectedEndDate, m.ActualEndDate, m.Cost, m.Status
    FROM dbo.MaintenanceRecords m INNER JOIN dbo.Vehicles v ON v.VehicleId = m.VehicleId
    WHERE m.MaintenanceId = @MaintenanceId;
END;
GO
