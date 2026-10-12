CREATE OR ALTER PROCEDURE dbo.usp_Employees_GetByUserId @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeId, UserId, BranchId, Position, HireDate FROM dbo.Employees WHERE UserId=@UserId;
END;
GO
