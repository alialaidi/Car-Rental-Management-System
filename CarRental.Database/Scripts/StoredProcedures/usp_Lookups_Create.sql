CREATE OR ALTER PROCEDURE dbo.usp_Lookups_Create @Type NVARCHAR(30), @Name NVARCHAR(100), @Description NVARCHAR(500)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Type=N'Category' BEGIN INSERT dbo.VehicleCategories(Name,Description) VALUES(@Name,@Description); SELECT CONVERT(INT,SCOPE_IDENTITY()); END
    ELSE IF @Type=N'FuelType' BEGIN INSERT dbo.FuelTypes(Name) VALUES(@Name); SELECT CONVERT(INT,SCOPE_IDENTITY()); END
    ELSE IF @Type=N'TransmissionType' BEGIN INSERT dbo.TransmissionTypes(Name) VALUES(@Name); SELECT CONVERT(INT,SCOPE_IDENTITY()); END
    ELSE THROW 50055, 'Unknown lookup type.', 1;
END;
GO
