CREATE OR ALTER PROCEDURE dbo.usp_Branches_GetById @BranchId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BranchId, Name, Address, City, Phone, ManagerEmployeeId FROM dbo.Branches WHERE BranchId = @BranchId;
END;
GO
