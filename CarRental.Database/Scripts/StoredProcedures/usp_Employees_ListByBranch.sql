CREATE OR ALTER PROCEDURE dbo.usp_Employees_ListByBranch @BranchId INT, @Page INT, @PageSize INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeId, UserId, BranchId, Position, HireDate FROM dbo.Employees WHERE BranchId=@BranchId
    ORDER BY EmployeeId OFFSET (CONVERT(BIGINT, @Page) - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

