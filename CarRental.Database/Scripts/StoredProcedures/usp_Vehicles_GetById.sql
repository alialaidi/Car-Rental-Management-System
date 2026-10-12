CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_GetById @VehicleId INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT VehicleId,PlateNumber,Make,Model,Year,CategoryId,FuelTypeId,TransmissionTypeId,SeatsCount,DailyPrice,BranchId,Status,CurrentMileage
 FROM dbo.Vehicles WHERE VehicleId=@VehicleId;
END;
GO
