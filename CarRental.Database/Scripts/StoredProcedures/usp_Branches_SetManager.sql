CREATE OR ALTER PROCEDURE dbo.usp_Branches_SetManager @BranchId INT, @EmployeeId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Branches WHERE BranchId=@BranchId) THROW 50040, 'Branch does not exist.', 1;
    IF @EmployeeId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeId=@EmployeeId AND BranchId=@BranchId)
        THROW 50042, 'Manager must belong to the branch.', 1;
    UPDATE dbo.Branches SET ManagerEmployeeId=@EmployeeId WHERE BranchId=@BranchId;
END;
GO
