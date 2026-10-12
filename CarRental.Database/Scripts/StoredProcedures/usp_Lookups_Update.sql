CREATE OR ALTER PROCEDURE dbo.usp_Lookups_Update @Type NVARCHAR(30), @Id INT, @Name NVARCHAR(100), @Description NVARCHAR(500)=NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Type=N'Category' BEGIN UPDATE dbo.VehicleCategories SET Name=@Name,Description=@Description WHERE CategoryId=@Id; IF @@ROWCOUNT=0 THROW 50054,'Lookup does not exist.',1; END
    ELSE IF @Type=N'FuelType' BEGIN UPDATE dbo.FuelTypes SET Name=@Name WHERE FuelTypeId=@Id; IF @@ROWCOUNT=0 THROW 50054,'Lookup does not exist.',1; END
    ELSE IF @Type=N'TransmissionType' BEGIN UPDATE dbo.TransmissionTypes SET Name=@Name WHERE TransmissionTypeId=@Id; IF @@ROWCOUNT=0 THROW 50054,'Lookup does not exist.',1; END
    ELSE THROW 50055, 'Unknown lookup type.', 1;
END;
GO
