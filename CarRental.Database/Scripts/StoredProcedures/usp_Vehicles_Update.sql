CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_Update
 @VehicleId INT,@PlateNumber NVARCHAR(20),@Make NVARCHAR(100),@Model NVARCHAR(100),@Year INT,@CategoryId INT,@FuelTypeId INT,
 @TransmissionTypeId INT,@SeatsCount INT,@DailyPrice DECIMAL(18,2),@BranchId INT,@CurrentMileage INT
AS
BEGIN
 SET NOCOUNT ON;
 UPDATE dbo.Vehicles SET PlateNumber=@PlateNumber,Make=@Make,Model=@Model,Year=@Year,CategoryId=@CategoryId,FuelTypeId=@FuelTypeId,
 TransmissionTypeId=@TransmissionTypeId,SeatsCount=@SeatsCount,DailyPrice=@DailyPrice,BranchId=@BranchId,CurrentMileage=@CurrentMileage
 WHERE VehicleId=@VehicleId;
 IF @@ROWCOUNT=0 THROW 50056,'Vehicle does not exist.',1;
END;
GO
