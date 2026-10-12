CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_Create
 @PlateNumber NVARCHAR(20),@Make NVARCHAR(100),@Model NVARCHAR(100),@Year INT,@CategoryId INT,@FuelTypeId INT,
 @TransmissionTypeId INT,@SeatsCount INT,@DailyPrice DECIMAL(18,2),@BranchId INT,@CurrentMileage INT
AS
BEGIN
 SET NOCOUNT ON;
 INSERT dbo.Vehicles(PlateNumber,Make,Model,Year,CategoryId,FuelTypeId,TransmissionTypeId,SeatsCount,DailyPrice,BranchId,CurrentMileage)
 VALUES(@PlateNumber,@Make,@Model,@Year,@CategoryId,@FuelTypeId,@TransmissionTypeId,@SeatsCount,@DailyPrice,@BranchId,@CurrentMileage);
 SELECT CONVERT(INT,SCOPE_IDENTITY());
END;
GO
