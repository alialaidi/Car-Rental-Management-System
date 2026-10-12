CREATE OR ALTER PROCEDURE dbo.usp_Branches_HasDependents @BranchId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CONVERT(BIT, CASE WHEN EXISTS (SELECT 1 FROM dbo.Employees WHERE BranchId=@BranchId)
        OR EXISTS (SELECT 1 FROM dbo.Vehicles WHERE BranchId=@BranchId) THEN 1 ELSE 0 END);
END;
GO
