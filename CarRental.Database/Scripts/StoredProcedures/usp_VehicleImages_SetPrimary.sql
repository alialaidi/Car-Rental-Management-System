CREATE OR ALTER PROCEDURE dbo.usp_VehicleImages_SetPrimary @VehicleId INT,@ImageId INT
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRANSACTION;
 IF NOT EXISTS(SELECT 1 FROM dbo.Vehicles WITH(UPDLOCK,HOLDLOCK) WHERE VehicleId=@VehicleId) THROW 50056,'Vehicle does not exist.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.VehicleImages WITH(UPDLOCK,HOLDLOCK) WHERE ImageId=@ImageId AND VehicleId=@VehicleId) THROW 50057,'Vehicle image does not exist.',1;
 UPDATE dbo.VehicleImages SET IsPrimary=0 WHERE VehicleId=@VehicleId;
 UPDATE dbo.VehicleImages SET IsPrimary=1 WHERE VehicleId=@VehicleId AND ImageId=@ImageId;
 COMMIT TRANSACTION;
END;
GO
