CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_GetCatalogDetails
    @VehicleId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.VehicleId,
        v.PlateNumber,
        v.Make,
        v.Model,
        v.Year,
        v.CategoryId,
        c.Name AS CategoryName,
        v.FuelTypeId,
        f.Name AS FuelTypeName,
        v.TransmissionTypeId,
        t.Name AS TransmissionTypeName,
        v.SeatsCount,
        v.DailyPrice,
        v.BranchId,
        v.Status,
        v.CurrentMileage,
        primaryImage.ImageUrl AS PrimaryImageUrl,
        vi.ImageId,
        vi.VehicleId AS ImageVehicleId,
        vi.ImageUrl,
        vi.IsPrimary AS ImageIsPrimary
    FROM dbo.Vehicles AS v
    INNER JOIN dbo.VehicleCategories AS c ON c.CategoryId = v.CategoryId
    INNER JOIN dbo.FuelTypes AS f ON f.FuelTypeId = v.FuelTypeId
    INNER JOIN dbo.TransmissionTypes AS t ON t.TransmissionTypeId = v.TransmissionTypeId
    OUTER APPLY
    (
        SELECT TOP (1) image.ImageUrl
        FROM dbo.VehicleImages AS image
        WHERE image.VehicleId = v.VehicleId
        ORDER BY image.IsPrimary DESC, image.ImageId ASC
    ) AS primaryImage
    LEFT JOIN dbo.VehicleImages AS vi ON vi.VehicleId = v.VehicleId
    WHERE v.VehicleId = @VehicleId
      AND v.Status <> N'Maintenance'
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.MaintenanceRecords AS m
          WHERE m.VehicleId = v.VehicleId
            AND m.Status = N'InProgress'
      )
    ORDER BY vi.IsPrimary DESC, vi.ImageId ASC;
END;
GO
