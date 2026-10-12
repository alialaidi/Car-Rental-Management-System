CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_ListByBranch @BranchId INT,@Page INT,@PageSize INT
AS
BEGIN
 SET NOCOUNT ON;
 SELECT VehicleId,PlateNumber,Make,Model,Year,CategoryId,FuelTypeId,TransmissionTypeId,SeatsCount,DailyPrice,BranchId,Status,CurrentMileage
 FROM dbo.Vehicles WHERE BranchId=@BranchId ORDER BY VehicleId OFFSET (CONVERT(BIGINT,@Page)-1)*@PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO
