CREATE OR ALTER PROCEDURE dbo.usp_Employees_CountByBranch @BranchId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) FROM dbo.Employees WHERE BranchId=@BranchId;
END;
GO
