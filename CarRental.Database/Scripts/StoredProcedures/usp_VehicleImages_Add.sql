CREATE OR ALTER PROCEDURE dbo.usp_VehicleImages_Add @VehicleId INT,@ImageUrl NVARCHAR(500),@IsPrimary BIT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.Vehicles WITH(UPDLOCK,HOLDLOCK) WHERE VehicleId=@VehicleId) THROW 50056,'Vehicle does not exist.',1;
 IF @IsPrimary=1 UPDATE dbo.VehicleImages WITH(UPDLOCK,HOLDLOCK) SET IsPrimary=0 WHERE VehicleId=@VehicleId;
 INSERT dbo.VehicleImages(VehicleId,ImageUrl,IsPrimary) VALUES(@VehicleId,@ImageUrl,@IsPrimary);
 SELECT CONVERT(INT,SCOPE_IDENTITY());
 COMMIT TRANSACTION;
END;
GO
