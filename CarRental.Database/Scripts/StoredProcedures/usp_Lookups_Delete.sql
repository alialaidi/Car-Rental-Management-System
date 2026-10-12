CREATE OR ALTER PROCEDURE dbo.usp_Lookups_Delete @Type NVARCHAR(30), @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF @Type=N'Category'
    BEGIN
        IF EXISTS(SELECT 1 FROM dbo.Vehicles WHERE CategoryId=@Id) THROW 50051,'Lookup is in use.',1;
        DELETE dbo.VehicleCategories WHERE CategoryId=@Id;
    END
    ELSE IF @Type=N'FuelType'
    BEGIN
        IF EXISTS(SELECT 1 FROM dbo.Vehicles WHERE FuelTypeId=@Id) THROW 50051,'Lookup is in use.',1;
        DELETE dbo.FuelTypes WHERE FuelTypeId=@Id;
    END
    ELSE IF @Type=N'TransmissionType'
    BEGIN
        IF EXISTS(SELECT 1 FROM dbo.Vehicles WHERE TransmissionTypeId=@Id) THROW 50051,'Lookup is in use.',1;
        DELETE dbo.TransmissionTypes WHERE TransmissionTypeId=@Id;
    END
    ELSE THROW 50055, 'Unknown lookup type.', 1;
    IF @@ROWCOUNT=0 THROW 50054,'Lookup does not exist.',1;
END;
GO
