CREATE OR ALTER PROCEDURE dbo.usp_Employees_GetById @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT EmployeeId, UserId, BranchId, Position, HireDate FROM dbo.Employees WHERE EmployeeId=@EmployeeId;
END;
GO
