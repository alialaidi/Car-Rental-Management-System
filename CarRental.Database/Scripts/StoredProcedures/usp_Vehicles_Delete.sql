CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_Delete @VehicleId INT
AS
BEGIN
 SET NOCOUNT ON;
 DELETE dbo.Vehicles WHERE VehicleId=@VehicleId;
 IF @@ROWCOUNT=0 THROW 50056,'Vehicle does not exist.',1;
END;
GO
