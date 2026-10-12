CREATE OR ALTER PROCEDURE dbo.usp_VehicleImages_ListByVehicle @VehicleId INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT ImageId,VehicleId,ImageUrl,IsPrimary FROM dbo.VehicleImages WHERE VehicleId=@VehicleId ORDER BY IsPrimary DESC,ImageId;
END;
GO
