CREATE OR ALTER PROCEDURE dbo.usp_Vehicles_CountByBranch @BranchId INT
AS BEGIN SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.Vehicles WHERE BranchId=@BranchId; END;
GO
