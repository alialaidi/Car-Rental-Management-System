SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION dbo.fn_Vehicles_IsAvailable
(
    @VehicleId INT,
    @PickupDateTime DATETIME2(3),
    @ExpectedReturnDateTime DATETIME2(3)
)
RETURNS TABLE
AS
RETURN
(
    SELECT CONVERT(bit, CASE WHEN EXISTS
    (
        SELECT 1
        FROM dbo.Vehicles AS v
        WHERE v.VehicleId = @VehicleId
          AND v.Status <> N'Maintenance'
          AND
          (
              (
                  @PickupDateTime IS NULL
                  AND @ExpectedReturnDateTime IS NULL
                  AND v.Status = N'Available'
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.MaintenanceRecords AS m
                      WHERE m.VehicleId = v.VehicleId
                        AND m.Status = N'InProgress'
                  )
              )
              OR
              (
                  @PickupDateTime IS NOT NULL
                  AND @ExpectedReturnDateTime IS NOT NULL
                  AND @ExpectedReturnDateTime > @PickupDateTime
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.Reservations AS r
                      WHERE r.VehicleId = v.VehicleId
                        AND r.Status IN (N'Pending', N'Confirmed')
                        AND r.PickupDateTime < @ExpectedReturnDateTime
                        AND r.ExpectedReturnDateTime > @PickupDateTime
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.RentalContracts AS c
                      LEFT JOIN dbo.Reservations AS r ON r.ReservationId = c.ReservationId
                      WHERE c.VehicleId = v.VehicleId
                        AND c.Status = N'Active'
                        AND c.ActualPickupDate < @ExpectedReturnDateTime
                        AND COALESCE(r.ExpectedReturnDateTime, DATEADD(DAY, c.RentalDays, c.ActualPickupDate)) > @PickupDateTime
                  )
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.MaintenanceRecords AS m
                      WHERE m.VehicleId = v.VehicleId
                        AND m.Status = N'InProgress'
                        AND m.StartDate < @ExpectedReturnDateTime
                        AND DATEADD(DAY, 1, COALESCE(m.ActualEndDate, m.ExpectedEndDate, CONVERT(date, N'99991230'))) > @PickupDateTime
                  )
              )
          )
    ) THEN 1 ELSE 0 END) AS IsAvailable
);
GO
