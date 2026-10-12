CREATE OR ALTER PROCEDURE dbo.usp_Lookups_List @Type NVARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    IF @Type=N'Category' SELECT CategoryId AS Id, Name, Description FROM dbo.VehicleCategories ORDER BY Name;
    ELSE IF @Type=N'FuelType' SELECT FuelTypeId AS Id, Name, CONVERT(NVARCHAR(500),NULL) AS Description FROM dbo.FuelTypes ORDER BY Name;
    ELSE IF @Type=N'TransmissionType' SELECT TransmissionTypeId AS Id, Name, CONVERT(NVARCHAR(500),NULL) AS Description FROM dbo.TransmissionTypes ORDER BY Name;
    ELSE THROW 50055, 'Unknown lookup type.', 1;
END;
GO
