CREATE OR ALTER PROCEDURE dbo.usp_VehicleImages_Remove @ImageId INT
AS
BEGIN
 SET NOCOUNT ON;
 DELETE dbo.VehicleImages WHERE ImageId=@ImageId;
 IF @@ROWCOUNT=0 THROW 50057,'Vehicle image does not exist.',1;
END;
GO
