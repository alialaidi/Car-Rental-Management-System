CREATE OR ALTER PROCEDURE dbo.usp_Maintenance_ListByVehicle @VehicleId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT m.MaintenanceId, m.VehicleId, v.BranchId, m.CreatedByEmployeeId, m.MaintenanceType,
        m.ProblemDescription, m.StartDate, m.ExpectedEndDate, m.ActualEndDate, m.Cost, m.Status
    FROM dbo.MaintenanceRecords m INNER JOIN dbo.Vehicles v ON v.VehicleId = m.VehicleId
    WHERE m.VehicleId = @VehicleId ORDER BY m.StartDate DESC, m.MaintenanceId DESC;
END;
GO
